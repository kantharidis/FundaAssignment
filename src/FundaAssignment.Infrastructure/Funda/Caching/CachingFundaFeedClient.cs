using System.Globalization;
using System.Text.Json;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Requests;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FundaAssignment.Infrastructure.Funda.Caching;

/// <summary>
/// Remembers pages that have already been fetched.
/// </summary>
internal sealed partial class CachingFundaFeedClient(
    IFundaFeedClient inner,
    IDistributedCache cache,
    FundaCachingOptions options,
    FundaCacheStatistics statistics,
    TimeProvider timeProvider,
    ILogger<CachingFundaFeedClient> logger) : IFundaFeedClient
{
    private static string CacheKeyFor(FeedPageRequest request, DateTimeOffset now, TimeSpan snapshotWindow)
    {
        var snapshot = now.ToUnixTimeSeconds() / (long)snapshotWindow.TotalSeconds;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"funda:{snapshot}:{FundaSearchPath.From(request.Search)}:p{request.Page}:s{request.PageSize}");
    }

    public async Task<FeedPage> GetPageAsync(FeedPageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!options.Enabled)
        {
            statistics.RecordMiss();

            return await inner.GetPageAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var key = CacheKeyFor(request, timeProvider.GetUtcNow(), options.SnapshotWindow);

        if (await ReadAsync(key, cancellationToken).ConfigureAwait(false) is { } cached)
        {
            statistics.RecordHit();
            LogHit(logger, key);

            return cached;
        }

        statistics.RecordMiss();

        var page = await inner.GetPageAsync(request, cancellationToken).ConfigureAwait(false);

        await cache
            .SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(page, FundaCacheJsonContext.Default.FeedPage),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = options.SnapshotWindow },
                cancellationToken)
            .ConfigureAwait(false);

        return page;
    }

    private async Task<FeedPage?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        var stored = await cache.GetAsync(key, cancellationToken).ConfigureAwait(false);

        if (stored is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(stored, FundaCacheJsonContext.Default.FeedPage);
        }
        catch (JsonException failure)
        {
            LogUnreadableEntry(logger, key, failure.Message);

            return null;
        }
    }

    [LoggerMessage(
        EventId = 520,
        Level = LogLevel.Debug,
        Message = "Served {CacheKey} from the cache; no request and no rate limit token spent.")]
    private static partial void LogHit(ILogger logger, string cacheKey);

    [LoggerMessage(
        EventId = 521,
        Level = LogLevel.Warning,
        Message = "Ignored the cached entry for {CacheKey}, which could not be read back: {Reason}")]
    private static partial void LogUnreadableEntry(ILogger logger, string cacheKey, string reason);
}

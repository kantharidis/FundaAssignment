using System.Threading.RateLimiting;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Requests;
using Microsoft.Extensions.Logging;
using Polly;

namespace FundaAssignment.Infrastructure.Funda.Resilience;

internal sealed class ResilientFundaFeedClient : IFundaFeedClient, IDisposable
{
    private readonly IFundaFeedClient inner;
    private readonly RateLimiter limiter;
    private readonly ResiliencePipeline<FeedPage> pipeline;

    internal ResilientFundaFeedClient(
        IFundaFeedClient inner,
        FundaResilienceOptions options,
        TimeProvider timeProvider,
        ILogger<ResilientFundaFeedClient> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(options);

        this.inner = inner;
        limiter = LimiterFor(options.RequestsPerMinute);
        pipeline = FundaResiliencePipeline.Build(limiter, options.MaxRetryAttempts, timeProvider, logger);
    }

    private static TimeSpan SpacingBetweenRequests(int requestsPerMinute) =>
        TimeSpan.FromSeconds(60d / requestsPerMinute);

    public Task<FeedPage> GetPageAsync(FeedPageRequest request, CancellationToken cancellationToken) =>
        pipeline
            .ExecuteAsync(
                static (state, token) => new ValueTask<FeedPage>(state.inner.GetPageAsync(state.request, token)),
                (inner, request),
                cancellationToken)
            .AsTask();

    public void Dispose() => limiter.Dispose();

    private static RateLimiter LimiterFor(int requestsPerMinute) =>
        new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = SpacingBetweenRequests(requestsPerMinute),
            QueueLimit = 64,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
}

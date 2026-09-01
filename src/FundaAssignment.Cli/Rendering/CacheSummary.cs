using System.Globalization;

namespace FundaAssignment.Cli.Rendering;

/// <summary>
/// Says where a ranking's pages came from.
/// </summary>
internal static class CacheSummary
{
    internal static string? For(long hits, long misses) => (hits, misses) switch
    {
        (0, 0) => null,
        (_, 0) => $"All {Count(hits)} pages came from the cache - nothing was asked of funda.",
        (0, _) => $"Fetched all {Count(misses)} pages from funda.",
        _ => $"Fetched {Count(misses)} pages from funda, {Count(hits)} came from the cache.",
    };

    private static string Count(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}

using FundaAssignment.Application.Ports;

namespace FundaAssignment.Infrastructure.Funda.Caching;

/// <summary>
/// How many pages a run took from the cache, and how many it had to fetch.
/// </summary>
internal sealed class FundaCacheStatistics : IListingSourceStatistics
{
    private long hits;
    private long misses;

    public long PagesServedFromCache => Interlocked.Read(ref hits);

    public long PagesFetched => Interlocked.Read(ref misses);

    internal void RecordHit() => Interlocked.Increment(ref hits);

    internal void RecordMiss() => Interlocked.Increment(ref misses);
}

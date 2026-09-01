namespace FundaAssignment.Application.Ports;

/// <summary>
/// Where the pages behind a run came from: how many the listing source had to fetch, and how many
/// it already held.
/// </summary>
public interface IListingSourceStatistics
{
    long PagesServedFromCache { get; }

    long PagesFetched { get; }
}

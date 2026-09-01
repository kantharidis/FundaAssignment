using FundaAssignment.Application.Ports;

namespace FundaAssignment.EndToEnd.Tests.Doubles;

/// <summary>
/// Statistics that never move.
/// </summary>
internal sealed class StubListingSourceStatistics : IListingSourceStatistics
{
    public long PagesServedFromCache => 0;

    public long PagesFetched => 0;
}

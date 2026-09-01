using FundaAssignment.Application.Ports;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.EndToEnd.Tests.Doubles;

/// <summary>
/// Fails on every search, and remembers each one it was asked for.
/// </summary>
internal sealed class FailingListingSource : IListingSource
{
    internal const string Reason = "funda could not supply page 1 of amsterdam: no route to host";

    private readonly List<SearchSpecification> searches = [];

    internal IReadOnlyList<SearchSpecification> ReceivedSearches => searches;

    public IAsyncEnumerable<Listing> GetListings(SearchSpecification search, CancellationToken cancellationToken)
    {
        searches.Add(search);

        throw new ListingSourceUnavailableException(Reason);
    }
}

using System.Runtime.CompilerServices;
using FundaAssignment.Application.Ports;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.EndToEnd.Tests.Doubles;

/// <summary>
/// Hands back the same listings whatever it is asked, and remembers every search it was asked
/// for, in order.
/// </summary>
internal sealed class RecordingListingSource(params Listing[] listings) : IListingSource
{
    private readonly List<SearchSpecification> searches = [];

    internal IReadOnlyList<SearchSpecification> ReceivedSearches => searches;

    public async IAsyncEnumerable<Listing> GetListings(
        SearchSpecification search,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        searches.Add(search);

        foreach (var listing in listings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return listing;
        }
    }
}

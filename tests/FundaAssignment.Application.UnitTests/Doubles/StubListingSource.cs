using System.Runtime.CompilerServices;
using FundaAssignment.Application.Ports;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.Application.UnitTests.Doubles;

/// <summary>
/// Hands back the listings it was built with, and remembers what it was asked for.
/// </summary>
internal sealed class StubListingSource(params Listing[] listings) : IListingSource
{
    internal SearchSpecification? ReceivedSearch { get; private set; }

    public async IAsyncEnumerable<Listing> GetListings(
        SearchSpecification search,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ReceivedSearch = search;

        foreach (var listing in listings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return listing;
        }
    }
}

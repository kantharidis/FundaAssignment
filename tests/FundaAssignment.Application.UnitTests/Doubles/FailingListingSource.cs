using System.Runtime.CompilerServices;
using FundaAssignment.Application.Ports;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.Application.UnitTests.Doubles;

/// <summary>
/// Yields some listings and then fails.
/// </summary>
internal sealed class FailingListingSource(Exception failure, params Listing[] listingsBeforeFailure)
    : IListingSource
{
    public async IAsyncEnumerable<Listing> GetListings(
        SearchSpecification search,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var listing in listingsBeforeFailure)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return listing;
        }

        throw failure;
    }
}

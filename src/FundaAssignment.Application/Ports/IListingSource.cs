using FundaAssignment.Domain.Models;

namespace FundaAssignment.Application.Ports;

/// <summary>
/// Supplies the listings matching a search.
/// </summary>
public interface IListingSource
{
    IAsyncEnumerable<Listing> GetListings(SearchSpecification search, CancellationToken cancellationToken);
}

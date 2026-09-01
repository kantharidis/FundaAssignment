using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Requests;

namespace FundaAssignment.Infrastructure.Funda;

/// <summary>
/// Fetches one page of the funda feed.
/// </summary>
internal interface IFundaFeedClient
{
    Task<FeedPage> GetPageAsync(FeedPageRequest request, CancellationToken cancellationToken);
}

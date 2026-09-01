using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Requests;

namespace FundaAssignment.Infrastructure.Tests.Doubles;

/// <summary>
/// Serves pages that were handed to it, and remembers what it was asked for.
/// </summary>
internal sealed class StubFundaFeedClient(params FeedPage[] pages) : IFundaFeedClient
{
    private readonly List<FeedPageRequest> receivedRequests = [];

    internal IReadOnlyList<FeedPageRequest> ReceivedRequests => receivedRequests;

    public Task<FeedPage> GetPageAsync(FeedPageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        receivedRequests.Add(request);

        return request.Page <= pages.Length
            ? Task.FromResult(pages[request.Page - 1])
            : throw new InvalidOperationException($"Page {request.Page} was requested but the feed has {pages.Length}.");
    }
}

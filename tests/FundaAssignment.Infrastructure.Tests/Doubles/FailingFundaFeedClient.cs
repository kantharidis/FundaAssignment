using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Requests;

namespace FundaAssignment.Infrastructure.Tests.Doubles;

/// <summary>
/// Serves a few pages and then fails.
/// </summary>
internal sealed class FailingFundaFeedClient(Exception failure, params FeedPage[] pagesBeforeFailure)
    : IFundaFeedClient
{
    public Task<FeedPage> GetPageAsync(FeedPageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return request.Page <= pagesBeforeFailure.Length
            ? Task.FromResult(pagesBeforeFailure[request.Page - 1])
            : throw failure;
    }
}

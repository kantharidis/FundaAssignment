using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Requests;

namespace FundaAssignment.Infrastructure.Tests.Doubles;

/// <summary>
/// Refuses the first few requests the way funda does inside a 200 OK, then serves the page.
/// </summary>
internal sealed class RefusingFundaFeedClient(int refusals, FeedPage page) : IFundaFeedClient
{
    private int requests;

    internal int Requests => requests;

    public Task<FeedPage> GetPageAsync(FeedPageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ++requests <= refusals
            ? throw new FundaRejectedRequestException("funda refused the request.")
            : Task.FromResult(page);
    }
}

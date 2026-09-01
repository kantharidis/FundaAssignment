using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Requests;
using FundaAssignment.Infrastructure.Funda.Resilience;
using FundaAssignment.Infrastructure.Tests.Doubles;
using FundaAssignment.Infrastructure.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using System.Diagnostics;

namespace FundaAssignment.Infrastructure.Tests.Resilience;

/// <summary>
/// The client with retrying and rate limiting wrapped around it.
/// </summary>
public sealed class ResilientFundaFeedClientTests
{
    private static readonly FeedPage Page = new([], PageCount: 1, TotalListings: 0, SkippedListings: 0);

    private static readonly FeedPageRequest Request =
        new(new SearchSpecification("amsterdam", ListingFeatures.Garden), Page: 1, PageSize: 25);

    private readonly FakeTimeProvider clock = new();

    [Fact]
    public async Task Hands_the_request_to_the_client_underneath()
    {
        var inner = new StubFundaFeedClient(Page);
        using var client = ClientOver(inner);

        var page = await client.GetPageAsync(Request, TestContext.Current.CancellationToken);

        Assert.Same(Page, page);
        Assert.Equal([Request], inner.ReceivedRequests);
    }

    [Fact]
    public async Task Asks_again_for_a_page_funda_refused()
    {
        var inner = new RefusingFundaFeedClient(refusals: 1, Page);
        using var client = ClientOver(inner);

        var page = await ClockPump.RunAsync(
            clock,
            client.GetPageAsync(Request, TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);

        Assert.Same(Page, page);
        Assert.Equal(2, inner.Requests);
    }

    [Fact]
    public async Task Makes_the_next_request_wait_for_its_token()
    {
        var inner = new StubFundaFeedClient(Page, Page);
        using var client = ClientOver(inner);

        var elapsed = Stopwatch.StartNew();
        await client.GetPageAsync(Request, TestContext.Current.CancellationToken);
        await client.GetPageAsync(Request with { Page = 2 }, TestContext.Current.CancellationToken);
        elapsed.Stop();

        Assert.Equal(2, inner.ReceivedRequests.Count);
        Assert.True(
            elapsed.Elapsed >= TimeSpan.FromMilliseconds(500),
            $"Expected the second request to wait for a token, but the pair took {elapsed.Elapsed}.");
    }

    private ResilientFundaFeedClient ClientOver(IFundaFeedClient inner) =>
        new(
            inner,
            new FundaResilienceOptions { RequestsPerMinute = 100 },
            clock,
            NullLogger<ResilientFundaFeedClient>.Instance);

}

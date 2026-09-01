using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Caching;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Requests;
using FundaAssignment.Infrastructure.Tests.Doubles;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FundaAssignment.Infrastructure.Tests.Caching;

/// <summary>
/// The outermost client, which serves pages it already holds.
/// </summary>
public sealed class CachingFundaFeedClientTests
{
    private static readonly FeedPageRequest Request =
        new(new SearchSpecification("amsterdam", ListingFeatures.Garden), Page: 1, PageSize: 25);

    private static readonly FeedPage Page = new(
        [new Listing(Guid.Parse("3f2c1d8e-0000-4000-8000-000000000001"), new RealEstateAgent(24585, "Agent 24585"))],
        PageCount: 2,
        TotalListings: 3,
        SkippedListings: 0);

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero));

    private readonly IDistributedCache cache =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task Serves_a_page_it_already_holds_without_asking_funda()
    {
        var inner = new StubFundaFeedClient(Page);
        var statistics = new FundaCacheStatistics();
        var client = ClientOver(inner, statistics);

        await client.GetPageAsync(Request, TestContext.Current.CancellationToken);
        var second = await client.GetPageAsync(Request, TestContext.Current.CancellationToken);

        Assert.Single(inner.ReceivedRequests);
        Assert.Equal(Page.PageCount, second.PageCount);
        Assert.Equal(Page.TotalListings, second.TotalListings);
        Assert.Equal([24585], second.Listings.Select(listing => listing.Agent.Id));
        Assert.Equal(1, statistics.PagesFetched);
        Assert.Equal(1, statistics.PagesServedFromCache);
    }

    [Fact]
    public async Task Fetches_again_once_the_snapshot_window_has_passed()
    {
        var inner = new StubFundaFeedClient(Page, Page);
        var client = ClientOver(inner);

        await client.GetPageAsync(Request, TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(20));
        await client.GetPageAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal(2, inner.ReceivedRequests.Count);
    }

    [Fact]
    public async Task Does_not_remember_a_request_funda_refused()
    {
        var inner = new RefusingFundaFeedClient(refusals: 1, Page);
        var client = ClientOver(inner);

        await Assert.ThrowsAsync<FundaRejectedRequestException>(() =>
            client.GetPageAsync(Request, TestContext.Current.CancellationToken));

        var page = await client.GetPageAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal(2, inner.Requests);
        Assert.Equal(Page.TotalListings, page.TotalListings);
    }

    private CachingFundaFeedClient ClientOver(IFundaFeedClient inner, FundaCacheStatistics? statistics = null) =>
        new(
            inner,
            cache,
            new FundaCachingOptions(),
            statistics ?? new FundaCacheStatistics(),
            clock,
            NullLogger<CachingFundaFeedClient>.Instance);

}

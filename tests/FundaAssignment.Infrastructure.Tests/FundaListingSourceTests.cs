using FundaAssignment.Application.Ports;
using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Tests.Client;
using FundaAssignment.Infrastructure.Tests.Doubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace FundaAssignment.Infrastructure.Tests;

/// <summary>
/// Paging over a canned feed. Nothing here touches HTTP.
/// </summary>
public sealed class FundaListingSourceTests
{
    private static readonly SearchSpecification Search = new("amsterdam", ListingFeatures.Garden);

    private static readonly FundaClientOptions Options = new() { ApiKey = "test-key", PageSize = 2 };

    [Fact]
    public async Task Walks_every_page_the_first_one_announced()
    {
        var feed = new StubFundaFeedClient(
            PageOf(pageCount: 3, 24585, 24633),
            PageOf(pageCount: 3, 61489, 24585),
            PageOf(pageCount: 3, 24633));

        var listings = await CollectAsync(feed);

        Assert.Equal([24585, 24633, 61489, 24585, 24633], listings.Select(listing => listing.Agent.Id));
        Assert.Equal([1, 2, 3], feed.ReceivedRequests.Select(request => request.Page));
        Assert.All(feed.ReceivedRequests, request => Assert.Equal(Options.PageSize, request.PageSize));
    }

    [Fact]
    public async Task Fetches_a_page_only_when_the_previous_one_has_been_read()
    {
        var feed = new StubFundaFeedClient(PageOf(pageCount: 2, 24585, 24633), PageOf(pageCount: 2, 61489));

        await foreach (var listing in SourceOf(feed).GetListings(Search, TestContext.Current.CancellationToken))
        {
            break;
        }

        Assert.Single(feed.ReceivedRequests);
    }

    [Fact]
    public async Task Ends_the_run_and_reports_a_refusal_as_a_port_failure()
    {
        var refusal = new FundaRejectedRequestException("funda refused the request.");
        var feed = new FailingFundaFeedClient(refusal, PageOf(pageCount: 3, 24585, 24633));

        var failure = await Assert.ThrowsAsync<ListingSourceUnavailableException>(() => CollectAsync(feed));

        Assert.Same(refusal, failure.InnerException);
        Assert.Contains("funda refused the request.", failure.Message, StringComparison.Ordinal);
    }

    private static FundaListingSource SourceOf(IFundaFeedClient feed) =>
        new(feed, Options, NullLogger<FundaListingSource>.Instance);

    private static async Task<IReadOnlyList<Listing>> CollectAsync(IFundaFeedClient feed)
    {
        var collected = new List<Listing>();

        await foreach (var listing in SourceOf(feed).GetListings(Search, TestContext.Current.CancellationToken))
        {
            collected.Add(listing);
        }

        return collected;
    }

    /// <summary>One page holding one listing per agent id given.</summary>
    private static FeedPage PageOf(int pageCount, params int[] agentIds) =>
        new(
            [.. agentIds.Select(id => new Listing(Guid.NewGuid(), new RealEstateAgent(id, $"Agent {id}")))],
            PageCount: pageCount,
            TotalListings: agentIds.Length * pageCount,
            SkippedListings: 0);

}

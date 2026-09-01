using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Requests;
using FundaAssignment.Infrastructure.Tests.Doubles;
using FundaAssignment.Infrastructure.Tests.Support;

namespace FundaAssignment.Infrastructure.Tests.Client;

/// <summary>
/// The client over a stub transport.
/// </summary>
public sealed class FundaFeedClientTests
{
    private static readonly FundaClientOptions Options = new() { ApiKey = "test-key" };

    private static readonly FeedPageRequest Request =
        new(new SearchSpecification("amsterdam", ListingFeatures.Garden), Page: 1, PageSize: 25);

    [Fact]
    public async Task Fetches_a_page_and_hands_back_domain_listings()
    {
        var page = await GetPageAsync(StubHttpMessageHandler.Serving(Fixture.ReadAllText("amsterdam-garden-page1.json")));

        Assert.Equal(480, page.PageCount);
        Assert.Equal(959, page.TotalListings);
        Assert.Equal([24585, 24633], page.Listings.Select(listing => listing.Agent.Id));
    }

    [Fact]
    public async Task Requests_the_url_that_the_request_describes()
    {
        var transport = StubHttpMessageHandler.Serving(Fixture.ReadAllText("empty-page.json"));

        await GetPageAsync(transport);

        Assert.Equal(
            "http://partnerapi.funda.nl/feeds/Aanbod.svc/json/test-key/"
            + "?type=koop&zo=/amsterdam/tuin/&page=1&pagesize=25",
            transport.ReceivedUri?.ToString());
    }

    [Fact]
    public async Task Fails_when_funda_refuses_the_request_in_the_body_of_a_success() =>
        await Assert.ThrowsAsync<FundaRejectedRequestException>(() =>
            GetPageAsync(StubHttpMessageHandler.Serving(Fixture.ReadAllText("rejected-request.json"))));

    private static async Task<FeedPage> GetPageAsync(StubHttpMessageHandler transport)
    {
        using var httpClient = new HttpClient(transport);

        return await new FundaFeedClient(httpClient, Options)
            .GetPageAsync(Request, TestContext.Current.CancellationToken);
    }

}

using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Mapping;
using FundaAssignment.Infrastructure.Tests.Support;

using System.Text.Json;

namespace FundaAssignment.Infrastructure.Tests.Mapping;

/// <summary>
/// Deserialisation and mapping, against a recorded response.
/// </summary>
public sealed class FundaContractMapperTests
{
    [Fact]
    public void Maps_the_recorded_feed_response()
    {
        var page = Map("amsterdam-garden-page1.json");

        Assert.Equal(480, page.PageCount);
        Assert.Equal(959, page.TotalListings);
        Assert.Equal(0, page.SkippedListings);

        Assert.Collection(
            page.Listings,
            listing =>
            {
                Assert.Equal(new Guid("a99813e5-cf01-49bf-8260-56ece054f859"), listing.Id);
                Assert.Equal(24585, listing.Agent.Id);
                Assert.Equal("Bert van Vulpen makelaars + hypotheken Amstelveen", listing.Agent.Name);
            },
            listing => Assert.Equal(new Guid("9ddd5cc7-fabd-420a-abb0-3bb447cb169e"), listing.Id));
    }

    [Fact]
    public void Rejects_a_page_funda_marked_as_failed()
    {
        var failure = Assert.Throws<FundaRejectedRequestException>(() => Map("rejected-request.json"));

        Assert.Contains("ValidationFailed=True", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Rate limit exceeded", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Skips_listings_that_arrived_without_the_fields_we_need()
    {
        var page = Map("objects-missing-fields.json");

        Assert.Equal(3, page.SkippedListings);
        Assert.Equal([24585, 102], page.Listings.Select(listing => listing.Agent.Id));
        Assert.Equal("Padded Makelaardij", page.Listings[1].Agent.Name);
    }

    private static FeedPage Map(string fixtureFileName)
    {
        using var json = Fixture.OpenRead(fixtureFileName);

        return FundaContractMapper.ToFeedPage(
            JsonSerializer.Deserialize(json, FundaJsonContext.Default.FeedPageResponse));
    }

}

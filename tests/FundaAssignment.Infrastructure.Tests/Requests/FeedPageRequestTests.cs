using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda.Requests;

namespace FundaAssignment.Infrastructure.Tests.Requests;

/// <summary>
/// URL construction for one page of one search.
/// </summary>
public sealed class FeedPageRequestTests
{
    // Shaped like a real key so the URL reads the same, but not one.
    private const string ApiKey = "00000000000000000000000000000000";

    private static readonly Uri BaseAddress = new("http://partnerapi.funda.nl/feeds/Aanbod.svc/json/");

    [Fact]
    public void Builds_the_url_the_assignment_documents() =>
        Assert.Equal(
            "http://partnerapi.funda.nl/feeds/Aanbod.svc/json/" + ApiKey + "/"
            + "?type=koop&zo=/amsterdam/tuin/&page=1&pagesize=25",
            Uri(new SearchSpecification("amsterdam", ListingFeatures.Garden), page: 1, pageSize: 25));

    [Theory]
    [InlineData("   Amsterdam   ", "/amsterdam/")]
    [InlineData("Den Haag", "/den-haag/")]
    [InlineData("s  Hertogenbosch", "/s-hertogenbosch/")]
    public void Renders_the_city_the_way_funda_spells_it_in_a_url(string city, string expected) =>
        Assert.Contains($"zo={expected}&", Uri(new SearchSpecification(city)), StringComparison.Ordinal);

    [Fact]
    public void Does_not_carry_the_key_in_its_own_state() =>
        Assert.DoesNotContain(
            ApiKey,
            new FeedPageRequest(new SearchSpecification("amsterdam"), 1, 25).ToString(),
            StringComparison.Ordinal);

    private static string Uri(SearchSpecification search, int page = 1, int pageSize = 25) =>
        new FeedPageRequest(search, page, pageSize).ToUri(BaseAddress, ApiKey).ToString();

}

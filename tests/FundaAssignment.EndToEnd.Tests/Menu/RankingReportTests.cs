using FundaAssignment.Application.Handlers;
using FundaAssignment.Application.Ports;
using FundaAssignment.Cli.Menu;
using FundaAssignment.Domain.Models;
using FundaAssignment.EndToEnd.Tests.Doubles;

namespace FundaAssignment.EndToEnd.Tests.Menu;

/// <summary>
/// A whole run, from query to rendered tables, over a stubbed feed.
/// </summary>
public sealed class RankingReportTests
{
    private static readonly Listing[] Listings =
    [
        Listing(24585, "Broersma Wonen"),
        Listing(24585, "Broersma Wonen"),
        Listing(24633, "Hallie & Van Klooster"),
    ];

    /// <summary>The two questions the assignment asks, in the order it asks them.</summary>
    [Fact]
    public async Task Asks_for_all_of_amsterdam_and_then_for_gardens_only()
    {
        var source = new RecordingListingSource(Listings);

        await RunAsync(source);

        Assert.Equal(
            [new SearchSpecification("amsterdam"), new SearchSpecification("amsterdam", ListingFeatures.Garden)],
            source.ReceivedSearches);
    }

    [Fact]
    public async Task Prints_a_table_for_each_of_them_in_the_order_it_asked()
    {
        var report = await RunAsync(new RecordingListingSource(Listings));

        Assert.Equal(2, report.Split("## ", StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.True(
            report.IndexOf("## Amsterdam - ", StringComparison.Ordinal)
            < report.IndexOf("## Amsterdam with a garden", StringComparison.Ordinal),
            $"The garden table should come second, but the report reads:{Environment.NewLine}{report}");
    }

    [Fact]
    public async Task Ranks_the_agent_with_the_most_listings_first()
    {
        var report = await RunAsync(new RecordingListingSource(Listings));

        Assert.Contains("| 1 | Broersma Wonen | 2 |", report, StringComparison.Ordinal);
        Assert.Contains("| 2 | Hallie & Van Klooster | 1 |", report, StringComparison.Ordinal);
    }

    private static async Task<string> RunAsync(IListingSource source)
    {
        await using var output = new StringWriter();

        await RankingReport.WriteAsync(new RankAgentsHandler(source), output, TestContext.Current.CancellationToken);

        return output.ToString();
    }

    private static Listing Listing(int agentId, string agentName) =>
        new(Guid.NewGuid(), new RealEstateAgent(agentId, agentName));
}

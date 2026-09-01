using FundaAssignment.Application.Handlers;
using FundaAssignment.Application.Ports;
using FundaAssignment.Cli.Menu;
using FundaAssignment.Domain.Models;
using FundaAssignment.EndToEnd.Tests.Doubles;

namespace FundaAssignment.EndToEnd.Tests.Menu;

/// <summary>
/// The interactive menu, driven through readers and writers rather than the console.
/// </summary>
public sealed class RankingMenuTests
{
    private static readonly Listing[] Listings =
    [
        Listing(24585, "Broersma Wonen"),
        Listing(24585, "Broersma Wonen"),
        Listing(24633, "Hallie & Van Klooster"),
    ];

    [Fact]
    public async Task Ranks_what_was_chosen_and_keeps_the_menu_out_of_the_tables()
    {
        var session = await RunAsync("1", "q");

        Assert.Equal([new SearchSpecification("amsterdam")], session.Source.ReceivedSearches);
        Assert.Contains("## Amsterdam - top 10 agents", session.Tables, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose:", session.Tables, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stops_when_asked_to_quit_and_when_the_input_runs_out()
    {
        Assert.Empty((await RunAsync("q", "1")).Source.ReceivedSearches);
        Assert.Empty((await RunAsync()).Source.ReceivedSearches);
    }

    [Fact]
    public async Task Keeps_the_session_alive_when_a_ranking_fails()
    {
        var source = new FailingListingSource();

        var session = await DriveAsync(source, "1", "1", "q");

        Assert.Equal(2, source.ReceivedSearches.Count);
        Assert.Contains(FailingListingSource.Reason, session.Prompts, StringComparison.Ordinal);
        Assert.DoesNotContain(FailingListingSource.Reason, session.Tables, StringComparison.Ordinal);
    }

    private static async Task<(string Tables, string Prompts, RecordingListingSource Source)> RunAsync(
        params string[] typed)
    {
        var source = new RecordingListingSource(Listings);

        var session = await DriveAsync(source, typed);

        return (session.Tables, session.Prompts, source);
    }

    private static async Task<(string Tables, string Prompts)> DriveAsync(
        IListingSource source,
        params string[] typed)
    {
        using var input = new StringReader(string.Join(Environment.NewLine, typed));
        await using var tables = new StringWriter();
        await using var prompts = new StringWriter();

        await RankingMenu.RunAsync(
            new RankAgentsHandler(source),
            new StubListingSourceStatistics(),
            input,
            tables,
            prompts,
            TestContext.Current.CancellationToken);

        return (tables.ToString(), prompts.ToString());
    }

    private static Listing Listing(int agentId, string agentName) =>
        new(Guid.NewGuid(), new RealEstateAgent(agentId, agentName));
}

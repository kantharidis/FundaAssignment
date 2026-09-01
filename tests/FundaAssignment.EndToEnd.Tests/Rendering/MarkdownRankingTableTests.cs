using FundaAssignment.Application.Dtos;
using FundaAssignment.Cli.Rendering;

namespace FundaAssignment.EndToEnd.Tests.Rendering;

/// <summary>
/// The markdown table a run prints.
/// </summary>
public sealed class MarkdownRankingTableTests
{
    private static readonly RankingResultDto Ranking = new(
        [
            new RankedAgentDto(1, 24585, "Broersma Wonen", 187),
            new RankedAgentDto(2, 24633, "Hallie & Van Klooster", 142),
        ],
        ListingsCounted: 2431);

    [Fact]
    public void Renders_a_table_markdown_can_actually_show()
    {
        var table = MarkdownRankingTable.Render("Amsterdam - top 10 agents", Ranking);

        Assert.StartsWith("## Amsterdam - top 10 agents", table, StringComparison.Ordinal);
        Assert.Contains("| Rank | Agent | Listings |", table, StringComparison.Ordinal);
        Assert.Contains("| ---: | --- | ---: |", table, StringComparison.Ordinal);
        Assert.Contains("| 1 | Broersma Wonen | 187 |", table, StringComparison.Ordinal);
        Assert.Contains("| 2 | Hallie & Van Klooster | 142 |", table, StringComparison.Ordinal);
    }

    [Fact]
    public void Escapes_a_pipe_in_an_agent_name()
    {
        var awkward = new RankingResultDto([new RankedAgentDto(1, 1, "Smit | Zoon", 3)], ListingsCounted: 3);

        Assert.Contains(@"| 1 | Smit \| Zoon | 3 |", MarkdownRankingTable.Render("Amsterdam", awkward), StringComparison.Ordinal);
    }

    [Fact]
    public void Says_so_when_nothing_matched()
    {
        var table = MarkdownRankingTable.Render("Rotterdam", new RankingResultDto([], ListingsCounted: 0));

        Assert.Contains("No listings matched", table, StringComparison.Ordinal);
        Assert.DoesNotContain("| Rank |", table, StringComparison.Ordinal);
    }
}

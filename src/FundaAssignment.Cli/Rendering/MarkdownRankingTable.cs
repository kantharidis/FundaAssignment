using System.Globalization;
using System.Text;
using FundaAssignment.Application.Dtos;

namespace FundaAssignment.Cli.Rendering;

/// <summary>
/// Renders one ranking as a markdown table.
/// </summary>
internal static class MarkdownRankingTable
{
    internal static string Render(string title, RankingResultDto ranking)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(ranking);

        var table = new StringBuilder()
            .Append("## ").AppendLine(title)
            .AppendLine();

        if (ranking.Agents.Count == 0)
        {
            return table.AppendLine("No listings matched this search.").ToString();
        }

        table
            .Append(Count(ranking.ListingsCounted)).AppendLine(" listings counted.")
            .AppendLine()
            .AppendLine("| Rank | Agent | Listings |")
            .AppendLine("| ---: | --- | ---: |");

        foreach (var agent in ranking.Agents)
        {
            table
                .Append("| ").Append(Count(agent.Rank))
                .Append(" | ").Append(Cell(agent.AgentName))
                .Append(" | ").Append(Count(agent.ListingCount))
                .AppendLine(" |");
        }

        return table.ToString();
    }

    private static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string Cell(string value) => value.Replace("|", @"\|", StringComparison.Ordinal);
}

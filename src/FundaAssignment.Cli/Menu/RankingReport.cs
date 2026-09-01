using FundaAssignment.Application.Handlers;
using FundaAssignment.Application.Queries;
using FundaAssignment.Cli.Rendering;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.Cli.Menu;

internal sealed record RankingChoice(string Title, SearchSpecification Search);

internal static class RankingReport
{
    private const string City = "Amsterdam";

    private const int TopCount = 10;

    internal static RankingChoice Everything { get; } = new(
        $"{City} - top {TopCount} agents by properties for sale",
        new SearchSpecification(City.ToLowerInvariant()));

    internal static RankingChoice WithGarden { get; } = new(
        $"{City} with a garden - top {TopCount} agents by properties for sale",
        new SearchSpecification(City.ToLowerInvariant(), ListingFeatures.Garden));

    internal static IReadOnlyList<RankingChoice> Both { get; } = [Everything, WithGarden];

    internal static async Task WriteAsync(
        RankAgentsHandler handler,
        TextWriter output,
        IReadOnlyList<RankingChoice> choices,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(choices);

        foreach (var choice in choices)
        {
            var ranking = await handler
                .HandleAsync(new RankAgentsQuery(choice.Search, TopCount), cancellationToken)
                .ConfigureAwait(false);

            await output.WriteLineAsync(MarkdownRankingTable.Render(choice.Title, ranking)).ConfigureAwait(false);
        }
    }

    internal static Task WriteAsync(RankAgentsHandler handler, TextWriter output, CancellationToken cancellationToken) =>
        WriteAsync(handler, output, Both, cancellationToken);
}

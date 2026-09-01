using FundaAssignment.Application.Handlers;
using FundaAssignment.Application.Ports;
using FundaAssignment.Cli.Rendering;

namespace FundaAssignment.Cli.Menu;

/// <summary>
/// Menu for ranking agents by the properties they have for sale in Amsterdam, optionally
/// with a garden.
/// </summary>
internal static class RankingMenu
{
    internal static async Task RunAsync(
        RankAgentsHandler handler,
        IListingSourceStatistics statistics,
        TextReader input,
        TextWriter tables,
        TextWriter prompts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(prompts);

        while (!cancellationToken.IsCancellationRequested)
        {
            await WriteMenuAsync(prompts).ConfigureAwait(false);

            var typed = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (typed is null)
            {
                return;
            }

            var chosen = typed.Trim();

            if (string.Equals(chosen, "q", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var choices = ChoicesFor(chosen);

            if (choices is null)
            {
                await prompts.WriteLineAsync($"'{chosen}' is not one of the choices.").ConfigureAwait(false);

                continue;
            }

            foreach (var choice in choices)
            {
                await prompts.WriteLineAsync($"Ranking {choice.Title}...").ConfigureAwait(false);
            }

            var fromCacheBefore = statistics.PagesServedFromCache;
            var fetchedBefore = statistics.PagesFetched;

            try
            {
                await RankingReport.WriteAsync(handler, tables, choices, cancellationToken).ConfigureAwait(false);
            }
            catch (ListingSourceUnavailableException failure)
            {
                await prompts
                    .WriteLineAsync($"That ranking could not be produced: {failure.Message}")
                    .ConfigureAwait(false);

                await prompts
                    .WriteLineAsync("Choose again to retry - the pages that did arrive are still cached.")
                    .ConfigureAwait(false);
            }

            var summary = CacheSummary.For(
                statistics.PagesServedFromCache - fromCacheBefore,
                statistics.PagesFetched - fetchedBefore);

            if (summary is not null)
            {
                await prompts.WriteLineAsync(summary).ConfigureAwait(false);
            }
        }
    }

    private static IReadOnlyList<RankingChoice>? ChoicesFor(string chosen) => chosen switch
    {
        "1" => [RankingReport.Everything],
        "2" => [RankingReport.WithGarden],
        "3" => RankingReport.Both,
        _ => null,
    };

    private static async Task WriteMenuAsync(TextWriter prompts)
    {
        await prompts.WriteLineAsync().ConfigureAwait(false);
        await prompts.WriteLineAsync("  1  Agents with the most properties for sale in Amsterdam").ConfigureAwait(false);
        await prompts.WriteLineAsync("  2  ...and the same for properties with a garden").ConfigureAwait(false);
        await prompts.WriteLineAsync("  3  Both").ConfigureAwait(false);
        await prompts.WriteLineAsync("  q  Quit").ConfigureAwait(false);
        await prompts.WriteAsync("Choose: ").ConfigureAwait(false);
    }
}

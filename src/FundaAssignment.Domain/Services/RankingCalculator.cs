using FundaAssignment.Domain.Models;

namespace FundaAssignment.Domain.Services;

public static class RankingCalculator
{
    /// <summary>
    /// Ranks the <paramref name="topCount"/> agents with the most listings.
    /// </summary>
    public static Ranking Rank(IEnumerable<Listing> listings, int topCount)
    {
        ArgumentNullException.ThrowIfNull(listings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topCount);

        var all = listings as IReadOnlyCollection<Listing> ?? listings.ToArray();

        var entries = all
            .GroupBy(listing => listing.Agent.Id)
            .Select(group => (Agent: group.First().Agent, Count: group.Count()))
            .OrderByDescending(agent => agent.Count)
            .ThenBy(agent => agent.Agent.Name, StringComparer.Ordinal)
            .ThenBy(agent => agent.Agent.Id)
            .Take(topCount)
            .Select((agent, index) => new RankingEntry(index + 1, agent.Agent, agent.Count))
            .ToArray();

        return new Ranking(entries, all.Count);
    }
}

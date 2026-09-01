using FundaAssignment.Domain.Models;
using FundaAssignment.Domain.Services;

namespace FundaAssignment.Domain.UnitTests.Services;

/// <summary>
/// Counting, ordering, tie-breaks and the top-N cut.
/// </summary>
public sealed class RankingCalculatorTests
{
    [Fact]
    public void Counts_listings_per_agent_and_ranks_them_by_that_count()
    {
        var ranking = RankingCalculator.Rank(
            [
                Listing(1, agentId: 20, "Hallie"),
                Listing(2, agentId: 10, "Broersma"),
                Listing(3, agentId: 10, "Broersma"),
            ],
            topCount: 10);

        Assert.Equal(3, ranking.ListingsCounted);
        Assert.Collection(
            ranking.Entries,
            entry => AssertEntry(entry, rank: 1, agentId: 10, listingCount: 2),
            entry => AssertEntry(entry, rank: 2, agentId: 20, listingCount: 1));
    }

    [Fact]
    public void Returns_at_most_the_requested_number_of_agents()
    {
        var listings = Enumerable
            .Range(1, 25)
            .Select(index => Listing(index, agentId: index, $"Agent {index:D2}"));

        Assert.Equal(10, RankingCalculator.Rank(listings, topCount: 10).Entries.Count);
    }

    [Fact]
    public void Produces_the_same_ranking_whatever_order_listings_arrive_in()
    {
        var listings = Enumerable
            .Range(1, 60)
            .Select(index => Listing(index, agentId: (index % 7) + 1, $"Agent {(index % 7) + 1}"))
            .ToArray();

        var shuffled = listings.ToArray();
        new Random(Seed: 1337).Shuffle(shuffled);

        Assert.Equal(Positions(listings), Positions(shuffled));

        static IEnumerable<(int Rank, int AgentId, int Count)> Positions(IEnumerable<Listing> source) =>
            RankingCalculator.Rank(source, topCount: 10).Entries
                .Select(entry => (entry.Rank, entry.Agent.Id, entry.ListingCount))
                .ToArray();
    }

    private static void AssertEntry(RankingEntry entry, int rank, int agentId, int listingCount)
    {
        Assert.Equal(rank, entry.Rank);
        Assert.Equal(agentId, entry.Agent.Id);
        Assert.Equal(listingCount, entry.ListingCount);
    }

    /// <summary>Deterministic listing identifiers keep the assertions readable.</summary>
    private static Listing Listing(int listingNumber, int agentId, string agentName) =>
        new(new Guid(listingNumber, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), new RealEstateAgent(agentId, agentName));
}

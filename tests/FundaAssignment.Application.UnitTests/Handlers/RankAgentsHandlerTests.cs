using FundaAssignment.Application.Dtos;
using FundaAssignment.Application.Handlers;
using FundaAssignment.Application.Queries;
using FundaAssignment.Application.UnitTests.Doubles;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.Application.UnitTests.Handlers;

public sealed class RankAgentsHandlerTests
{
    private static readonly SearchSpecification Amsterdam = new("amsterdam");

    [Fact]
    public async Task Ranks_the_agents_behind_the_listings_it_is_given()
    {
        var handler = new RankAgentsHandler(new StubListingSource(
            Listing(1, agentId: 10, "Broersma"),
            Listing(2, agentId: 10, "Broersma"),
            Listing(3, agentId: 20, "Hallie")));

        var result = await handler.HandleAsync(
            new RankAgentsQuery(Amsterdam, TopCount: 10),
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result.ListingsCounted);
        Assert.Collection(
            result.Agents,
            agent => AssertAgent(agent, rank: 1, agentId: 10, name: "Broersma", listingCount: 2),
            agent => AssertAgent(agent, rank: 2, agentId: 20, name: "Hallie", listingCount: 1));
    }

    [Fact]
    public async Task Asks_the_source_for_the_search_it_was_given()
    {
        var source = new StubListingSource();
        var search = new SearchSpecification("amsterdam", ListingFeatures.Garden);

        await new RankAgentsHandler(source).HandleAsync(
            new RankAgentsQuery(search, TopCount: 10),
            TestContext.Current.CancellationToken);

        Assert.Equal(search, source.ReceivedSearch);
    }

    [Fact]
    public async Task Does_not_hide_a_failure_from_the_source()
    {
        var source = new FailingListingSource(
            new InvalidOperationException("page 4 could not be fetched"),
            Listing(1, agentId: 10, "Broersma"));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RankAgentsHandler(source).HandleAsync(
                new RankAgentsQuery(Amsterdam, TopCount: 10),
                TestContext.Current.CancellationToken));

        Assert.Equal("page 4 could not be fetched", failure.Message);
    }

    private static void AssertAgent(RankedAgentDto agent, int rank, int agentId, string name, int listingCount)
    {
        Assert.Equal(rank, agent.Rank);
        Assert.Equal(agentId, agent.AgentId);
        Assert.Equal(name, agent.AgentName);
        Assert.Equal(listingCount, agent.ListingCount);
    }

    private static Listing Listing(int listingNumber, int agentId, string agentName) =>
        new(new Guid(listingNumber, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), new RealEstateAgent(agentId, agentName));
}

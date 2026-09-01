using FundaAssignment.Application.Dtos;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.Application.Mapping;

/// <summary>
/// Flattens a domain <see cref="Ranking"/> into the shape that leaves this layer.
/// </summary>
internal static class RankingResultMapper
{
    internal static RankingResultDto ToDto(Ranking ranking) =>
        new(
            [.. ranking.Entries.Select(entry =>
                new RankedAgentDto(entry.Rank, entry.Agent.Id, entry.Agent.Name, entry.ListingCount))],
            ranking.ListingsCounted);
}

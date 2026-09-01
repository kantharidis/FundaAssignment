namespace FundaAssignment.Application.Dtos;

public sealed record RankingResultDto(IReadOnlyList<RankedAgentDto> Agents, int ListingsCounted);

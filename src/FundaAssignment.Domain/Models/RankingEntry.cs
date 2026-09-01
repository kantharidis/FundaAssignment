namespace FundaAssignment.Domain.Models;

/// <param name="Rank">Position in the ranking, starting at 1.</param>
public sealed record RankingEntry(int Rank, RealEstateAgent Agent, int ListingCount);

namespace FundaAssignment.Domain.Models;

public sealed record Ranking(IReadOnlyList<RankingEntry> Entries, int ListingsCounted);

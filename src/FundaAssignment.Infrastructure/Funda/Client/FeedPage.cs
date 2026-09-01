using FundaAssignment.Domain.Models;

namespace FundaAssignment.Infrastructure.Funda.Client;

/// <param name="Listings">Listings on this page that could be mapped.</param>
/// <param name="PageCount">Total pages for this search at the requested page size.</param>
/// <param name="TotalListings">Listings funda reports for this search.</param>
/// <param name="SkippedListings">Entries dropped because they arrived without a usable id or agent.</param>
internal sealed record FeedPage(
    IReadOnlyList<Listing> Listings,
    int PageCount,
    int TotalListings,
    int SkippedListings);

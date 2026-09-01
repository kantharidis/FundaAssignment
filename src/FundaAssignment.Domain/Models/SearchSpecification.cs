namespace FundaAssignment.Domain.Models;

/// <summary>
/// What to search for: a place, and optionally features the listing must have.
/// </summary>
public sealed record SearchSpecification(string City, ListingFeatures Features = ListingFeatures.None);

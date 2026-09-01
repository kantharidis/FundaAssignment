namespace FundaAssignment.Domain.Models;

/// <summary>
/// A property offered for sale.
/// </summary>
public sealed record Listing(Guid Id, RealEstateAgent Agent);

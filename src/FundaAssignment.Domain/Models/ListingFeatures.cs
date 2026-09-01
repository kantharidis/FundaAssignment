namespace FundaAssignment.Domain.Models;

/// <summary>
/// Features a search can require of a listing, in domain names rather than funda's.
/// Infrastructure translates <see cref="Garden"/> into the Dutch path segment "tuin".
/// </summary>
public enum ListingFeatures
{
    None,

    Garden
}

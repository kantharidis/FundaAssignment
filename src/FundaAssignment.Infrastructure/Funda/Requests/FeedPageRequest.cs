using System.Globalization;
using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda.Client;

namespace FundaAssignment.Infrastructure.Funda.Requests;

/// <summary>
/// One page of one search, as a funda feed request.
/// </summary>
/// <param name="Page">One-based page number, as funda counts them.</param>
internal sealed record FeedPageRequest(SearchSpecification Search, int Page, int PageSize)
{
    /// <summary>
    /// The assignment only ever asks about properties for sale.
    /// </summary>
    private const string OfferType = "koop";

    internal Uri ToUri(Uri baseAddress, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PageSize);

        // The search path keeps its slashes: funda documents zo=/amsterdam/tuin/.
        var query = string.Create(
            CultureInfo.InvariantCulture,
            $"?type={OfferType}&zo={FundaSearchPath.From(Search)}&page={Page}&pagesize={PageSize}");

        return new Uri(baseAddress, Uri.EscapeDataString(apiKey) + "/" + query);
    }
}

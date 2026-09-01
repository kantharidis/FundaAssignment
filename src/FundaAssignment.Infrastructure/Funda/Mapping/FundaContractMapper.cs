using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Contracts;

namespace FundaAssignment.Infrastructure.Funda.Mapping;

/// <summary>
/// Turns funda's wire contracts into domain listings.
/// </summary>
internal static class FundaContractMapper
{
    internal static FeedPage ToFeedPage(FeedPageResponse? response)
    {
        if (response is null)
        {
            throw new FundaRejectedRequestException("funda returned an empty body where a feed page was expected.");
        }

        EnsureAccepted(response);

        var listings = new List<Listing>(response.Objects?.Count ?? 0);
        var skipped = 0;

        foreach (var payload in response.Objects ?? [])
        {
            var listing = ToListing(payload);

            if (listing is null)
            {
                skipped++;
            }
            else
            {
                listings.Add(listing);
            }
        }

        if (response.Paging?.AantalPaginas is not { } pageCount)
        {
            throw new FundaRejectedRequestException("funda returned a feed page without paging information.");
        }

        return new FeedPage(listings, pageCount, response.TotaalAantalObjecten ?? 0, skipped);
    }

    private static void EnsureAccepted(FeedPageResponse response)
    {
        if (response.ValidationFailed is true
            || response.EmailNotConfirmed is true
            || response.AccountStatus is not (null or 0))
        {
            throw new FundaRejectedRequestException(
                $"funda refused the request. ValidationFailed={response.ValidationFailed}, "
                + $"AccountStatus={response.AccountStatus}, EmailNotConfirmed={response.EmailNotConfirmed}, "
                + $"ValidationReport={response.ValidationReport?.ToString() ?? "null"}");
        }
    }

    private static Listing? ToListing(ObjectPayload payload)
    {
        if (!Guid.TryParse(payload.Id, out var id) || id == Guid.Empty)
        {
            return null;
        }

        if (payload.MakelaarId is not > 0 || string.IsNullOrWhiteSpace(payload.MakelaarNaam))
        {
            return null;
        }

        return new Listing(id, new RealEstateAgent(payload.MakelaarId.Value, payload.MakelaarNaam.Trim()));
    }
}

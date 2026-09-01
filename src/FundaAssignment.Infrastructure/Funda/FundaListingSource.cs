using System.Runtime.CompilerServices;
using FundaAssignment.Application.Ports;
using FundaAssignment.Domain.Models;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Requests;
using Microsoft.Extensions.Logging;

namespace FundaAssignment.Infrastructure.Funda;

/// <summary>
/// The funda feed as an application port: pages through a search and streams what it finds.
/// </summary>
internal sealed partial class FundaListingSource(
    IFundaFeedClient feedClient,
    FundaClientOptions options,
    ILogger<FundaListingSource> logger) : IListingSource
{
    public async IAsyncEnumerable<Listing> GetListings(
        SearchSpecification search,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);

        var firstPage = await GetPageAsync(search, 1, cancellationToken).ConfigureAwait(false);

        var listingsCounted = firstPage.Listings.Count;
        var listingsSkipped = firstPage.SkippedListings;

        foreach (var listing in firstPage.Listings)
        {
            yield return listing;
        }

        // The page count is read once, from page 1
        for (var page = 2; page <= firstPage.PageCount; page++)
        {
            var nextPage = await GetPageAsync(search, page, cancellationToken).ConfigureAwait(false);

            listingsCounted += nextPage.Listings.Count;
            listingsSkipped += nextPage.SkippedListings;

            foreach (var listing in nextPage.Listings)
            {
                yield return listing;
            }
        }

        if (listingsSkipped > 0)
        {
            LogSkippedListings(logger, listingsSkipped, search.City);
        }

        LogRunFinished(logger, search.City, listingsCounted, firstPage.TotalListings, firstPage.PageCount);
    }

    /// <summary>
    /// Fetches one page, translating funda's failures into the port's own.
    /// </summary>
    private async Task<FeedPage> GetPageAsync(
        SearchSpecification search,
        int page,
        CancellationToken cancellationToken)
    {
        try
        {
            return await feedClient
                .GetPageAsync(new FeedPageRequest(search, page, options.PageSize), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception failure) when (failure is FundaRejectedRequestException or HttpRequestException)
        {
            throw new ListingSourceUnavailableException(
                $"funda could not supply page {page} of {search.City}: {failure.Message}",
                failure);
        }
    }

    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Information,
        Message = "Paged {City}: counted {ListingsCounted} listings over {PageCount} pages, funda reported {TotalListings}.")]
    private static partial void LogRunFinished(
        ILogger logger,
        string city,
        int listingsCounted,
        int totalListings,
        int pageCount);

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Warning,
        Message = "Dropped {ListingsSkipped} listings while paging {City}: the feed returned them without an agent.")]
    private static partial void LogSkippedListings(ILogger logger, int listingsSkipped, string city);
}

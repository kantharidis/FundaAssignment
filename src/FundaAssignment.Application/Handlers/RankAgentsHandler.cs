using FundaAssignment.Application.Dtos;
using FundaAssignment.Application.Mapping;
using FundaAssignment.Application.Ports;
using FundaAssignment.Application.Queries;
using FundaAssignment.Domain.Models;
using FundaAssignment.Domain.Services;

namespace FundaAssignment.Application.Handlers;

/// <summary>
/// The use case: pull every listing matching the search, rank the agents behind them, hand back a
/// flat result.
/// </summary>
public sealed class RankAgentsHandler(IListingSource listingSource)
{
    public async Task<RankingResultDto> HandleAsync(RankAgentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var collectedListings = new List<Listing>();

        await foreach (var listing in listingSource.GetListings(query.Search, cancellationToken))
        {
            collectedListings.Add(listing);
        }

        return RankingResultMapper.ToDto(RankingCalculator.Rank(collectedListings, query.TopCount));
    }
}

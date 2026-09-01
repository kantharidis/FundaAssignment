using System.Text.Json;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Contracts;
using FundaAssignment.Infrastructure.Funda.Mapping;
using FundaAssignment.Infrastructure.Funda.Requests;

namespace FundaAssignment.Infrastructure.Funda.Client;

/// <summary>
/// Fetches one page over HTTP and maps it.
/// </summary>
internal sealed class FundaFeedClient(HttpClient httpClient, FundaClientOptions options) : IFundaFeedClient
{
    public async Task<FeedPage> GetPageAsync(FeedPageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uri = request.ToUri(options.BaseAddress, options.ApiKey);

        using var response = await httpClient
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        return FundaContractMapper.ToFeedPage(await ReadPayloadAsync(body, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<FeedPageResponse?> ReadPayloadAsync(Stream body, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer
                .DeserializeAsync(body, FundaJsonContext.Default.FeedPageResponse, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException failure)
        {
            throw new FundaRejectedRequestException("funda returned a body that is not a feed page.", failure);
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace FundaAssignment.Infrastructure.Funda.Contracts;

internal sealed record FeedPageResponse
{
    [JsonPropertyName("Objects")]
    public IReadOnlyList<ObjectPayload>? Objects { get; init; }

    [JsonPropertyName("Paging")]
    public PagingPayload? Paging { get; init; }

    [JsonPropertyName("TotaalAantalObjecten")]
    public int? TotaalAantalObjecten { get; init; }

    [JsonPropertyName("ValidationFailed")]
    public bool? ValidationFailed { get; init; }

    [JsonPropertyName("ValidationReport")]
    public JsonElement? ValidationReport { get; init; }

    [JsonPropertyName("AccountStatus")]
    public int? AccountStatus { get; init; }

    [JsonPropertyName("EmailNotConfirmed")]
    public bool? EmailNotConfirmed { get; init; }
}

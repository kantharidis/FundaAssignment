using System.Text.Json.Serialization;

namespace FundaAssignment.Infrastructure.Funda.Contracts;

internal sealed record ObjectPayload
{
    [JsonPropertyName("Id")]
    public string? Id { get; init; }

    [JsonPropertyName("MakelaarId")]
    public int? MakelaarId { get; init; }

    [JsonPropertyName("MakelaarNaam")]
    public string? MakelaarNaam { get; init; }
}

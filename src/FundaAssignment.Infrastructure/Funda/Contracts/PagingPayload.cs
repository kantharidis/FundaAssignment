using System.Text.Json.Serialization;

namespace FundaAssignment.Infrastructure.Funda.Contracts;

internal sealed record PagingPayload
{
    [JsonPropertyName("AantalPaginas")]
    public int? AantalPaginas { get; init; }

    [JsonPropertyName("HuidigePagina")]
    public int? HuidigePagina { get; init; }
}

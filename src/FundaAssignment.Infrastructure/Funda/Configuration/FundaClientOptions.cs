using System.ComponentModel.DataAnnotations;

namespace FundaAssignment.Infrastructure.Funda.Configuration;

public sealed class FundaClientOptions
{
    public const string SectionName = "Funda:Client";

    /// <summary>
    /// Supplied from the environment rather than from appsettings.json.
    /// </summary>
    [Required(ErrorMessage =
        "No funda API key. Set the environment variable Funda__Client__ApiKey (two underscores between each part).")]
    public required string ApiKey { get; init; }

    public Uri BaseAddress { get; init; } = new("http://partnerapi.funda.nl/feeds/Aanbod.svc/json/");

    [Range(1, 500)]
    public int PageSize { get; init; } = 25;

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

using System.ComponentModel.DataAnnotations;

namespace FundaAssignment.Infrastructure.Funda.Configuration;

public sealed class FundaResilienceOptions
{
    public const string SectionName = "Funda:Resilience";

    [Range(1, 100)]
    public int RequestsPerMinute { get; init; } = 90;

    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 3;
}

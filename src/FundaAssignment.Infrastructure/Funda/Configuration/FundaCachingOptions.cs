using System.ComponentModel.DataAnnotations;

namespace FundaAssignment.Infrastructure.Funda.Configuration;

public sealed class FundaCachingOptions
{
    public const string SectionName = "Funda:Caching";

    public bool Enabled { get; init; } = true;

    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan SnapshotWindow { get; init; } = TimeSpan.FromMinutes(15);
}

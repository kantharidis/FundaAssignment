using FundaAssignment.Cli.Rendering;

namespace FundaAssignment.EndToEnd.Tests.Rendering;

/// <summary>
/// The sentence printed after a ranking, saying where its pages came from.
/// </summary>
public sealed class CacheSummaryTests
{
    [Fact]
    public void Says_when_every_page_came_from_the_cache()
    {
        var summary = CacheSummary.For(hits: 187, misses: 0);

        Assert.Contains("187", summary, StringComparison.Ordinal);
        Assert.Contains("cache", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fetched", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Says_how_the_two_split_when_some_of_each()
    {
        var summary = CacheSummary.For(hits: 175, misses: 12);

        Assert.Contains("175", summary, StringComparison.Ordinal);
        Assert.Contains("12", summary, StringComparison.Ordinal);
        Assert.Contains("funda", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Says_nothing_when_no_page_was_asked_for() =>
        Assert.Null(CacheSummary.For(hits: 0, misses: 0));
}

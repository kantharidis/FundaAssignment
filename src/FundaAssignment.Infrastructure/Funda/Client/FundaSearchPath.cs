using System.Text;
using FundaAssignment.Domain.Models;

namespace FundaAssignment.Infrastructure.Funda.Client;

internal static class FundaSearchPath
{
    internal static string From(SearchSpecification search)
    {
        ArgumentNullException.ThrowIfNull(search);

        var path = new StringBuilder("/").Append(Segment(search.City)).Append('/');

        if (search.Features != ListingFeatures.None)
        {
            path.Append(Segment(NameOf(search.Features))).Append('/');
        }

        return path.ToString();
    }

    private static string NameOf(ListingFeatures feature) => feature switch
    {
        ListingFeatures.Garden => "tuin",
        _ => throw new NotSupportedException($"No funda search term is known for {feature}."),
    };

    private static string Segment(string value) =>
        Uri.EscapeDataString(string.Join('-', value.ToLowerInvariant().Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
}

using System.Text.Json.Serialization;
using FundaAssignment.Infrastructure.Funda.Client;

namespace FundaAssignment.Infrastructure.Funda.Caching;

[JsonSerializable(typeof(FeedPage))]
internal sealed partial class FundaCacheJsonContext : JsonSerializerContext;

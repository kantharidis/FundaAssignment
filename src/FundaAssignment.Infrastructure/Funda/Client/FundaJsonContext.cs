using System.Text.Json.Serialization;
using FundaAssignment.Infrastructure.Funda.Contracts;

namespace FundaAssignment.Infrastructure.Funda.Client;

[JsonSerializable(typeof(FeedPageResponse))]
internal sealed partial class FundaJsonContext : JsonSerializerContext;

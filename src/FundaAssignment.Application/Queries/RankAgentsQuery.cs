using FundaAssignment.Domain.Models;

namespace FundaAssignment.Application.Queries;

public sealed record RankAgentsQuery(SearchSpecification Search, int TopCount);

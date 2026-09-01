namespace FundaAssignment.Architecture.Tests;

/// <summary>
/// The dependency rule, expressed as a table. Anything not listed is forbidden.
/// </summary>
public sealed class ProjectReferenceRuleTests
{
    private const string Domain = "FundaAssignment.Domain";
    private const string Application = "FundaAssignment.Application";
    private const string Infrastructure = "FundaAssignment.Infrastructure";
    private const string Cli = "FundaAssignment.Cli";

    private static readonly Dictionary<string, string[]> AllowedReferences = new(StringComparer.Ordinal)
    {
        [Domain] = [],
        [Application] = [Domain],
        [Infrastructure] = [Application, Domain],
        [Cli] = [Application, Infrastructure, Domain],

        ["FundaAssignment.Domain.UnitTests"] = [Domain],
        ["FundaAssignment.Application.UnitTests"] = [Application, Domain],
        ["FundaAssignment.Infrastructure.Tests"] = [Infrastructure, Application, Domain],
        ["FundaAssignment.EndToEnd.Tests"] = [Cli, Infrastructure, Application, Domain],

        // Reflecting over the layers is this project's entire job.
        ["FundaAssignment.Architecture.Tests"] = [Cli, Infrastructure, Application, Domain],
    };

    public static TheoryData<string> AllProjects()
    {
        var data = new TheoryData<string>();
        foreach (var project in SolutionLayout.Projects)
        {
            data.Add(project.Name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void Project_only_references_what_the_dependency_table_allows(string projectName)
    {
        Assert.True(
            AllowedReferences.ContainsKey(projectName),
            $"'{projectName}' is not in the dependency table. Add it to AllowedReferences with the "
            + "references it is permitted, so the rule stays a deliberate decision.");

        var project = SolutionLayout.Projects.Single(candidate => candidate.Name == projectName);
        var forbidden = project.ProjectReferences.Except(AllowedReferences[projectName], StringComparer.Ordinal).ToArray();

        Assert.True(
            forbidden.Length == 0,
            $"'{projectName}' references {string.Join(", ", forbidden)}, which the dependency table forbids.");
    }

    [Fact]
    public void Domain_has_no_dependencies_at_all()
    {
        var domain = SolutionLayout.Projects.Single(project => project.Name == Domain);

        Assert.Empty(domain.ProjectReferences);
    }

    [Fact]
    public void Every_project_is_listed_in_the_solution()
    {
        var missing = SolutionLayout.Projects
            .Where(project => !SolutionLayout.SolutionProjectPaths.Contains(project.RelativePath, StringComparer.OrdinalIgnoreCase))
            .Select(project => project.RelativePath)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"Projects on disk but not in the solution: {string.Join(", ", missing)}. "
            + "A project outside the solution is never built and never tested.");
    }

}

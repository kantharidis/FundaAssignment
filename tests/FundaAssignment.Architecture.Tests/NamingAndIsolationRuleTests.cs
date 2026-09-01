using System.Reflection;
using System.Runtime.CompilerServices;

namespace FundaAssignment.Architecture.Tests;

public sealed class NamingAndIsolationRuleTests
{
    private const string DomainAssembly = "FundaAssignment.Domain";
    private const string InfrastructureAssembly = "FundaAssignment.Infrastructure";

    private static readonly string[] LayerAssemblies =
    [
        DomainAssembly,
        "FundaAssignment.Application",
        InfrastructureAssembly,
        "FundaAssignment.Cli",
    ];

    [Theory]
    [InlineData("Payload")]
    [InlineData("Response")]
    public void Wire_contract_types_are_internal_and_live_in_Infrastructure(string suffix)
    {
        var offenders = AuthoredTypes()
            .Where(candidate => candidate.Type.Name.EndsWith(suffix, StringComparison.Ordinal))
            .Where(candidate => candidate.Type.IsPublic || candidate.Assembly != InfrastructureAssembly)
            .Select(candidate => $"{candidate.Assembly}::{candidate.Type.FullName}")
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Types ending in '{suffix}' must be internal to {InfrastructureAssembly}: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Domain_does_not_depend_on_serialization_or_transport()
    {
        string[] forbidden = ["System.Text.Json", "System.Net.Http", "Microsoft.Extensions.DependencyInjection"];

        var violations = Load(DomainAssembly)
            .GetReferencedAssemblies()
            .Select(name => name.Name)
            .OfType<string>()
            .Intersect(forbidden, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{DomainAssembly} must stay free of infrastructure concerns but references: {string.Join(", ", violations)}");
    }

    [Fact]
    public void The_cli_names_the_funda_adapter_nowhere_but_its_registration()
    {
        const string AdapterNamespace = "FundaAssignment.Infrastructure.Funda";

        var offenders = Directory
            .EnumerateFiles(Path.Combine(SolutionLayout.Root, "src", "FundaAssignment.Cli"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains(AdapterNamespace, StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(SolutionLayout.Root, path))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"The CLI names {AdapterNamespace} in {string.Join(", ", offenders)}. Reach for an "
            + "Application port instead - the CLI's only permitted mention of funda is AddFundaFeed.");
    }

    /// <summary>Types we wrote, excluding everything the compiler synthesises.</summary>
    private static IEnumerable<(string Assembly, Type Type)> AuthoredTypes() =>
        LayerAssemblies
            .SelectMany(name => Load(name).GetTypes().Select(type => (Assembly: name, Type: type)))
            .Where(candidate => !IsCompilerGenerated(candidate.Type))
            .Where(candidate => candidate.Type.Namespace?.StartsWith("System.", StringComparison.Ordinal) != true)
            .Where(candidate => candidate.Type.Namespace?.StartsWith("Microsoft.", StringComparison.Ordinal) != true);

    /// <summary>True for a compiler-generated type, including anything nested inside one.</summary>
    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        || type.Name.StartsWith('<')
        || (type.DeclaringType is { } declaring && IsCompilerGenerated(declaring));

    private static Assembly Load(string assemblyName) =>
        Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll"));
}

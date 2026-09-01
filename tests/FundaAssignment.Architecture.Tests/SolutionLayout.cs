using System.Xml.Linq;

namespace FundaAssignment.Architecture.Tests;

/// <summary>
/// Reads the solution's project files from disk, as XML rather than as compiled metadata.
/// </summary>
internal static class SolutionLayout
{
    private const string SolutionFileName = "FundaAssignment.slnx";

    internal static string Root { get; } = FindRoot();

    internal static IReadOnlyList<ProjectFile> Projects { get; } =
    [
        .. Directory
            .EnumerateFiles(Root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(ProjectFile.Load)
            .OrderBy(project => project.Name, StringComparer.Ordinal)
    ];

    internal static IReadOnlyList<string> SolutionProjectPaths { get; } =
    [
        .. XDocument
            .Load(Path.Combine(Root, SolutionFileName))
            .Descendants("Project")
            .Select(element => (string?)element.Attribute("Path"))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!.Replace('\\', '/'))
    ];

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not locate {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}

internal sealed record ProjectFile(
    string Name,
    string RelativePath,
    IReadOnlyList<string> ProjectReferences)
{
    internal static ProjectFile Load(string absolutePath)
    {
        var projectReferences = XDocument
            .Load(absolutePath)
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => include is not null)
            .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return new ProjectFile(
            Path.GetFileNameWithoutExtension(absolutePath),
            Path.GetRelativePath(SolutionLayout.Root, absolutePath).Replace('\\', '/'),
            projectReferences);
    }
}

namespace FundaAssignment.Infrastructure.Tests.Support;

/// <summary>
/// Reads the recorded responses in Fixtures/.
/// </summary>
internal static class Fixture
{
    internal static Stream OpenRead(string fileName) => File.OpenRead(PathTo(fileName));

    internal static string ReadAllText(string fileName) => File.ReadAllText(PathTo(fileName));

    private static string PathTo(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
}

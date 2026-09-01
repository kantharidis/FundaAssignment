using FundaAssignment.Cli.Hosting;
using Microsoft.Extensions.Configuration;

namespace FundaAssignment.EndToEnd.Tests.Hosting;

/// <summary>
/// Where the log file goes.
/// </summary>
public sealed class LogFileTests
{
    private const string BaseDirectory = @"C:\tools\funda-rank\";

    [Fact]
    public void Writes_beside_the_executable_when_nothing_is_configured() =>
        Assert.Equal(
            Path.Combine(BaseDirectory, "logs", "funda-rank-.log"),
            LogFile.PathFrom(Configuration(), BaseDirectory));

    [Fact]
    public void Resolves_a_relative_path_against_the_executable() =>
        Assert.Equal(
            Path.Combine(BaseDirectory, "diagnostics", "run-.log"),
            LogFile.PathFrom(Configuration(("Logging:File:Path", @"diagnostics\run-.log")), BaseDirectory));

    [Fact]
    public void Leaves_an_absolute_path_alone() =>
        Assert.Equal(
            @"D:\logs\funda-.log",
            LogFile.PathFrom(Configuration(("Logging:File:Path", @"D:\logs\funda-.log")), BaseDirectory));

    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting => new KeyValuePair<string, string?>(setting.Key, setting.Value)))
            .Build();
}

using Microsoft.Extensions.Configuration;

namespace FundaAssignment.Cli.Hosting;

/// <summary>
/// Where the run writes its log (example funda-rank-20260829.log).
/// </summary>
internal static class LogFile
{
    private const string SettingKey = "Logging:File:Path";

    private static readonly string DefaultPath = Path.Combine("logs", "funda-rank-.log");

    internal static string PathFrom(IConfiguration configuration, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var configured = configuration[SettingKey];

        var path = string.IsNullOrWhiteSpace(configured) ? DefaultPath : configured.Trim();

        return Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path);
    }
}

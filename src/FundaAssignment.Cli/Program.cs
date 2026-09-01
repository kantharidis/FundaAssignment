using FundaAssignment.Application.Handlers;
using FundaAssignment.Application.Ports;
using FundaAssignment.Cli.Hosting;
using FundaAssignment.Cli.Menu;
using FundaAssignment.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddFundaFeed(builder.Configuration);
builder.Services.AddSingleton<RankAgentsHandler>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole(console => console.LogToStandardErrorThreshold = LogLevel.Trace);

// The request URI carries the API key, and Serilog does not honour this filter - see the
// MinimumLevel.Override below, which has to say the same thing again.
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

var logFilePath = LogFile.PathFrom(builder.Configuration, AppContext.BaseDirectory);

builder.Logging.AddSerilog(
    new LoggerConfiguration()
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
        .CreateLogger(),
    dispose: true);

using var host = builder.Build();

await Console.Error.WriteLineAsync($"Logging to {logFilePath}");

using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, pressed) =>
{
    pressed.Cancel = true;
    cancellation.Cancel();
};

try
{
    var handler = host.Services.GetRequiredService<RankAgentsHandler>();

    if (Console.IsInputRedirected)
    {
        await RankingReport.WriteAsync(handler, Console.Out, cancellation.Token);
    }
    else
    {
        await RankingMenu.RunAsync(
            handler,
            host.Services.GetRequiredService<IListingSourceStatistics>(),
            Console.In,
            Console.Out,
            Console.Error,
            cancellation.Token);
    }

    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    await Console.Error.WriteLineAsync("Cancelled.");

    return 130;
}
catch (OptionsValidationException failure)
{
    await Console.Error.WriteLineAsync($"Configuration is not usable: {string.Join("; ", failure.Failures)}");

    return 2;
}
catch (ListingSourceUnavailableException failure)
{
    await Console.Error.WriteLineAsync($"The listings could not be read: {failure.Message}");

    return 1;
}
catch (Exception failure)
{
    host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FundaAssignment.Cli")
        .LogError(failure, "The run ended unexpectedly.");

    await Console.Error.WriteLineAsync($"The run ended unexpectedly: {failure.Message}");

    return 1;
}

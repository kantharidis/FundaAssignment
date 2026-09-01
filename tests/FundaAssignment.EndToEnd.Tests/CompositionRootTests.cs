using FundaAssignment.Application.Handlers;
using FundaAssignment.Application.Ports;
using FundaAssignment.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FundaAssignment.EndToEnd.Tests;

/// <summary>
/// The composition root, resolved without a socket.
/// </summary>
public sealed class CompositionRootTests
{
    [Fact]
    public void Builds_everything_a_run_needs()
    {
        using var services = Provider(("Funda:Client:ApiKey", "test-key"));

        Assert.NotNull(services.GetRequiredService<RankAgentsHandler>());
        Assert.NotNull(services.GetRequiredService<IListingSource>());
    }

    [Fact]
    public void Hands_out_one_listing_source_for_the_whole_run()
    {
        using var services = Provider(("Funda:Client:ApiKey", "test-key"));

        Assert.Same(services.GetRequiredService<IListingSource>(), services.GetRequiredService<IListingSource>());
    }

    [Fact]
    public void Refuses_to_run_without_an_api_key()
    {
        using var services = Provider();

        Assert.Throws<OptionsValidationException>(services.GetRequiredService<IListingSource>);
    }

    private static ServiceProvider Provider(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting => new KeyValuePair<string, string?>(setting.Key, setting.Value)))
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddDistributedMemoryCache()
            .AddFundaFeed(configuration)
            .AddSingleton<RankAgentsHandler>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}

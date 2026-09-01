using FundaAssignment.Application.Ports;
using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Caching;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundaAssignment.Infrastructure.Tests;

/// <summary>
/// The shape of the registered client chain, checked from inside Infrastructure.
/// </summary>
public sealed class FundaServiceCollectionExtensionsTests
{
    [Fact]
    public void Hands_out_a_client_with_the_cache_outermost()
    {
        using var services = Provider();

        Assert.IsType<CachingFundaFeedClient>(services.GetRequiredService<IFundaFeedClient>());
    }

    [Fact]
    public void Never_hands_out_a_single_link_of_the_chain()
    {
        using var services = Provider();

        Assert.Null(services.GetService<FundaFeedClient>());
        Assert.Null(services.GetService<ResilientFundaFeedClient>());
    }

    [Fact]
    public void Reports_the_cache_counters_through_the_application_port()
    {
        using var services = Provider();

        Assert.Same(
            services.GetRequiredService<FundaCacheStatistics>(),
            services.GetRequiredService<IListingSourceStatistics>());
    }

    private static ServiceProvider Provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Funda:Client:ApiKey", "test-key")])
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddDistributedMemoryCache()
            .AddFundaFeed(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}

using FundaAssignment.Application.Ports;
using FundaAssignment.Infrastructure.Funda;
using FundaAssignment.Infrastructure.Funda.Caching;
using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Configuration;
using FundaAssignment.Infrastructure.Funda.Resilience;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundaAssignment.Infrastructure;

/// <summary>
/// Registers the funda feed as the application's listing source.
/// </summary>
public static class FundaServiceCollectionExtensions
{
    private const string HttpClientName = "funda";

    /// <summary>
    /// Keys for the inner links of the client chain.
    /// </summary>
    private const string Transport = "funda:transport";

    private const string Resilient = "funda:resilient";

    public static IServiceCollection AddFundaFeed(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        Bind<FundaClientOptions>(services, configuration, FundaClientOptions.SectionName);
        Bind<FundaResilienceOptions>(services, configuration, FundaResilienceOptions.SectionName);
        Bind<FundaCachingOptions>(services, configuration, FundaCachingOptions.SectionName);

        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient(HttpClientName, (provider, client) =>
        {
            var options = provider.GetRequiredService<FundaClientOptions>();

            client.BaseAddress = options.BaseAddress;
            client.Timeout = options.RequestTimeout;
        });

        services.TryAddSingleton(_ => new FundaCacheStatistics());
        services.TryAddSingleton<IListingSourceStatistics>(
            provider => provider.GetRequiredService<FundaCacheStatistics>());

        // cache -> retry (with rate limiter) -> socket, innermost first.
        services.AddKeyedSingleton<IFundaFeedClient>(Transport, (provider, _) => new FundaFeedClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            provider.GetRequiredService<FundaClientOptions>()));

        services.AddKeyedSingleton<IFundaFeedClient>(Resilient, (provider, _) => new ResilientFundaFeedClient(
            provider.GetRequiredKeyedService<IFundaFeedClient>(Transport),
            provider.GetRequiredService<FundaResilienceOptions>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<ResilientFundaFeedClient>>()));

        services.AddSingleton<IFundaFeedClient>(provider => new CachingFundaFeedClient(
            provider.GetRequiredKeyedService<IFundaFeedClient>(Resilient),
            provider.GetRequiredService<IDistributedCache>(),
            provider.GetRequiredService<FundaCachingOptions>(),
            provider.GetRequiredService<FundaCacheStatistics>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<CachingFundaFeedClient>>()));

        services.AddSingleton<IListingSource, FundaListingSource>();

        return services;
    }

    /// <summary>
    /// Binds one section, validates it, and registers the bound object itself.
    /// </summary>
    private static void Bind<TOptions>(IServiceCollection services, IConfiguration configuration, string section)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(provider => provider.GetRequiredService<IOptions<TOptions>>().Value);
    }
}

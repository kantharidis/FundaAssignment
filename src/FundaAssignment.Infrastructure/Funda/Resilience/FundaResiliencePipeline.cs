using System.Threading.RateLimiting;
using FundaAssignment.Infrastructure.Funda.Client;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace FundaAssignment.Infrastructure.Funda.Resilience;

internal static partial class FundaResiliencePipeline
{
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(1);

    internal static ResiliencePipeline<FeedPage> Build(
        RateLimiter limiter,
        int maxRetryAttempts,
        TimeProvider timeProvider,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(limiter);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetryAttempts);

        return new ResiliencePipelineBuilder<FeedPage> { TimeProvider = timeProvider }
            .AddRetry(new RetryStrategyOptions<FeedPage>
            {
                ShouldHandle = new PredicateBuilder<FeedPage>()
                    .Handle<HttpRequestException>()
                    .Handle<FundaRejectedRequestException>(),
                MaxRetryAttempts = maxRetryAttempts,
                Delay = FirstRetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                OnRetry = arguments =>
                {
                    LogRetry(
                        logger,
                        arguments.AttemptNumber + 1,
                        arguments.RetryDelay,
                        arguments.Outcome.Exception?.Message ?? "no reason given");

                    return default;
                },
            })
            .AddRateLimiter(limiter)
            .Build();
    }

    [LoggerMessage(
        EventId = 510,
        Level = LogLevel.Warning,
        Message = "Funda request failed on attempt {AttemptNumber}, retrying in {RetryDelay}: {Reason}")]
    private static partial void LogRetry(ILogger logger, int attemptNumber, TimeSpan retryDelay, string reason);
}

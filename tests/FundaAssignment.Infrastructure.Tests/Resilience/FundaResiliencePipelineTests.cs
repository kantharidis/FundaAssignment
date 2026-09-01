using FundaAssignment.Infrastructure.Funda.Client;
using FundaAssignment.Infrastructure.Funda.Resilience;
using FundaAssignment.Infrastructure.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Polly;
using Polly.RateLimiting;

using System.Threading.RateLimiting;

namespace FundaAssignment.Infrastructure.Tests.Resilience;

/// <summary>
/// The retry and rate limiting strategies, over a callback rather than a feed client.
/// </summary>
public sealed class FundaResiliencePipelineTests
{
    private static readonly FeedPage Page = new([], PageCount: 1, TotalListings: 0, SkippedListings: 0);

    private readonly FakeTimeProvider clock = new();

    [Fact]
    public async Task Retries_a_refused_request_until_it_is_accepted()
    {
        var attempts = 0;

        var page = await RunAsync(PipelineOf(Unlimited()), _ =>
        {
            attempts++;

            return attempts < 3
                ? throw new FundaRejectedRequestException("funda refused the request.")
                : Page;
        });

        Assert.Same(Page, page);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Waits_twice_as_long_before_every_further_attempt_and_then_gives_up()
    {
        var attempts = 0;
        var startedAt = clock.GetUtcNow();

        await Assert.ThrowsAsync<FundaRejectedRequestException>(() =>
            RunAsync(PipelineOf(Unlimited(), maxRetryAttempts: 3), _ =>
            {
                attempts++;

                throw new FundaRejectedRequestException("funda refused the request.");
            }));

        Assert.Equal(4, attempts);

        // The clock is moved in 250ms steps, so the last step can overshoot by that much.
        Assert.InRange(clock.GetUtcNow() - startedAt, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(7.25));
    }

    [Fact]
    public async Task Spends_a_token_on_every_attempt_including_a_retried_one()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<RateLimiterRejectedException>(() =>
            RunAsync(PipelineOf(AllowanceOf(1)), _ =>
            {
                attempts++;

                throw new FundaRejectedRequestException("funda refused the request.");
            }));

        Assert.Equal(1, attempts);
    }

    private ResiliencePipeline<FeedPage> PipelineOf(RateLimiter limiter, int maxRetryAttempts = 3) =>
        FundaResiliencePipeline.Build(limiter, maxRetryAttempts, clock, NullLogger.Instance);

    /// <summary>Runs the pipeline against the test's own clock, so no backoff is waited out.</summary>
    private Task<FeedPage> RunAsync(ResiliencePipeline<FeedPage> pipeline, Func<CancellationToken, FeedPage> work) =>
        ClockPump.RunAsync(
            clock,
            pipeline
                .ExecuteAsync(token => ValueTask.FromResult(work(token)), TestContext.Current.CancellationToken)
                .AsTask(),
            TestContext.Current.CancellationToken);

    private static RateLimiter AllowanceOf(int tokens) =>
        new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = tokens,

            // Refills once an hour and only when asked to, so nothing is handed back mid-test.
            TokensPerPeriod = tokens,
            QueueLimit = 0,
            ReplenishmentPeriod = TimeSpan.FromHours(1),
            AutoReplenishment = false,
        });

    /// <summary>A limiter that never refuses, for the tests that are about retrying.</summary>
    private static RateLimiter Unlimited() =>
        new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = int.MaxValue,
            TokensPerPeriod = int.MaxValue,
            QueueLimit = int.MaxValue,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = false,
        });
}

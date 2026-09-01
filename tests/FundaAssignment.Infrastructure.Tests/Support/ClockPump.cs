using Microsoft.Extensions.Time.Testing;

namespace FundaAssignment.Infrastructure.Tests.Support;

/// <summary>
/// Moves a fake clock forward in small steps until the work being timed finishes.
/// </summary>
internal static class ClockPump
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(250);

    internal static async Task<TResult> RunAsync<TResult>(
        FakeTimeProvider clock,
        Task<TResult> execution,
        CancellationToken cancellationToken)
    {
        for (var step = 0; step < 200 && !execution.IsCompleted; step++)
        {
            clock.Advance(Step);
            await Task.Delay(1, cancellationToken);
        }

        return await execution;
    }
}

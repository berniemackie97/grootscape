using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Grootscape.Server.World;

/// <summary>Reports ready while the world loop keeps ticking.</summary>
internal sealed class WorldLoopHealthCheck(WorldLoop loop) : IHealthCheck
{
    /// <summary>The number of tick durations without a tick before the world counts as stalled.</summary>
    private const int StallThresholdTicks = 3;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        HealthCheckResult result = loop.TimeSinceLastTick switch
        {
            null => HealthCheckResult.Unhealthy("The world has not completed its first tick."),
            TimeSpan since when since > loop.TickDuration * StallThresholdTicks =>
                HealthCheckResult.Unhealthy($"The world has not ticked for {since.TotalMilliseconds:F0} ms."),
            _ => HealthCheckResult.Healthy(),
        };
        return Task.FromResult(result);
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Grootscape.Server.World;

internal sealed class WorldLoopHealthCheck(WorldLoop loop) : IHealthCheck
{
    private const int StallThresholdTicks = 3;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        HealthCheckResult result = loop.TimeSinceLastTick switch
        {
            null => HealthCheckResult.Unhealthy("The world has not completed its first tick."),
            { } since when since > loop.TickDuration * StallThresholdTicks
                => HealthCheckResult.Unhealthy($"The world has not ticked for {since.TotalMilliseconds:F0} ms."),
            _ => HealthCheckResult.Healthy(),
        };
        return Task.FromResult(result);
    }
}

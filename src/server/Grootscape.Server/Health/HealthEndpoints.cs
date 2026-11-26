using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Grootscape.Server.Health;

/// <summary>Liveness and readiness probes for orchestrators and load balancers.</summary>
internal static class HealthEndpoints
{
    /// <summary>Tag for checks that must pass before the server accepts players.</summary>
    public const string ReadyTag = "ready";

    public static IServiceCollection AddGrootscapeHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Liveness runs no checks: the process answering is the signal.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
        });
    }
}

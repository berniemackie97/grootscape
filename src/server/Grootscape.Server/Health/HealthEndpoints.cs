using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Grootscape.Server.Health;

internal static class HealthEndpoints
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddGrootscapeHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
        });
    }
}

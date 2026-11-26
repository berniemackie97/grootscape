using Grootscape.Server.Health;
using Grootscape.World;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grootscape.Server.World;

internal static class WorldServiceCollectionExtensions
{
    public static IServiceCollection AddWorld(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WorldOptions>()
            .Bind(configuration.GetSection(WorldOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<GameWorld>();
        services.AddSingleton<WorldLoop>();
        services.AddHostedService(provider => provider.GetRequiredService<WorldLoop>());
        services.AddHealthChecks().AddCheck<WorldLoopHealthCheck>("world", tags: [HealthEndpoints.ReadyTag]);
        return services;
    }
}

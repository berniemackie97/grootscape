using Microsoft.Extensions.Options;

namespace Grootscape.Server.Network;

/// <summary>The WebSocket endpoint that every client connects to. See <c>protocol/README.md</c>.</summary>
internal static class PlayEndpoint
{
    public const string Path = "/play";

    public static IServiceCollection AddNetwork(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NetworkOptions>()
            .Bind(configuration.GetSection(NetworkOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<PlaySessionHandler>();
        return services;
    }

    public static void MapPlayEndpoint(this WebApplication app)
    {
        NetworkOptions options = app.Services.GetRequiredService<IOptions<NetworkOptions>>().Value;
        WebSocketOptions webSockets = new() { KeepAliveInterval = TimeSpan.FromSeconds(options.KeepAliveIntervalSeconds) };
        foreach (string origin in options.AllowedOrigins)
        {
            webSockets.AllowedOrigins.Add(origin);
        }

        app.UseWebSockets(webSockets);
        app.Map(Path, (HttpContext context, PlaySessionHandler handler) => handler.HandleAsync(context));
    }
}

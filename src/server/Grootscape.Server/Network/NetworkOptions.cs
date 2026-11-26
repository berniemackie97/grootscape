using System.ComponentModel.DataAnnotations;

namespace Grootscape.Server.Network;

/// <summary>Settings for client connections, bound from the <c>Network</c> configuration section.</summary>
internal sealed class NetworkOptions
{
    public const string SectionName = "Network";

    /// <summary>How long a new connection may take to send its <c>ClientHello</c>.</summary>
    [Range(100, 60_000)]
    public int HandshakeTimeoutMs { get; set; } = 5_000;

    /// <summary>How long the server waits for a client to acknowledge a close before dropping it.</summary>
    [Range(100, 30_000)]
    public int CloseTimeoutMs { get; set; } = 2_000;

    /// <summary>The WebSocket keep-alive ping interval.</summary>
    [Range(1, 300)]
    public int KeepAliveIntervalSeconds { get; set; } = 30;

    /// <summary>Browser origins allowed to connect. Empty allows any origin.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    public TimeSpan HandshakeTimeout => TimeSpan.FromMilliseconds(HandshakeTimeoutMs);

    public TimeSpan CloseTimeout => TimeSpan.FromMilliseconds(CloseTimeoutMs);
}

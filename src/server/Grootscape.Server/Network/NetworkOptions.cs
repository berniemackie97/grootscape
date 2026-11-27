using System.ComponentModel.DataAnnotations;

namespace Grootscape.Server.Network;

internal sealed class NetworkOptions
{
    public const string SectionName = "Network";

    [Range(100, 60_000)]
    public int HandshakeTimeoutMs { get; set; } = 5_000;

    [Range(100, 30_000)]
    public int CloseTimeoutMs { get; set; } = 2_000;

    [Range(1, 300)]
    public int KeepAliveIntervalSeconds { get; set; } = 30;

    public string[] AllowedOrigins { get; set; } = [];

    public TimeSpan HandshakeTimeout => TimeSpan.FromMilliseconds(HandshakeTimeoutMs);

    public TimeSpan CloseTimeout => TimeSpan.FromMilliseconds(CloseTimeoutMs);
}

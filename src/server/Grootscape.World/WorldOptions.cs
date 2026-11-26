using System.ComponentModel.DataAnnotations;

namespace Grootscape.World;

/// <summary>Settings for the world simulation, bound from the <c>World</c> configuration section.</summary>
public sealed class WorldOptions
{
    public const string SectionName = "World";

    /// <summary>The length of one world tick in milliseconds.</summary>
    /// <remarks>The upper bound keeps the value representable in the protocol's 16-bit field.</remarks>
    [Range(50, 5000)]
    public int TickDurationMs { get; set; } = 600;

    public TimeSpan TickDuration => TimeSpan.FromMilliseconds(TickDurationMs);
}

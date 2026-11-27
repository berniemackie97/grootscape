using System.ComponentModel.DataAnnotations;

namespace Grootscape.World;

public sealed class WorldOptions
{
    public const string SectionName = "World";
    
    [Range(50, 5000)]
    public int TickDurationMs { get; set; } = 600;

    public TimeSpan TickDuration => TimeSpan.FromMilliseconds(TickDurationMs);
}

namespace Grootscape.World;

/// <summary>The authoritative world state.</summary>
/// <remarks>
/// The world loop is the only caller. It advances one tick at a time, so the type needs no locking.
/// Game rules join this type as they land and stay callable without hosting, sockets, or storage.
/// </remarks>
public sealed class GameWorld
{
    /// <summary>The number of ticks completed since the world started.</summary>
    public long CurrentTick { get; private set; }

    /// <summary>Completes one tick.</summary>
    public void AdvanceTick() => CurrentTick++;
}

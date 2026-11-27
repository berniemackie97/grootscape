namespace Grootscape.World;

public sealed class GameWorld
{
    public long CurrentTick
    {
        get; private set;
    }

    public void AdvanceTick() => CurrentTick++;
}

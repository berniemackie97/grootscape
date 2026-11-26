using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Grootscape.World.Tests;

public sealed class GameWorldTests
{
    [Fact]
    public void StartsBeforeTheFirstTick() => Assert.Equal(0, new GameWorld().CurrentTick);

    [Fact]
    public void AdvancesOneTickAtATime()
    {
        GameWorld world = new();

        world.AdvanceTick();
        world.AdvanceTick();

        Assert.Equal(2, world.CurrentTick);
    }
}

public sealed class WorldOptionsTests
{
    [Theory]
    [InlineData(50, true)]
    [InlineData(600, true)]
    [InlineData(5000, true)]
    [InlineData(49, false)]
    [InlineData(5001, false)]
    public void BoundsTickDuration(int tickDurationMs, bool valid)
    {
        WorldOptions options = new() { TickDurationMs = tickDurationMs };

        bool result = Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true);

        Assert.Equal(valid, result);
        Assert.Equal(TimeSpan.FromMilliseconds(tickDurationMs), options.TickDuration);
    }
}

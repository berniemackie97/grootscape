using Grootscape.Server.Observability;
using Grootscape.World;
using Microsoft.Extensions.Options;

namespace Grootscape.Server.World;

internal sealed partial class WorldLoop(GameWorld world, IOptions<WorldOptions> options, TimeProvider time, ServerMetrics metrics, ILogger<WorldLoop> logger)
    : BackgroundService
{
    private long lastTickTimestamp;

    public TimeSpan TickDuration => options.Value.TickDuration;

    public TimeSpan? TimeSinceLastTick
    {
        get
        {
            long last = Interlocked.Read(ref lastTickTimestamp);
            return last == 0 ? null : time.GetElapsedTime(last);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan tickDuration = options.Value.TickDuration;
        LogStarted(tickDuration.TotalMilliseconds);
        using PeriodicTimer timer = new(tickDuration, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                long started = time.GetTimestamp();
                world.AdvanceTick();
                TimeSpan elapsed = time.GetElapsedTime(started);
                bool overrun = elapsed > tickDuration;
                if (overrun)
                {
                    LogOverrun(world.CurrentTick, elapsed.TotalMilliseconds);
                }

                metrics.TickCompleted(elapsed, overrun);
                Interlocked.Exchange(ref lastTickTimestamp, time.GetTimestamp());
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogStopped(world.CurrentTick);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "World loop started with a {TickDurationMs} ms tick")]
    private partial void LogStarted(double tickDurationMs);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Tick {Tick} took {ElapsedMs} ms")]
    private partial void LogOverrun(long tick, double elapsedMs);
    [LoggerMessage(Level = LogLevel.Information, Message = "World loop stopped after tick {Tick}")]
    private partial void LogStopped(long tick);
}

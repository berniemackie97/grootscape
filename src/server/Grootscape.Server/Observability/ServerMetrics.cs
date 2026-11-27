using System.Diagnostics.Metrics;

namespace Grootscape.Server.Observability;

internal sealed class ServerMetrics
{
    public const string MeterName = "Grootscape.Server";

    private readonly Counter<long> ticks;
    private readonly Histogram<double> tickDuration;
    private readonly Counter<long> tickOverruns;
    private readonly UpDownCounter<long> sessions;
    private readonly Counter<long> handshakes;

    public ServerMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);
        ticks = meter.CreateCounter<long>("grootscape.world.ticks", description: "World ticks completed.");
        tickDuration = meter.CreateHistogram<double>("grootscape.world.tick.duration", unit: "s", description: "Time spent processing one world tick.");
        tickOverruns = meter.CreateCounter<long>("grootscape.world.tick.overruns", description: "Ticks that took longer than the tick duration.");
        sessions = meter.CreateUpDownCounter<long>("grootscape.network.sessions", description: "Clients that completed the handshake and are connected.");
        handshakes = meter.CreateCounter<long>("grootscape.network.handshakes", description: "Handshake outcomes, tagged by result.");
    }

    public void TickCompleted(TimeSpan elapsed, bool overrun)
    {
        ticks.Add(1);
        tickDuration.Record(elapsed.TotalSeconds);
        if (overrun)
        {
            tickOverruns.Add(1);
        }
    }

    public void SessionOpened() => sessions.Add(1);
    public void SessionClosed() => sessions.Add(-1);
    public void Handshake(string result) => handshakes.Add(1, new KeyValuePair<string, object?>("result", result));
}

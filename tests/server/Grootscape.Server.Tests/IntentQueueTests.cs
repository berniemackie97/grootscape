using System.Threading;
using System.Threading.Channels;
using System.Diagnostics;
using Xunit;

namespace Grootscape.Server.Tests;

public sealed class IntentQueueTests(ITestOutputHelper output)
{
    [Fact]
    public void BoundedChannelRejectsInputWhenFull()
    {
        BoundedChannelOptions options = new(2)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        };

        Channel<int> channel = Channel.CreateBounded<int>(options);

        Assert.True(channel.Writer.TryWrite(10));
        Assert.True(channel.Writer.TryWrite(20));
        Assert.False(channel.Writer.TryWrite(30));

        Assert.True(channel.Reader.TryRead(out int first));
        Assert.Equal(10, first);

        Assert.True(channel.Reader.TryRead(out int second));
        Assert.Equal(20, second);

        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public void BoundedQueueRejectsInputWhenFull()
    {
        BoundedIntentQueue queue = new(2);

        Assert.True(queue.TryEnqueue(10));
        Assert.True(queue.TryEnqueue(20));
        Assert.False(queue.TryEnqueue(30));

        Assert.True(queue.TryDequeue(out int first));
        Assert.Equal(10, first);

        Assert.True(queue.TryDequeue(out int second));
        Assert.Equal(20, second);

        Assert.False(queue.TryDequeue(out _));
    }

    [Fact]
    public void MeasuresPerSessionChannelIntake()
    {
        const int sessionCount = 1000;
        const int warmupTicks = 100;
        const int measuredTicks = 1000;

        BoundedChannelOptions options = new(240)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        };

        Channel<int>[] sessions = new Channel<int>[sessionCount];

        for (int i = 0; i < sessions.Length; i++)
        {
            sessions[i] = Channel.CreateBounded<int>(options);
        }

        RunTicks(warmupTicks);

        long started = Stopwatch.GetTimestamp();

        RunTicks(measuredTicks);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(started);

        output.WriteLine($"Total duration: {elapsed.TotalMilliseconds:F3} ms");
        output.WriteLine($"Mean tick cost: {elapsed.TotalMilliseconds / measuredTicks:F3} ms");

        void RunTicks(int count)
        {
            for (int tick = 0; tick < count; tick++)
            {
                for (int session = 0; session < sessions.Length; session++)
                {
                    if (!sessions[session].Writer.TryWrite(session))
                    {
                        throw new InvalidOperationException("Intent queue rejected input.");
                    }
                }

                for (int session = 0; session < sessions.Length; session++)
                {
                    if (!sessions[session].Reader.TryRead(out int intent) || intent != session)
                    {
                        throw new InvalidOperationException("Intent queue returned an unexpected value.");
                    }
                }
            }
        }
    }

    [Fact]
    public void MeasuresPerSessionQueueIntake()
    {
        const int sessionCount = 1000;
        const int warmupTicks = 100;
        const int measuredTicks = 1000;

        BoundedIntentQueue[] sessions = new BoundedIntentQueue[sessionCount];

        for (int i = 0; i < sessions.Length; i++)
        {
            sessions[i] = new BoundedIntentQueue(240);
        }

        RunTicks(warmupTicks);

        long started = Stopwatch.GetTimestamp();

        RunTicks(measuredTicks);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(started);

        output.WriteLine($"Total duration: {elapsed.TotalMilliseconds:F3} ms");
        output.WriteLine($"Mean tick cost: {elapsed.TotalMilliseconds / measuredTicks:F3} ms");
        void RunTicks(int count)
        {
            for (int tick = 0; tick < count; tick++)
            {
                for (int session = 0; session < sessions.Length; session++)
                {
                    if (!sessions[session].TryEnqueue(session))
                    {
                        throw new InvalidOperationException("Intent queue rejected input.");
                    }
                }

                for (int session = 0; session < sessions.Length; session++)
                {
                    if (!sessions[session].TryDequeue(out int intent) || intent != session)
                    {
                        throw new InvalidOperationException("Intent queue returned an unexpected value.");
                    }
                }
            }
        }
    }

    [Fact]
    public void MeasuresSharedChannelIntake()
    {
        const int sessionCount = 1000;
        const int warmupTicks = 100;
        const int measuredTicks = 1000;

        BoundedChannelOptions options = new(sessionCount * 240)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        };

        Channel<int> channel = Channel.CreateBounded<int>(options);

        RunTicks(warmupTicks);

        long started = Stopwatch.GetTimestamp();

        RunTicks(measuredTicks);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(started);
        void RunTicks(int count)
        {
            for (int tick = 0; tick < count; tick++)
            {
                for (int session = 0; session < sessionCount; session++)
                {
                    if (!channel.Writer.TryWrite(session))
                    {
                        throw new InvalidOperationException("Intent queue rejected input.");
                    }
                }

                for (int session = 0; session < sessionCount; session++)
                {
                    if (!channel.Reader.TryRead(out int intent) || intent != session)
                    {
                        throw new InvalidOperationException("Intent queue returned an unexpected value.");
                    }
                }
            }
        }

        output.WriteLine($"Total duration: {elapsed.TotalMilliseconds:F3} ms");
        output.WriteLine($"Mean tick cost: {elapsed.TotalMilliseconds / measuredTicks:F3} ms");
    }

    [Fact]
    public void BoundedQueuePreservesCapacityUnderConcurrentWrites()
    {
        const int capacity = 240;
        const int attempts = 1000;

        BoundedIntentQueue queue = new(capacity);
        int accepted = 0;

        Parallel.For(0, attempts, intent =>
        {
            if (queue.TryEnqueue(intent))
            {
                Interlocked.Increment(ref accepted);
            }
        });

        Assert.Equal(capacity, accepted);

        HashSet<int> consumed = [];
        while (queue.TryDequeue(out int intent))
        {
            Assert.InRange(intent,0, attempts - 1);
            Assert.True(consumed.Add(intent));
        }

        Assert.Equal(accepted, consumed.Count);
    }

    private sealed class BoundedIntentQueue(int capacity)
    {
        private readonly Queue<int> _pending = [];
        private readonly Lock _gate = new();

        public bool TryEnqueue(int intent)
        {
            lock (_gate)
            {
                if (_pending.Count >= capacity)
                {
                    return false;
                }

                _pending.Enqueue(intent);
                return true;
            }
        }

        public bool TryDequeue(out int intent)
        {
            lock (_gate)
            {
                return _pending.TryDequeue(out intent);
            }
        }
    }
}

using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Grootscape.Server.Tests;

public sealed class TickSchedulingTests
{
    [Fact]
    public async Task PeriodicTimerRetainsOriginalScheduleAfterOverrun()
    {
        FakeTimeProvider time = new();
        TimeSpan interval = TimeSpan.FromMilliseconds(600);
        CancellationToken ct = TestContext.Current.CancellationToken;

        using PeriodicTimer timer = new(interval, time);

        time.Advance(interval);
        Assert.True(await timer.WaitForNextTickAsync(ct));
        time.Advance(TimeSpan.FromMilliseconds(2500));
        Assert.True(await timer.WaitForNextTickAsync(ct));

        ValueTask<bool> nextTick = timer.WaitForNextTickAsync(ct);
        Assert.False(nextTick.IsCompleted);
        time.Advance(TimeSpan.FromMilliseconds(499));
        Assert.False(nextTick.IsCompleted);

        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(nextTick.IsCompletedSuccessfully);
        Assert.True(await nextTick);
    }

    [Theory]
    [InlineData(600, 600, 600, 1200)]
    [InlineData(600, 850, 600, 1200)]
    [InlineData(600, 1800, 600, 1200)]
    [InlineData(600, 1801, 600, 2401)]
    [InlineData(600, 3100, 600, 3700)]
    public void CalculatesNextDeadline(int expectedDeadlineMs, int actualStartMs, int intervalMs, int expectedResultMs)
    {
        TimeSpan expectedDeadline = TimeSpan.FromMilliseconds(expectedDeadlineMs);
        TimeSpan actualStart = TimeSpan.FromMilliseconds(actualStartMs);
        TimeSpan interval = TimeSpan.FromMilliseconds(intervalMs);
        TimeSpan expectedResult = TimeSpan.FromMilliseconds(expectedResultMs);

        Assert.Equal(expectedResult, CalculateNextDeadline(expectedDeadline, actualStart, interval));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(600)]
    [InlineData(850)]
    public async Task RejectsWaitWhenAlreadyCancelled(int elapsedMs)
    {
        using CancellationTokenSource cancellation = new();
        FakeTimeProvider time = new();
        long origin = time.GetTimestamp();
        TimeSpan deadline = TimeSpan.FromMilliseconds(600);

        time.Advance(TimeSpan.FromMilliseconds(elapsedMs));

        cancellation.Cancel();
        Task wait = WaitUntilDeadlineAsync(time, origin, deadline, cancellation.Token);
        Assert.True(wait.IsCanceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }

    [Fact]
    public async Task WaitsOnlyForRemainingDuration()
    {
        FakeTimeProvider time = new();
        CancellationToken ct = TestContext.Current.CancellationToken;
        long origin = time.GetTimestamp();
        TimeSpan deadline = TimeSpan.FromMilliseconds(600);

        time.Advance(TimeSpan.FromMilliseconds(200));

        Task wait = WaitUntilDeadlineAsync(time, origin, deadline, ct);
        Assert.False(wait.IsCompleted);

        time.Advance(TimeSpan.FromMilliseconds(399));
        Assert.False(wait.IsCompleted);

        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(wait.IsCompletedSuccessfully);

        await wait;
    }

    [Fact]
    public async Task ExpiredDeadlineCompletesInstantly()
    {
        FakeTimeProvider time = new();
        CancellationToken ct = TestContext.Current.CancellationToken;
        long origin = time.GetTimestamp();
        TimeSpan deadline = TimeSpan.FromMilliseconds(600);

        time.Advance(TimeSpan.FromMilliseconds(850));

        Task wait = WaitUntilDeadlineAsync(time, origin, deadline, ct);
        Assert.True(wait.IsCompletedSuccessfully);
        await wait;
    }

    [Fact]
    public async Task CancelsPendingDeadlineWait()
    {
        using CancellationTokenSource cancellation = new();
        FakeTimeProvider time = new();
        long origin = time.GetTimestamp();
        TimeSpan deadline = TimeSpan.FromMilliseconds(600);

        time.Advance(TimeSpan.FromMilliseconds(200));

        Task wait = WaitUntilDeadlineAsync(time, origin, deadline, cancellation.Token);
        Assert.False(wait.IsCompleted);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }

    [Fact]
    public async Task SchedulerExecutesFirstTickAtDeadline()
    {
        using CancellationTokenSource cancellation = new();
        CancellationToken ct = TestContext.Current.CancellationToken;
        FakeTimeProvider time = new();
        TimeSpan interval = TimeSpan.FromMilliseconds(600);
        int ticks = 0;

        Task scheduler = RunSchedulerAsync(time, interval,
            () =>
            {
                ticks++;
                cancellation.Cancel();
            },
            cancellation.Token);

        try
        {
            Assert.Equal(0, ticks);
            time.Advance(TimeSpan.FromMilliseconds(599));
            Assert.Equal(0, ticks);
            time.Advance(TimeSpan.FromMilliseconds(1));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                scheduler.WaitAsync(TimeSpan.FromSeconds(5), ct));
            Assert.True(scheduler.IsCanceled);
            Assert.Equal(1, ticks);
        }
        finally
        {
            cancellation.Cancel();
        }
    }

    [Fact]
    public async Task SchedulerExecutesOverdueTickWithoutAdditionalDelay()
    {
        using CancellationTokenSource cancellation = new();
        CancellationToken ct = TestContext.Current.CancellationToken;
        FakeTimeProvider time = new();
        TimeSpan interval = TimeSpan.FromMilliseconds(600);
        long origin = time.GetTimestamp();


        List<TimeSpan> tickStarts = [];

        Task scheduler = RunSchedulerAsync(time, interval,
            () =>
            {
                tickStarts.Add(time.GetElapsedTime(origin));

                if (tickStarts.Count == 1)
                {
                    time.Advance(TimeSpan.FromMilliseconds(2500));
                }
                else
                {
                    cancellation.Cancel();
                }
            },
            cancellation.Token);

        try
        {
            time.Advance(interval);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                scheduler.WaitAsync(TimeSpan.FromSeconds(5), ct));

            TimeSpan[] expected =
            [
                TimeSpan.FromMilliseconds(600),
                TimeSpan.FromMilliseconds(3100)
            ];

            Assert.Equal(expected, tickStarts);
            Assert.True(scheduler.IsCanceled);
        }
        finally
        {
            cancellation.Cancel();
        }
    }

    private static TimeSpan CalculateNextDeadline(TimeSpan expectedDeadline, TimeSpan actualStart, TimeSpan interval)
    {
        expectedDeadline += interval;

        if (actualStart > expectedDeadline + interval)
        {
            expectedDeadline = actualStart + interval;
        }

        return expectedDeadline;
    }

    private static Task WaitUntilDeadlineAsync(TimeProvider time, long origin, TimeSpan deadline,
        CancellationToken cancellationToken)
    {
        TimeSpan elapsed = time.GetElapsedTime(origin);
        TimeSpan delay = deadline - elapsed;
        return Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, time, cancellationToken);
    }

    private static async Task RunSchedulerAsync(TimeProvider time, TimeSpan interval, Action tick, CancellationToken ct)
    {
        long origin = time.GetTimestamp();
        TimeSpan deadline = interval;
        bool firstTick = true;

        while (true)
        {
            await WaitUntilDeadlineAsync(time, origin, deadline, ct);

            ct.ThrowIfCancellationRequested();

            TimeSpan actualStart = time.GetElapsedTime(origin);

            deadline = firstTick ? actualStart + interval : CalculateNextDeadline(deadline, actualStart, interval);
            firstTick = false;
            tick();
        }
    }
}

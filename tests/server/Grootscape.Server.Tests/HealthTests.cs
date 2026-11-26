using System.Net;
using Grootscape.World;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Grootscape.Server.Tests;

public sealed class HealthTests
{
    [Fact]
    public async Task RunningHostRespondsToUncachedLivenessProbe()
    {
        await using ServerFactory host = new();
        using HttpClient client = host.CreateClient();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using HttpResponseMessage response = await client.GetAsync("/health/live", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(cancellationToken));
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task ReadinessFollowsTheWorldLoop()
    {
        await using ServerFactory host = new();
        using HttpClient client = host.CreateClient();
        TimeSpan tick = host.Services.GetRequiredService<IOptions<WorldOptions>>().Value.TickDuration;

        Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadinessAsync(client));

        host.Time.Advance(tick);
        Assert.Equal(HttpStatusCode.OK, await EventuallyAsync(client, HttpStatusCode.OK));
    }

    private static async Task<HttpStatusCode> ReadinessAsync(HttpClient client)
    {
        using HttpResponseMessage response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private static async Task<HttpStatusCode> EventuallyAsync(HttpClient client, HttpStatusCode expected)
    {
        HttpStatusCode status = await ReadinessAsync(client);
        for (int attempt = 0; attempt < 100 && status != expected; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
            status = await ReadinessAsync(client);
        }

        return status;
    }
}

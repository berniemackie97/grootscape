using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Grootscape.Server.Tests;

public sealed class LivenessTests
{
    [Fact]
    public async Task RunningHostRespondsToUncachedLivenessProbe()
    {
        await using WebApplicationFactory<Program> host = new();
        using HttpClient client = host.CreateClient();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using HttpResponseMessage response = await client.GetAsync("/health/live", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(cancellationToken));
        Assert.True(response.Headers.CacheControl?.NoStore);
    }
}

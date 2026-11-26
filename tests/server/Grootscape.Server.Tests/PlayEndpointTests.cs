using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Grootscape.Server.Tests;

/// <summary>Drives the handshake described in <c>protocol/README.md</c> over a real WebSocket.</summary>
public sealed class PlayEndpointTests : IAsyncDisposable
{
    private static readonly Uri PlayUri = new("ws://localhost/play");
    private static readonly byte[] WebHello = Convert.FromHexString("01000105");

    private readonly ServerFactory host = new();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => host.DisposeAsync();

    [Fact]
    public async Task WelcomesSupportedClientWithTickDuration()
    {
        using WebSocket socket = await ConnectAsync();

        await SendAsync(socket, WebHello);

        Assert.Equal("0200010258", Convert.ToHexString(await ReceiveAsync(socket)));
        Assert.Equal(WebSocketState.Open, socket.State);
    }

    [Fact]
    public async Task ClientCanCloseAnOpenSession()
    {
        using WebSocket socket = await ConnectAsync();
        await SendAsync(socket, WebHello);
        await ReceiveAsync(socket);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, Cancellation);

        Assert.Equal(WebSocketState.Closed, socket.State);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, socket.CloseStatus);
    }

    [Theory]
    [InlineData("0100020500FF", "0301")]
    [InlineData("01000106", "0302")]
    [InlineData("7F", "0302")]
    public async Task RejectsInvalidHelloThenCloses(string hello, string reject)
    {
        using WebSocket socket = await ConnectAsync();

        await SendAsync(socket, Convert.FromHexString(hello));

        Assert.Equal(reject, Convert.ToHexString(await ReceiveAsync(socket)));
        Assert.Equal(WebSocketCloseStatus.ProtocolError, await ReceiveCloseAsync(socket));
    }

    [Fact]
    public async Task ClosesTextMessages()
    {
        using WebSocket socket = await ConnectAsync();

        await socket.SendAsync("hello"u8.ToArray(), WebSocketMessageType.Text, true, Cancellation);

        Assert.Equal(WebSocketCloseStatus.InvalidMessageType, await ReceiveCloseAsync(socket));
    }

    [Fact]
    public async Task ClosesOversizedMessages()
    {
        using WebSocket socket = await ConnectAsync();

        await SendAsync(socket, new byte[2048]);

        Assert.Equal(WebSocketCloseStatus.MessageTooBig, await ReceiveCloseAsync(socket));
    }

    [Fact]
    public async Task ClosesMessagesAfterTheHandshake()
    {
        using WebSocket socket = await ConnectAsync();
        await SendAsync(socket, WebHello);
        await ReceiveAsync(socket);

        await SendAsync(socket, WebHello);

        Assert.Equal(WebSocketCloseStatus.ProtocolError, await ReceiveCloseAsync(socket));
    }

    [Fact]
    public async Task ClosesClientsThatNeverSayHello()
    {
        using WebSocket socket = await ConnectAsync();
        Task<WebSocketCloseStatus?> close = ReceiveCloseAsync(socket);

        await AdvanceUntilAsync(close, TimeSpan.FromSeconds(1));

        Assert.Equal(WebSocketCloseStatus.PolicyViolation, await close);
    }

    [Fact]
    public async Task ClosesOpenSessionsWhenTheServerStops()
    {
        using WebSocket socket = await ConnectAsync();
        await SendAsync(socket, WebHello);
        await ReceiveAsync(socket);

        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        Assert.Equal(WebSocketCloseStatus.EndpointUnavailable, await ReceiveCloseAsync(socket));
    }

    [Fact]
    public async Task PlainHttpRequestIsRejected()
    {
        using HttpClient client = host.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/play", Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<WebSocket> ConnectAsync()
    {
        WebSocketClient client = host.Server.CreateWebSocketClient();
        return await client.ConnectAsync(PlayUri, Cancellation);
    }

    /// <summary>Moves the fake clock forward in steps until <paramref name="task"/> completes.</summary>
    private async Task AdvanceUntilAsync(Task task, TimeSpan step)
    {
        for (int attempt = 0; attempt < 100 && !task.IsCompleted; attempt++)
        {
            host.Time.Advance(step);
            await Task.WhenAny(task, Task.Delay(TimeSpan.FromMilliseconds(20), Cancellation));
        }
    }

    private static Task SendAsync(WebSocket socket, byte[] message) =>
        socket.SendAsync(message, WebSocketMessageType.Binary, true, Cancellation);

    private static async Task<byte[]> ReceiveAsync(WebSocket socket)
    {
        byte[] buffer = new byte[1024];
        WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, Cancellation);
        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
        Assert.True(result.EndOfMessage);
        return buffer[..result.Count];
    }

    private static async Task<WebSocketCloseStatus?> ReceiveCloseAsync(WebSocket socket)
    {
        WebSocketReceiveResult result = await socket.ReceiveAsync(new byte[1024], Cancellation);
        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, Cancellation);
        return result.CloseStatus;
    }
}

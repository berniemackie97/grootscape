using System.Net.WebSockets;
using Grootscape.Protocol;
using Grootscape.Server.Observability;
using Grootscape.World;
using Microsoft.Extensions.Options;

namespace Grootscape.Server.Network;

internal sealed partial class PlaySessionHandler(IOptions<NetworkOptions> network, IOptions<WorldOptions> world, TimeProvider time,
    IHostApplicationLifetime lifetime, ServerMetrics metrics, ILogger<PlaySessionHandler> logger)
{
    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
        Connection connection = new(socket, context.TraceIdentifier, context.RequestAborted);
        try
        {
            await RunAsync(connection);
        }
        catch (WebSocketException exception) when (socket.State == WebSocketState.Aborted)
        {
            LogAborted(connection.Id, exception.Message);
        }
    }

    private async Task RunAsync(Connection connection)
    {
        Task<ReceivedMessage> firstReceive = connection.ReceiveAsync();
        Task timeout = Task.Delay(network.Value.HandshakeTimeout, time, connection.Aborted);
        if (await Task.WhenAny(firstReceive, timeout) != firstReceive)
        {
            metrics.Handshake("timeout");
            LogHandshakeTimeout(connection.Id);

            await CloseWithPendingReceiveAsync(connection, firstReceive, WebSocketCloseStatus.PolicyViolation, "handshake timeout");
            return;
        }

        ReceivedMessage first = await firstReceive;
        if (!await AcceptBinaryAsync(connection, first))
        {
            metrics.Handshake("rejected");
            return;
        }

        if (!ClientHello.TryDecode(connection.Received(first), out ClientHello hello, out DecodeError error))
        {
            metrics.Handshake("rejected");
            LogRejected(connection.Id, error);

            RejectReason reason = error == DecodeError.UnsupportedVersion
                ? RejectReason.UnsupportedVersion
                : RejectReason.Malformed;

            await connection.SendAsync(new ServerReject(reason).Encode());
            await CloseAsync(connection, WebSocketCloseStatus.ProtocolError, "handshake rejected");
            return;
        }

        await connection.SendAsync(new ServerWelcome((ushort)world.Value.TickDurationMs).Encode());
        metrics.Handshake("accepted");
        metrics.SessionOpened();
        LogSessionOpened(connection.Id, hello.Platform);
        try
        {
            await RunSessionAsync(connection);
        }
        finally
        {
            metrics.SessionClosed();
            LogSessionClosed(connection.Id);
        }
    }

    private async Task RunSessionAsync(Connection connection)
    {
        Task<ReceivedMessage> receive = connection.ReceiveAsync();
        await using (lifetime.ApplicationStopping.Register(() => _ = connection.TrySendCloseAsync(WebSocketCloseStatus.EndpointUnavailable, "server shutting down")))
        {
            ReceivedMessage message = await receive;
            if (message.Kind == ReceivedKind.Close && connection.CloseSent)
            {
                return;
            }

            if (!await AcceptBinaryAsync(connection, message))
            {
                return;
            }
        }

        await CloseAsync(connection, WebSocketCloseStatus.ProtocolError, "unexpected message");
    }

    private async Task<bool> AcceptBinaryAsync(Connection connection, ReceivedMessage message)
    {
        switch (message.Kind)
        {
            case ReceivedKind.Binary:
                return true;
            case ReceivedKind.Close:
                await CloseAsync(connection, WebSocketCloseStatus.NormalClosure, null);
                return false;
            case ReceivedKind.Text:
                await CloseAsync(connection, WebSocketCloseStatus.InvalidMessageType, "binary messages only");
                return false;
            case ReceivedKind.TooLarge:
            default:
                await CloseAsync(connection, WebSocketCloseStatus.MessageTooBig, "message too big");
                return false;
        }
    }

    private async Task CloseAsync(Connection connection, WebSocketCloseStatus status, string? description)
    {
        using CancellationTokenSource deadline = new(network.Value.CloseTimeout, time);
        try
        {
            await connection.Socket.CloseAsync(status, description, deadline.Token);
        }
        catch (OperationCanceledException)
        {
            connection.Socket.Abort();
        }
    }

    private async Task CloseWithPendingReceiveAsync(Connection connection, Task<ReceivedMessage> pending, WebSocketCloseStatus status, string description)
    {
        await connection.TrySendCloseAsync(status, description);
        if (await Task.WhenAny(pending, Task.Delay(network.Value.CloseTimeout, time)) != pending)
        {
            connection.Socket.Abort();
        }

        try
        {
            await pending;
        }
        catch (WebSocketException)
        {
            // The client did not acknowledge the close and the connection was dropped.
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Session {ConnectionId} opened from {Platform}")]
    private partial void LogSessionOpened(string connectionId, Platform platform);
    [LoggerMessage(Level = LogLevel.Information, Message = "Session {ConnectionId} closed")]
    private partial void LogSessionClosed(string connectionId);
    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} rejected: {Error}")]
    private partial void LogRejected(string connectionId, DecodeError error);
    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} did not complete the handshake in time")]
    private partial void LogHandshakeTimeout(string connectionId);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} dropped: {Reason}")]
    private partial void LogAborted(string connectionId, string reason);

    private sealed class Connection(WebSocket socket, string id, CancellationToken aborted)
    {
        private readonly byte[] buffer = new byte[WireProtocol.MaxMessageLength];
        private int closeSent;

        public WebSocket Socket => socket;
        public string Id => id;
        public CancellationToken Aborted => aborted;
        public bool CloseSent => Volatile.Read(ref closeSent) == 1;
        public Task<ReceivedMessage> ReceiveAsync() => MessageReceiver.ReceiveAsync(socket, buffer, aborted);
        public ReadOnlySpan<byte> Received(ReceivedMessage message) => buffer.AsSpan(0, message.Length);
        public Task SendAsync(byte[] message) => socket.SendAsync(message, WebSocketMessageType.Binary, endOfMessage: true, aborted);

        public async Task TrySendCloseAsync(WebSocketCloseStatus status, string description)
        {
            if (Interlocked.Exchange(ref closeSent, 1) == 1 || socket.State != WebSocketState.Open)
            {
                return;
            }

            try
            {
                await socket.CloseOutputAsync(status, description, aborted);
            }
            catch (WebSocketException)
            {
                // The connection dropped first; the receive loop observes it.
            }
        }
    }
}

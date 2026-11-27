using System.Net.WebSockets;
using Grootscape.Protocol;

namespace Grootscape.Server.Network;

internal enum ReceivedKind
{
    Binary,
    Text,
    TooLarge,
    Close,
}

internal static class MessageReceiver
{
    public static async Task<ReceivedMessage> ReceiveAsync(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
    {
        int length = 0;
        while (true)
        {
            ValueWebSocketReceiveResult result = await socket.ReceiveAsync(buffer.AsMemory(length), cancellationToken);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Close:
                    return new ReceivedMessage(ReceivedKind.Close, 0);
                case WebSocketMessageType.Text:
                    return new ReceivedMessage(ReceivedKind.Text, 0);
                case WebSocketMessageType.Binary:
                default:
                    break;
            }

            length += result.Count;
            if (result.EndOfMessage)
            {
                return new ReceivedMessage(ReceivedKind.Binary, length);
            }

            if (length == WireProtocol.MaxMessageLength)
            {
                return new ReceivedMessage(ReceivedKind.TooLarge, length);
            }
        }
    }
}

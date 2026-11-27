using System.Buffers.Binary;

namespace Grootscape.Protocol;

public readonly record struct ServerWelcome(ushort TickDurationMs)
{
    public byte[] Encode()
    {
        byte[] message = new byte[5];
        message[0] = WireProtocol.ServerWelcomeOpcode;

        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(1), WireProtocol.Version);
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(3), TickDurationMs);

        return message;
    }
}

using System.Buffers.Binary;

namespace Grootscape.Protocol;

/// <summary>The first message a client sends.</summary>
public readonly record struct ClientHello(Platform Platform)
{
    private const int Length = 4;

    /// <summary>Decodes one WebSocket binary message as a hello.</summary>
    /// <remarks>The version is checked before the rest of the message, as the specification requires.</remarks>
    public static bool TryDecode(ReadOnlySpan<byte> message, out ClientHello hello, out DecodeError error)
    {
        hello = default;
        error = Validate(message);
        if (error != DecodeError.None)
        {
            return false;
        }

        hello = new ClientHello((Platform)message[3]);
        return true;
    }

    private static DecodeError Validate(ReadOnlySpan<byte> message)
    {
        if (message.IsEmpty)
        {
            return DecodeError.Empty;
        }

        if (message[0] != WireProtocol.ClientHelloOpcode)
        {
            return DecodeError.UnknownOpcode;
        }

        if (message.Length < 3)
        {
            return DecodeError.Truncated;
        }

        if (BinaryPrimitives.ReadUInt16BigEndian(message[1..]) != WireProtocol.Version)
        {
            return DecodeError.UnsupportedVersion;
        }

        if (message.Length < Length)
        {
            return DecodeError.Truncated;
        }

        if (message.Length > Length)
        {
            return DecodeError.TrailingBytes;
        }

        return Enum.IsDefined((Platform)message[3]) ? DecodeError.None : DecodeError.InvalidValue;
    }
}

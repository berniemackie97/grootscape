namespace Grootscape.Protocol;

/// <summary>Constants shared by every message in the wire protocol. See <c>protocol/README.md</c>.</summary>
public static class WireProtocol
{
    /// <summary>The protocol version this codec speaks.</summary>
    public const ushort Version = 1;

    /// <summary>The largest message a version 1 peer accepts, in bytes.</summary>
    public const int MaxMessageLength = 1024;

    internal const byte ClientHelloOpcode = 0x01;
    internal const byte ServerWelcomeOpcode = 0x02;
    internal const byte ServerRejectOpcode = 0x03;
}

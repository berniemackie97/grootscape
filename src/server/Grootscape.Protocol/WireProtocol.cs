namespace Grootscape.Protocol;

public static class WireProtocol
{
    public const ushort Version = 1;

    public const int MaxMessageLength = 1024;

    internal const byte ClientHelloOpcode = 0x01;
    internal const byte ServerWelcomeOpcode = 0x02;
    internal const byte ServerRejectOpcode = 0x03;
}

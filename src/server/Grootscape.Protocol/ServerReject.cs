namespace Grootscape.Protocol;

/// <summary>Refuses a handshake. The server closes the connection after sending it.</summary>
public readonly record struct ServerReject(RejectReason Reason)
{
    public byte[] Encode() => [WireProtocol.ServerRejectOpcode, (byte)Reason];
}

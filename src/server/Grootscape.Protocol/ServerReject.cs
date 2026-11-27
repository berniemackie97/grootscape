namespace Grootscape.Protocol;

public readonly record struct ServerReject(RejectReason Reason)
{
    public byte[] Encode() => [WireProtocol.ServerRejectOpcode, (byte)Reason];
}

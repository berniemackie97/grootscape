namespace Grootscape.Protocol;

/// <summary>Why the server refused a handshake.</summary>
public enum RejectReason : byte
{
    UnsupportedVersion = 1,
    Malformed = 2,
}

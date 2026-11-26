namespace Grootscape.Protocol;

/// <summary>Why a byte sequence is not a valid message.</summary>
public enum DecodeError
{
    None,
    Empty,
    UnknownOpcode,
    Truncated,
    TrailingBytes,
    InvalidValue,
    UnsupportedVersion,
}

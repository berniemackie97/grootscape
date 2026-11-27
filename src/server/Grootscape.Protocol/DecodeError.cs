namespace Grootscape.Protocol;

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

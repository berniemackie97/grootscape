//! Client side of the Grootscape wire protocol.
//!
//! The specification and the shared fixtures live in `protocol/` at the repository root. This crate
//! encodes the messages a client sends and decodes the messages a client receives.

#![forbid(unsafe_code)]

use std::fmt;

/// The protocol version this crate speaks.
pub const PROTOCOL_VERSION: u16 = 1;

/// The largest message a version 1 peer accepts, in bytes.
pub const MAX_MESSAGE_LEN: usize = 1024;

const CLIENT_HELLO: u8 = 0x01;
const SERVER_WELCOME: u8 = 0x02;
const SERVER_REJECT: u8 = 0x03;

/// The platform a client runs on.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub enum Platform {
    Windows,
    Linux,
    MacOs,
    Android,
    Web,
}

impl Platform {
    /// The platform this binary was compiled for, when it is a supported client target.
    pub const fn current() -> Option<Self> {
        if cfg!(target_family = "wasm") {
            Some(Self::Web)
        } else if cfg!(target_os = "windows") {
            Some(Self::Windows)
        } else if cfg!(target_os = "android") {
            Some(Self::Android)
        } else if cfg!(target_os = "linux") {
            Some(Self::Linux)
        } else if cfg!(target_os = "macos") {
            Some(Self::MacOs)
        } else {
            None
        }
    }

    const fn to_wire(self) -> u8 {
        match self {
            Self::Windows => 1,
            Self::Linux => 2,
            Self::MacOs => 3,
            Self::Android => 4,
            Self::Web => 5,
        }
    }
}

/// Why the server refused a handshake.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub enum RejectReason {
    UnsupportedVersion,
    Malformed,
}

impl RejectReason {
    fn from_wire(value: u8) -> Result<Self, DecodeError> {
        match value {
            1 => Ok(Self::UnsupportedVersion),
            2 => Ok(Self::Malformed),
            _ => Err(DecodeError::InvalidValue),
        }
    }
}

/// The first message a client sends.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct ClientHello {
    pub platform: Platform,
}

impl ClientHello {
    /// Encodes the hello for [`PROTOCOL_VERSION`].
    pub fn encode(self) -> Vec<u8> {
        let [high, low] = PROTOCOL_VERSION.to_be_bytes();
        vec![CLIENT_HELLO, high, low, self.platform.to_wire()]
    }
}

/// A message the server sends to a client.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum ServerMessage {
    Welcome { tick_duration_ms: u16 },
    Reject { reason: RejectReason },
}

impl ServerMessage {
    /// Decodes one WebSocket binary message from the server.
    pub fn decode(bytes: &[u8]) -> Result<Self, DecodeError> {
        let (&opcode, fields) = bytes.split_first().ok_or(DecodeError::Empty)?;
        let mut reader = Reader(fields);
        let message = match opcode {
            SERVER_WELCOME => {
                if reader.u16()? != PROTOCOL_VERSION {
                    return Err(DecodeError::UnsupportedVersion);
                }
                Self::Welcome {
                    tick_duration_ms: reader.u16()?,
                }
            }
            SERVER_REJECT => Self::Reject {
                reason: RejectReason::from_wire(reader.u8()?)?,
            },
            _ => return Err(DecodeError::UnknownOpcode),
        };
        reader.finish()?;
        Ok(message)
    }
}

/// Why a byte sequence is not a valid message.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub enum DecodeError {
    Empty,
    UnknownOpcode,
    Truncated,
    TrailingBytes,
    InvalidValue,
    UnsupportedVersion,
}

impl fmt::Display for DecodeError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(match self {
            Self::Empty => "message is empty",
            Self::UnknownOpcode => "message has an unknown opcode",
            Self::Truncated => "message ends before its last field",
            Self::TrailingBytes => "message has bytes after its last field",
            Self::InvalidValue => "message has an unknown enumeration value",
            Self::UnsupportedVersion => "message uses an unsupported protocol version",
        })
    }
}

impl std::error::Error for DecodeError {}

struct Reader<'a>(&'a [u8]);

impl Reader<'_> {
    fn u8(&mut self) -> Result<u8, DecodeError> {
        let (&value, rest) = self.0.split_first().ok_or(DecodeError::Truncated)?;
        self.0 = rest;
        Ok(value)
    }

    fn u16(&mut self) -> Result<u16, DecodeError> {
        let (value, rest) = self
            .0
            .split_first_chunk::<2>()
            .ok_or(DecodeError::Truncated)?;
        self.0 = rest;
        Ok(u16::from_be_bytes(*value))
    }

    fn finish(self) -> Result<(), DecodeError> {
        if self.0.is_empty() {
            Ok(())
        } else {
            Err(DecodeError::TrailingBytes)
        }
    }
}

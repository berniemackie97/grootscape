//! Verifies the codec against the shared fixtures in `protocol/fixtures`.

use grootscape_protocol::{
    ClientHello, DecodeError, PROTOCOL_VERSION, Platform, RejectReason, ServerMessage,
};
use serde_json::Value;

const FIXTURES: &str = include_str!("../../../../protocol/fixtures/v1.json");

fn fixtures() -> Value {
    serde_json::from_str(FIXTURES).expect("fixtures are valid JSON")
}

fn cases<'a>(root: &'a Value, list: &str, direction: &str) -> Vec<&'a Value> {
    let cases: Vec<_> = root[list]
        .as_array()
        .expect("fixture list is an array")
        .iter()
        .filter(|case| case["direction"] == direction)
        .collect();
    assert!(!cases.is_empty(), "no {list} fixtures for {direction}");
    cases
}

fn bytes(case: &Value) -> Vec<u8> {
    let hex = case["hex"].as_str().expect("hex is a string");
    (0..hex.len())
        .step_by(2)
        .map(|i| u8::from_str_radix(&hex[i..i + 2], 16).expect("hex is valid"))
        .collect()
}

fn platform(name: &str) -> Platform {
    match name {
        "windows" => Platform::Windows,
        "linux" => Platform::Linux,
        "macos" => Platform::MacOs,
        "android" => Platform::Android,
        "web" => Platform::Web,
        other => panic!("unknown platform {other}"),
    }
}

fn error(name: &str) -> DecodeError {
    match name {
        "empty" => DecodeError::Empty,
        "unknownOpcode" => DecodeError::UnknownOpcode,
        "truncated" => DecodeError::Truncated,
        "trailingBytes" => DecodeError::TrailingBytes,
        "invalidValue" => DecodeError::InvalidValue,
        "unsupportedVersion" => DecodeError::UnsupportedVersion,
        other => panic!("unknown error {other}"),
    }
}

#[test]
fn fixtures_match_crate_version() {
    assert_eq!(fixtures()["protocolVersion"], PROTOCOL_VERSION);
}

#[test]
fn encodes_client_messages() {
    let root = fixtures();
    for case in cases(&root, "messages", "clientToServer") {
        let message = &case["message"];
        assert_eq!(message["type"], "clientHello", "{}", case["name"]);
        assert_eq!(message["protocolVersion"], PROTOCOL_VERSION);
        let hello = ClientHello {
            platform: platform(message["platform"].as_str().unwrap()),
        };
        assert_eq!(hello.encode(), bytes(case), "{}", case["name"]);
    }
}

#[test]
fn decodes_server_messages() {
    let root = fixtures();
    for case in cases(&root, "messages", "serverToClient") {
        let message = &case["message"];
        let expected = match message["type"].as_str().unwrap() {
            "serverWelcome" => {
                assert_eq!(message["protocolVersion"], PROTOCOL_VERSION);
                ServerMessage::Welcome {
                    tick_duration_ms: message["tickDurationMs"].as_u64().unwrap() as u16,
                }
            }
            "serverReject" => ServerMessage::Reject {
                reason: match message["reason"].as_str().unwrap() {
                    "unsupportedVersion" => RejectReason::UnsupportedVersion,
                    "malformed" => RejectReason::Malformed,
                    other => panic!("unknown reason {other}"),
                },
            },
            other => panic!("unknown server message {other}"),
        };
        assert_eq!(
            ServerMessage::decode(&bytes(case)),
            Ok(expected),
            "{}",
            case["name"]
        );
    }
}

#[test]
fn rejects_invalid_server_messages() {
    let root = fixtures();
    for case in cases(&root, "invalid", "serverToClient") {
        assert_eq!(
            ServerMessage::decode(&bytes(case)),
            Err(error(case["error"].as_str().unwrap())),
            "{}",
            case["name"]
        );
    }
}

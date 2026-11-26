# Wire protocol

This directory is the single source of truth for the Grootscape client/server wire contract. The C++23 codec (`src/client/cpp/protocol.cpp`) and the C# codec (`src/server/Grootscape.Protocol`) both verify themselves against [`fixtures/v1.json`](fixtures/v1.json). Change the specification, the fixtures, and both codecs in the same change.

## Transport

Clients connect with a WebSocket to `/play` on the game server. WebSockets work in every target, including the browser, so all clients share one transport. Each binary WebSocket message carries exactly one protocol message. Text messages are not part of the protocol.

## Encoding

- A message is a one-byte opcode followed by its fields in the order listed.
- Integers are unsigned and big-endian.
- A decoder rejects a message with missing bytes, extra bytes, an unknown opcode, or an unknown enumeration value.
- The largest message a v1 peer accepts is 1024 bytes.

## Handshake

The client sends `ClientHello` as its first message. The server answers with `ServerWelcome` when it supports the requested version, or with `ServerReject` followed by a close. The client must not send other messages before it receives `ServerWelcome`.

The opcode and version at the start of `ClientHello` are stable across every protocol version. A server reads the version first and rejects an unsupported version before it reads the rest of the message, so a future client still receives a meaningful rejection.

Version 1 defines no messages after the handshake. The server keeps the session open until either side closes it.

## Messages (version 1)

| Opcode | Message | Direction | Fields |
|---|---|---|---|
| `0x01` | `ClientHello` | client → server | `u16` protocol version, `u8` platform |
| `0x02` | `ServerWelcome` | server → client | `u16` protocol version, `u16` tick duration in milliseconds |
| `0x03` | `ServerReject` | server → client | `u8` reject reason |

| Platform | Value |
|---|---|
| `windows` | 1 |
| `linux` | 2 |
| `macos` | 3 |
| `android` | 4 |
| `web` | 5 |

| Reject reason | Value | Meaning |
|---|---|---|
| `unsupportedVersion` | 1 | The server does not speak the requested protocol version. |
| `malformed` | 2 | The first message was not a valid `ClientHello`. |

## Closing

| Situation | WebSocket close status |
|---|---|
| After `ServerReject` | 1002 protocol error |
| No `ClientHello` within the handshake timeout | 1008 policy violation |
| Text message | 1003 unsupported data |
| Message larger than 1024 bytes | 1009 message too big |
| Any message after the handshake in version 1 | 1002 protocol error |
| Server shutdown | 1001 going away |

## Fixtures

`fixtures/v1.json` lists valid messages with their exact bytes and invalid byte sequences with the error a decoder reports. Error names are `empty`, `unknownOpcode`, `truncated`, `trailingBytes`, `invalidValue`, and `unsupportedVersion`. Each codec encodes the messages its runtime sends, decodes the messages its runtime receives, and rejects the invalid sequences for its direction.

# 0005: Use one WebSocket transport

Status: Accepted.

## Context

The client targets include desktop, native mobile hosts and browsers. The wire contract must work across these targets without separate gameplay protocols. The existing v1 specification uses binary WebSocket messages at /play.

## Decision

Use WebSocket transport for all clients. Each binary WebSocket message contains one protocol message. Keep wire encoding in the C# and C++23 codecs, independent of transport and game rules. Maintain the shared specification and fixtures under `protocol/`.

A separate raw TCP or UDP gameplay transport would require another browser-compatible path and more transport-specific verification. Do not add it without a new decision and evidence.

## Consequences

The browser and native clients use the same message contract. WebSocket provides ordered reliable delivery; loss on a TCP connection can delay later data. The protocol still needs bounded message sizes, admission control and slow-client handling. Use the existing versioned handshake and explicit close behavior. A production client connection is future work in #36; v1 does not implement gameplay.

## Verification

See [wire protocol](../../protocol/README.md), [architecture](../architecture.md) and [migration verification](../verification/cpp23-migration.md). The existing integration test connects the C++ codec to the actual server. Production client transport verification remains in #36.

## Approval

Retrospective approval: repository owner `berniemackie97` merged [foundation PR #7](https://github.com/berniemackie97/grootscape-archive/pull/7) on 2026-11-26. It established the WebSocket endpoint and shared v1 wire contract. The same transport remains in the current protocol specification on main at `4f4983d04107cc36e1cd4c91b75de952aed94020`. The owner merge is approval evidence for the existing foundation, not verification of future client networking.

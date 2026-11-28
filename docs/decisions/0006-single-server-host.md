# 0006: Keep one deployable server host

Status: Accepted.

## Context

The world requires a single owner of mutable state. Hosting, transport, protocol encoding and game rules have different responsibilities. The current solution uses .NET 10 and three server projects.

## Decision

Deploy one ASP.NET Core host, `Grootscape.Server`. Keep game rules and world state in `Grootscape.World` and wire encoding in `Grootscape.Protocol`. The host references both libraries; neither library references the host or the other library. Organize host features by responsibility. Keep game rules independent of sockets, rendering and persistence.

Do not split features into separately deployed services without a measured requirement and a new decision. Do not move game rules into network handlers.

## Consequences

One host simplifies deployment and debugging. Library boundaries let tests exercise rules and codecs without hosting. The host owns configuration, lifecycle, networking and observability. The world loop owns mutable world state. Scaling beyond one process needs a separate architecture decision; the current contract does not establish distributed world support.

## Verification

See [architecture](../architecture.md), [repository rules](../../AGENTS.md), the three server project files and the server tests on main. The host entry point registers network and world features and maps the play and health endpoints. Use the README server verification commands.

## Approval

Retrospective approval: repository owner `berniemackie97` merged [foundation PR #7](https://github.com/berniemackie97/grootscape-archive/pull/7) on 2026-11-26. It established the single ASP.NET host and separate world and protocol libraries. The project references on main at `4f4983d04107cc36e1cd4c91b75de952aed94020` retain those boundaries. The owner merge is approval evidence for the existing architecture.

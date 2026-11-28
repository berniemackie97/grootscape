# 0004: Record architecture decisions

Status: Accepted on merge.

## Context

Issue #21 requires a durable review process for architecture decisions. The repository already has three numbered decision records in `docs/decisions/`. Existing documentation and issue references link to these paths. Creating another ADR directory would split the decision history; moving the records would break existing links.

## Decision

Keep `docs/decisions/` as the canonical ADR directory. Add an [index](README.md) and [template](template.md). Use sequential four-digit identifiers and the review and maintenance rules in the index.

Record architecture decisions before implementation. Record the context, decision, consequences, verification and approval. Record status separately from implementation progress. Preserve stable paths and decision history. Do not change an accepted decision silently.

Use issue or pull request review evidence for approvals. Obtain owner approval for product scope, licensing, compatibility changes and platform dependencies. Use engineering review for other significant technical decisions.

## Consequences

One index exposes decisions and their status. The existing record numbers and paths remain valid. A decision can be accepted before implementation; the acceptance does not mark the implementation complete. Reviewers must maintain the index and links when decisions change.

## Verification

Issue #21 and its implementation pull request track engineering review and the README verification suite. The records for existing client, transport and server decisions are in the same index.

## Approval

Engineering review and merge of the pull request for #21 establish acceptance.

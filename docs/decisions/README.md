# Architecture decision records

Use [the ADR process](0004-record-architecture-decisions.md) and [the template](template.md). The index records decisions, not implementation progress.

| Record | Status | Scope |
| --- | --- | --- |
| [0001](0001-cpp23-client.md) | Accepted | C++23 client foundation with SDL3 and WebGL 2 |
| [0002](0002-world-tick-and-intent-processing.md) | Accepted | World tick and intent processing (#23) |
| [0003](0003-world-region-format.md) | Proposed | World region source and runtime data (#26) |
| [0004](0004-record-architecture-decisions.md) | Accepted on merge | ADR process (#21) |
| [0005](0005-websocket-transport.md) | Accepted | Shared WebSocket transport |
| [0006](0006-single-server-host.md) | Accepted | Single deployable server and independent libraries |

## Review and maintenance

Add a record when a decision changes architecture, an external contract, ownership, product compatibility or a platform dependency. Do not create a record for every code change.

Use the next unused four-digit number. Keep the existing record number and file path. Update this index and the record in the same pull request when its status changes. Link the relevant issue, evidence and implementation work. Check the links during review.

The engineering reviewer checks the alternatives, evidence, consequences and verification plan. Obtain explicit owner approval for product scope, licensing, compatibility changes and platform dependencies. For routine engineering decisions, the engineering reviewer can approve the record. Do not treat a merged proposal as an accepted decision unless approval is recorded.

Use these status values: Proposed, Accepted, Rejected, Superseded. Record the approval source for acceptance. Keep rejected and superseded decisions in the index. For a material change to an accepted decision, add a new record and link both records. Correct factual errors in place without changing the decision. Keep implementation verification in its issue or evidence document.

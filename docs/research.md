# Offline research

The reference workspace is external to Grootscape (see #159). It is a development aid, not a product runtime or CI dependency. Keep acquired artifacts and machine-specific configuration outside this repository.

## Sources

| Source | Use | Limitations |
| --- | --- | --- |
| Revision-pinned TSPS and reference server source | Behavior, protocol and implementation evidence | Confirm the target revision and distinguish reference implementation choices from product requirements. |
| OpenRS2 and decoded game cache | Content identifiers, definitions, assets and client scripts | A cache definition alone does not establish a server-side rule. Record archive and revision identity. |
| OSRS Wiki | Documented gameplay, formulas, quests and historical changes | Check revision or date where available; absence from a search is not evidence of absence in game. |
| RuneLite and Plugin Hub source | Client behavior, interfaces and tooling references | Plugin behavior is not automatically game behavior. Record repository and commit. |

The local offline research directory contains `cache.sqlite`, `runelite-code.sqlite`, and `wiki-recovered-partial.sqlite`. The first two passed local integrity and checksum checks. The recovered Wiki index contains 40,363 searchable pages; its recovery report records 528 omitted pages with manifest mismatches and one omitted page whose revision endpoint returned HTTP 500. Original acquisition dates are unknown. These figures describe the local recovery, not coverage of the entire Wiki. Consult `snapshot-recovered-partial/recovery-report.json` for omissions and `wiki-recovery-tool/README.md` for recovery details. Preserve both snapshots; do not overwrite the original files or repeat bulk acquisition to answer individual questions.

## Research workflow

1. Establish the product requirement and target revision before choosing a source.
2. Search the local indexes first, then retrieve and inspect the underlying source or definition. Use targeted upstream queries if the local evidence is missing, stale or contradictory.
3. Cross-check behavior against independent evidence where possible. Distinguish observed behavior, documented behavior, inference and open questions. Record conflicting results instead of choosing silently.
4. In the issue or implementation evidence, record source URL or repository/path, revision or commit, relevant record identifiers and the observed result. For local artifacts record checksums or manifest identity when available. Never claim a revision or date that is unknown.
5. Turn verified behavior into acceptance criteria and regression checks. Keep unresolved compatibility claims open; do not invent implementation behavior to fill gaps.
6. Respect third-party licensing and provenance. Reference material does not authorize redistributing assets or copying source code. Use original or appropriately licensed fixtures.

## Local access

The reference utility exposes a local stdio MCP server using `serve --database <wiki> --cache-database <cache> --code-database <code>`. Point each argument at the corresponding external database. The executable and database paths belong in developer-local configuration; do not commit them. Compatibility of the recovered partial Wiki database with the original server has not yet been verified. Verify the recovery tool documentation and executable before registering it. Confirm tool discovery and representative searches and record retrieval for each dataset before relying on MCP. Local SQL access requires SQLite with FTS5 support.

A failed connection, empty result, omitted page or unavailable plugin is a research limitation, not evidence that the requested behavior does not exist. If local access fails, record the blocker and use available revision-appropriate sources; do not require the reference service for product build or test commands.

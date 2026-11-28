# 0003: World region source and runtime data

Status: proposed for owner review.

## Context

Issue #26 defines the format for #24 and #35. The owner selected an original content format with import tools, JSON source, compiled binary data, signed coordinates, 64 x 64 regions, four planes, 32-bit collision flags, vertex heights and layered terrain materials.

TSPS uses four planes, 64 x 64 tiles, flat collision storage and directional collision masks. RuneLite independently exposes the same region dimensions and separate movement and line-of-sight flags. The TSPS map editor preserves explicit vertex heights at region boundaries; the cache editor separates definitions, placement and the original cache. RuneLite stores underlay, overlay, overlay path and rotation separately. These sources establish reference representation, not full OSRS behavior.

## Decision

Use the [region format v1 contract](../world/region-format-v1.md). Keep source JSON and compiled binary separate. The C# server and C++23 engine read the same documented binary layout. Use an offline .NET 10 compiler with System.Text.Json, explicit little-endian encoding and no new runtime dependency for the first region. Use one validator in the compiler and CI. Runtime readers must still reject invalid binary input.

Use the canonical format as the document boundary for the future first-party authoring workspace (#161). Do not build a temporary editor or depend on a reference cache at runtime. Tiled or other external editors can export through an importer later; their document models are not the runtime contract. The desktop host and UI framework for #160 remain separate product and dependency decisions; select them before implementing the editor, not as part of the region compiler.

The original fixture contains an authored region, original material colors and expected movement observations. The first compiler slice must compile this fixture and verify reproducibility before #26 is complete. Do not add production world loading or rendering in this spike.

## Consequences

Explicit source defaults and per-coordinate overrides keep flat terrain concise without procedural runtime generation. Fixed-width dense arrays simplify bounds checks and independent readers. The first binary format is uncompressed; packaging and delivery belong to #54. A full region has 64 KiB of raw collision entries. This is a calculated array size, not a measured managed allocation or total region memory. #24 measures its actual storage.

Importers must identify the source revision and preserve or reject unsupported semantics. They must not silently collapse movement, projectile, route or dynamic occupancy flags. Bridge plane remapping, larger actor footprints, route-only blocking, line of sight and additional overlay geometry need their own evidence and regression tests. V1 is an initial static terrain and single-tile collision contract, not a complete cache implementation.

The fixture contains no imported data. The repository has no license yet (#20). Record original provenance and the pending license decision; permit the fixture for repository tests only. Do not approve it for distribution before #20.

## Sources

- [TSPS region](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/game/collision/Region.ts)
- [TSPS movement and terrain decoding](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/game/collision/RegionManager.ts)
- [TSPS collision flag categories](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/client/common/CollisionFlag.ts)
- [TSPS terrain geometry](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/client/rs/scene/SceneTileModel.ts)
- [TSPS map editor](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/docs/map-editor-area-copy.md)
- [TSPS cache editor](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/docs/agents/cache-editor.md)
- [RuneLite region](https://github.com/runelite/runelite/blob/ba34df51158d41e86365be1250ae018e62b96d14/cache/src/main/java/net/runelite/cache/region/Region.java)
- [RuneLite world coordinates](https://github.com/runelite/runelite/blob/ba34df51158d41e86365be1250ae018e62b96d14/runelite-api/src/main/java/net/runelite/api/coords/WorldPoint.java)
- [RuneLite collision flags](https://github.com/runelite/runelite/blob/ba34df51158d41e86365be1250ae018e62b96d14/runelite-api/src/main/java/net/runelite/api/CollisionDataFlag.java)

Local research MCP was unavailable. The format choices are Grootscape decisions. No reference source code or assets enter the fixture.

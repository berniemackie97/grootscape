# Region format v1

Status: draft contract for #26.

## Coordinates

World x and y are signed 32-bit tile coordinates. Positive x is east; positive y is north. Plane is 0 through 3. Regions contain 64 x 64 tiles on each plane. Region coordinates are the floor of world coordinates divided by 64, in the inclusive range -33554432 through 33554431. Local tile coordinates are 0 through 63. For example, world x -1 belongs to region -1, local x 63. Use checked or wider arithmetic for coordinate reconstruction and step calculations. A boundary vertex can lie one tile beyond the maximum world tile coordinate.

Use x-major ordering: tile index = x * 64 + y; vertex index = x * 65 + y. A full region uses plane order 0, 1, 2, 3. Heights are signed 32-bit integer units, positive up, with 128 height units per tile width. Vertices include the north and east borders (0 through 64). The same shared border vertex on adjacent regions must have the same height on the same plane. Do not derive collision from height. Use local coordinates for mesh construction to avoid large absolute floating-point positions.

## Authored JSON

UTF-8 JSON. Reject duplicate object properties, unknown fields, non-integer numbers in integer fields, invalid ranges and unsupported versions. All fields below are required. No implicit defaults beyond the explicit plane defaults. Array order does not change meaning for overrides, but authors and editors write overrides in x-major order. Each coordinate may occur only once per array.

Region object:
- `version`: integer 1.
- `regionX`, `regionY`: region coordinates.
- `provenance`: object with `origin` (original or imported), `source` (nonempty description or URI), `revision` (nonempty source identity), `licenseStatus` (approved or pending), `license` (nonempty license identifier or null if pending). Imported data requires an approved license and a source revision plus SHA-256 in a later importer contract; v1 fixture validation accepts original test data only.
- `planes`: exactly four entries in plane order.

Plane object:
- `defaultHeight`: signed 32-bit height.
- `defaultTile`: complete tile record.
- `vertices`: array of {x, y, height}; x and y are 0 through 64. Each entry replaces the default at that vertex.
- `tiles`: array of {x, y, tile}; x and y are 0 through 63. Each entry replaces the entire default tile.

Tile record:
- `underlay`, `overlay`: unsigned 32-bit material IDs; zero means no material.
- `shape`, `rotation`: unsigned 8-bit integers. Shape 0 means no overlay, 1 covers the whole tile, 2 covers the triangle with southwest, southeast and northwest corners at rotation 0. Rotation is clockwise quarter-turns, 0 through 3. Shape 0 requires overlay 0 and rotation 0; shape 1 requires nonzero overlay and rotation 0; shape 2 requires both materials nonzero. Other shapes are unsupported in v1; document and verify reference shape mappings before extending the contract.
- `settings`: array of unique names from the terrain settings table.
- `collision`: array of unique names from the collision flag table.

Terrain settings:

| Name | Bit value | Meaning |
| --- | ---: | --- |
| surface | 0x1 | Rendered terrain exists |
| roof | 0x2 | Roof metadata |
| bridge | 0x4 | Bridge metadata |

A surface requires a material. A tile without a surface has both materials zero, shape and rotation zero, and floor collision set in v1. Roof metadata does not change movement in #24. Reject bridge input in v1 source and binary readers until effective-plane mapping is specified; do not accept unsupported semantics. Other bits are invalid.

Collision flags use separate static categories. Values match the observed low-order reference masks to simplify future translation, but this contract does not depend on external cache encoding.

| Name | Hex value |
| --- | ---: |
| northWest | 0x1 |
| north | 0x2 |
| northEast | 0x4 |
| east | 0x8 |
| southEast | 0x10 |
| south | 0x20 |
| southWest | 0x40 |
| west | 0x80 |
| object | 0x100 |
| projectileNorthWest | 0x200 |
| projectileNorth | 0x400 |
| projectileNorthEast | 0x800 |
| projectileEast | 0x1000 |
| projectileSouthEast | 0x2000 |
| projectileSouth | 0x4000 |
| projectileSouthWest | 0x8000 |
| projectileWest | 0x10000 |
| projectileObject | 0x20000 |
| floorDecoration | 0x40000 |
| floor | 0x200000 |

Object, floor decoration and floor block destination movement. Projectile flags do not block ordinary movement. Preserve their distinction; line-of-sight execution is future work. Other bits are invalid. Dynamic occupancy and route-only flags are not serialized in v1. Directional flags must have reciprocal flags on an adjacent tile where both regions exist, including diagonal and projectile flags. For absent neighbors, movement must fail without loading data on the world thread.

For an ordinary 1 x 1 step, require the same plane, loaded source and destination, and a displacement of one tile in at least one axis and at most one in each axis. Reject a blocked destination or a wall in the travel direction (including the reciprocal destination flag). For diagonals, also check the two adjacent orthogonal tiles and the relevant cardinal wall edges from source and into destination; reject a blocked corner. Do not use source full-tile blocking to prevent an actor leaving a tile that has become blocked. Do not use projectile flags for these movement checks. #24 must verify the rule against reference evidence and document discrepancies before claiming parity.

The source material catalog is a separate JSON object with `version` (1), `provenance` as above and `materials` (array of objects with `id` nonzero uint32, `name` nonempty string, `rgba` four integers 0 through 255). IDs are unique. The catalog contains at most 65536 entries in v1. These original colors are sufficient for the initial terrain fixture; richer materials belong to content stories. Resolve every nonzero region material reference against this catalog. Compile the catalog to the binary format below. This catalog is not a complete production asset package.

## Compiled region

File extension `.grg`. All integers are little-endian. Signed values are two's complement. No padding, compression, platform structs or trailing bytes. Every reader bounds the input size before reading and validates the header and exact length before allocating decoded arrays or indexing. Reject unknown versions and nonzero reserved fields. Validate flag values and tile combinations, and verify the expected region identity. Load the compiled material catalog from the same verified build as the region; reject any nonzero material ID that does not resolve. The server can discard material data after validation if simulation does not use it. See Integrity and publication for the trust boundary.

Header (24 bytes):

| Offset | Type | Value |
| ---: | --- | --- |
| 0 | 4 bytes | ASCII GREG |
| 4 | uint16 | version 1 |
| 6 | uint16 | header length 24 |
| 8 | int32 | region x |
| 12 | int32 | region y |
| 16 | uint8 | plane count 4 |
| 17 | uint8 | region size 64 |
| 18 | uint16 | reserved 0 |
| 20 | uint32 | payload length 362512 |

The payload contains four consecutive plane blocks. Within each block:

| Offset in plane | Type | Count | Bytes |
| ---: | --- | ---: | ---: |
| 0 | int32 heights | 4225 | 16900 |
| 16900 | uint32 underlays | 4096 | 16384 |
| 33284 | uint32 overlays | 4096 | 16384 |
| 49668 | uint8 shapes | 4096 | 4096 |
| 53764 | uint8 rotations | 4096 | 4096 |
| 57860 | uint32 settings | 4096 | 16384 |
| 74244 | uint32 collision | 4096 | 16384 |

One plane block is 90628 bytes. A complete v1 file is 362536 bytes.

## Compiled material catalog

File name `materials.grm`. All integers are little-endian. No padding or trailing bytes. The header is 16 bytes:

| Offset | Type | Value |
| ---: | --- | --- |
| 0 | 4 bytes | ASCII GMAT |
| 4 | uint16 | version 1 |
| 6 | uint16 | header length 16 |
| 8 | uint32 | material count, 0 through 65536 |
| 12 | uint32 | reserved 0 |

Each following record is 8 bytes: uint32 nonzero material ID, then four uint8 values in red, green, blue, alpha order. Records are sorted by strictly increasing ID. Reject duplicate or zero IDs, unsupported versions, nonzero reserved fields, counts above the limit and a file length other than 16 + 8 * count. A reader must check the count and length before allocating decoded records. The fixture catalog has two records and is 32 bytes. Source names and provenance remain in authoring and package metadata. The renderer resolves IDs to RGBA through the loaded catalog. ID zero means no material and does not require a catalog record.

## Integrity and publication

Compiler output is deterministic for equivalent expanded region and material data. Do not include timestamps, source paths or native padding. Each invocation uses an exclusively owned staging directory and publishes a new, previously unused output directory only after all inputs, output files and checksums are valid. Publication must fail if the destination exists; never replace an existing build directory. Concurrent invocations cannot overwrite each other's output. Keep all region and catalog bytes in the same build. Remove incomplete staging output on failure. Preserve authored source. Do not update a shared active-build pointer in this compiler. Selecting and activating a published build belongs to deployment or #54; that operation must reject stale generations and switch the complete verified build as one unit.

For the local fixture, name region files `region_<regionX>_<regionY>.grg` using signed base-10 coordinates without leading zeroes or plus signs. Generate `SHA256SUMS` in the same directory: one lowercase 64-character SHA-256 hex digest, two ASCII spaces, and the exact file name per line, with LF line endings. Sort by file name in ordinal ascending order. Include one entry for each region file and the material catalog, and no other entries. Reject duplicate names, missing or extra entries, path components, invalid digests and names that do not match the required files. Commit the golden bytes and their checksum list through repository review. Tests obtain expected digests from this reviewed fixture baseline, not from an untrusted directory selected by a caller. Local loaders receive expected identities and digests from trusted configuration or the test harness. Do not treat a checksum list received alongside untrusted bytes as a trust anchor.

Read each bounded file once into an owned buffer. Compute SHA-256 over that exact buffer, compare it to the trusted expected digest, and decode that same buffer without reopening the file or permitting mutation. Reject mismatches before decoding or installing the region or catalog. Do not expose partially validated data. Validate every region against the catalog in the same trusted build. The fixture checksum list is a test contract, not a production package authentication format.

Issue #54 defines the production manifest, version selection, authentication and delivery. Before accepting any downloaded bytes, authenticate the manifest (or its digest) through a trusted signature or equivalent independently authenticated mechanism. A hash supplied by the same untrusted mirror as the content is insufficient. The authenticated manifest must bind each region identity, catalog and expected digest to one build. Reject untrusted, mismatched or stale builds, missing catalogs and mixed-build files. Production package loading must not ship before those checks are implemented and tested. The choice of trust mechanism and key management belongs to #54.

## Validation and scope

Validate the complete source and catalog before output. Report filename, plane and local coordinates for tile errors. Reject duplicate region identities across the entire input set, including identical files, before cross-region validation or output. Report both source filenames and the duplicate region coordinates. Reject duplicate tile or vertex coordinates, invalid IDs, invalid masks and nonreciprocal walls, mismatched neighboring borders, unsupported geometry and invalid source version. Bound input size and collection lengths before allocating; do not accept more than 4096 tile or 4225 vertex overrides per plane. Do not load an absent region on a movement query. Verify cross-region validation when multiple regions are supplied; a single fixture does not prove cross-region correctness.

The source fixture is under `tests/fixtures/world/region-v1/`. Expected observations are test data, not runtime spawn configuration. The compiler must generate the binary region and material catalog and their checksum list, and verify malformed source cases and deterministic output before #26 closes. Verify duplicate region identities and concurrent publication to the same destination, including a delayed older build. #24 verifies production C# loading and collision; #35 verifies C++ loading and rendering. Both use the same golden bytes and reviewed expected digests. Verify corrupt bytes, missing and mismatched catalogs, invalid catalog records, and replacement of a file after its bytes are read. Decoding must use only the verified buffer. #55 adds full CI validation and spawn reachability when spawn records exist. #54 owns packaging and distribution.

Fixture data is original and test-only until the project license decision (#20). Do not import or distribute third-party assets without verified permissions.

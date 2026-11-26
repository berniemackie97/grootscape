# Grootscape contribution rules

- Deliver a complete, verifiable slice through its normal entry point. Leave placeholders and TODOs out of the implementation. Track future work in issues with acceptance criteria. Merge code only when its promised behavior works and all required checks pass.
- Confirm decisions about product scope, compatibility targets, and platform dependencies before implementing them. Keep working on agreed scope while those decisions are open.
- Prefer direct implementations. Add abstractions when a real boundary or multiple callers justify them. Give each file a cohesive responsibility and put shared logic in one place.
- Keep game rules independent of transport, graphics, and persistence. Establish those boundaries when the first behavior crosses them. Give mutable world state one owner and document ordering at each boundary.
- Pin behavior and compatibility to a named upstream revision and reproducible evidence when a slice depends on TSPS. Preserve component license notices and record provenance before importing third-party code or data. Use original or appropriately licensed assets in the repository and verification fixtures.
- Cover changes to externally visible behavior with a focused regression check. Measure performance claims against a repeatable workload and keep the measurement alongside its results.
- Run the verification commands in the README before handing off a change. Report the checks run and any external validation still required.

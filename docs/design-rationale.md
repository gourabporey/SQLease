# SQLease — Design Rationale

This document records the reasoning behind significant design decisions in SQLease. Each entry explains the decision, the motivation, alternatives that were considered, the tradeoffs accepted, and the current status.

This document captures long-lived design philosophy. It is not a changelog and does not duplicate Architecture Decision Records. ADRs describe specific, bounded decisions at a point in time. This document explains the reasoning that makes a family of related decisions coherent.

---

## 1. Layered Pipeline with Strict Boundary Enforcement

**Decision:** The system is structured as a sequence of independent layers — CLI, Parser, Binder, Planner, Executor, Core, Storage — where each layer depends only on the layer directly below it, and communication crosses layer boundaries only through defined interfaces.

**Motivation:** A database engine is a transformation pipeline. SQL text enters; data changes result. Each stage of that transformation is conceptually independent: parsing does not need to know how data is stored, and storage does not need to know SQL syntax. Keeping these concerns separate means each stage can be implemented, tested, and replaced in isolation.

**Alternatives considered:**

- **Monolithic class:** All logic in a single class. Simple to start, impossible to extend or test as complexity grows.
- **Two-layer split (frontend/backend):** Parse and plan in one layer, execute and store in another. Simpler than five layers, but collapses important distinctions such as binding (which requires schema knowledge) and planning (which should be pure structure transformation).

**Tradeoffs accepted:**

- More project structure overhead than a small prototype requires.
- Each new layer added means a new communication contract to define.
- The architecture may feel heavy for the current state of the codebase, which has only one implemented layer.

**Current status:** The layer structure is defined as solution projects. Only `SQLease.Core` (domain model + storage) and `SQLease.CLI` (demo) contain real behaviour. All other layers are stubs. The architecture is not over-engineered for the current scope; it is appropriately prepared for what the project is intended to become.

---

## 2. `ITableStorage` as the Primary Extension Point

**Decision:** `Table` holds an `ITableStorage` and delegates all physical data operations through that interface. No layer above `ITableStorage` imports a concrete storage class.

**Motivation:** The domain model (`Table`, `Database`) should describe _what_ data operations mean, not _how_ they are performed physically. If `Table` constructs its own `DataFrameTableStorage` directly, the domain model is permanently coupled to one storage implementation. Testing the domain model requires testing through the storage layer. Replacing the backend requires modifying `Table`.

**Alternatives considered:**

- **Direct `DataFrameTableStorage` use in `Table`:** Simpler at first. Already the current implementation (and a documented deficiency). Creates coupling that cannot be undone without changing public API.
- **Abstract base class for storage:** Inheritance instead of interface. Would constrain implementations to a common state hierarchy unnecessarily.

**Tradeoffs accepted:**

- Every storage implementation must satisfy the full `ITableStorage` contract. If the contract evolves, all implementations must update.
- The interface must be stable. Changing it is a breaking change for all callers.

**Current status:** `ITableStorage` is defined and used. The violation is that `Table` currently constructs `DataFrameTableStorage` directly instead of accepting it via injection. This is a documented deficiency scheduled for correction in M2.

---

## 3. Tombstone-Based Soft Delete

**Decision:** Deleted rows are not removed immediately. Instead, a `__Deleted` boolean column is set to `true`. Rows are excluded from normal reads. Physical removal happens only when `Compact()` is called explicitly.

**Motivation:** Immediate physical deletion in a columnar store like `Microsoft.Data.Analysis.DataFrame` is expensive because it requires rebuilding the entire column structure. Tombstones defer that cost and decouple the logical deletion event from the physical compaction. This pattern is also compatible with future persistence features such as write-ahead logging, snapshot isolation, and crash recovery.

**Alternatives considered:**

- **Immediate physical deletion:** Simplest mental model for the caller. Requires rebuilding the DataFrame on every delete. Not composable with transactions or recovery.
- **Copy-on-write (immutable snapshots):** Functional approach where delete produces a new table view. More complex, significantly higher memory cost for large tables.

**Tradeoffs accepted:**

- Deleted rows consume storage until `Compact()` is called. For an educational in-memory engine, this is inconsequential.
- The system must reliably filter tombstoned rows on every read. A bug that includes tombstoned rows silently would be a correctness failure.
- The `__Deleted` column name is a storage-internal concern, but it currently leaks into `Database` (which passes the name to storage). This is a known deficiency.

**Current status:** Implemented and tested. The leakage of `__Deleted` into `Database` is a documented deficiency.

---

## 4. Programmatic API Before SQL Text Parsing

**Decision:** The domain model (`Database`, `Table`) exposes a C# API for all operations. SQL text parsing is a separate concern and will be layered on top.

**Motivation:** The domain model is the ground truth of what the system _can do_. If the domain model API is not clean and correct, building a parser on top of it will not produce a clean system. Validating the domain model API through direct programmatic use — and through unit tests — before introducing the complexity of parsing ensures the foundation is sound.

**Alternatives considered:**

- **SQL-first:** Implement the parser immediately and drive domain model design from query execution needs. Common in research databases. Risks producing a domain model shaped by parser convenience rather than domain clarity.
- **Internal DSL in C#:** Use method chaining or builder patterns to express queries without SQL text. More ergonomic than raw dictionaries, but adds an abstraction layer that does not contribute to the learning goals.

**Tradeoffs accepted:**

- The CLI is not useful as a SQL tool until the parser exists. Current CLI is a scripted demo only.
- There is an impedance mismatch between the domain API (uses `Dictionary<string, object?>` and C# lambdas) and what SQL will eventually express (structured predicates and column references). The executor will need to bridge this gap.

**Current status:** The programmatic API is the only interface. SQL parsing has not begun. The domain API will evolve when the executor layer is designed.

---

## 5. `Microsoft.Data.Analysis.DataFrame` as the Initial Storage Backend

**Decision:** The first `ITableStorage` implementation uses `Microsoft.Data.Analysis.DataFrame` as its in-memory columnar store.

For detailed motivation, alternatives, and tradeoffs, see [ADR-001](adr/ADR-001-dataframe-storage.md).

**Current status:** Implemented and tested. The backend's limitations are well-understood and documented. It is appropriate for the current phase. Replacement is expected when the query executor requires capabilities the DataFrame cannot provide.

---

## 6. Case-Insensitive Table Name Lookup

**Decision:** `Database` stores tables in a `Dictionary<string, Table>` with `StringComparer.OrdinalIgnoreCase`. Table names are matched case-insensitively.

**Motivation:** Standard SQL is case-insensitive for identifiers. A user writing `SELECT * FROM users` and `SELECT * FROM USERS` expects both to refer to the same table. Implementing case-insensitivity at the storage level prevents the parser and binder from needing to normalise identifiers before lookup.

**Alternatives considered:**

- **Case-sensitive lookup with normalisation at the binder:** Pushes the responsibility to the binder layer, which has not yet been built. Risks inconsistency if the binder is missed.
- **Case-sensitive lookup everywhere:** Non-standard. Would surprise contributors familiar with SQL.

**Tradeoffs accepted:**

- `OrdinalIgnoreCase` uses byte-level case folding, which is correct for ASCII identifiers. For Unicode identifiers, this may produce unexpected results, but the SQL subset being targeted uses ASCII-only identifiers.

**Current status:** Implemented in `Database`. Column name comparisons inside `DataFrameTableStorage` also use case-insensitive string comparison.

---

## 7. Test Project Structure Mirrors Source Namespaces

**Decision:** Test files are placed under `SQLease.Tests/` in a directory hierarchy that mirrors the namespace of the code under test. Tests for `SQLease.Core.Storage.DataFrameTableStorage` live at `SQLease.Tests/SQLease.Core/Storage/DataFrameTableStorageTests.cs`.

**Motivation:** A predictable test location removes the friction of finding tests. When a developer opens `DataFrameTableStorage.cs` and wants to find its tests, the path is mechanical. The structure also discourages placing tests for multiple subjects in a single file.

**Alternatives considered:**

- **Single flat test folder:** Simpler to start, becomes difficult to navigate as the codebase grows.
- **Per-project test projects (`SQLease.Core.Tests`, `SQLease.Engine.Tests`):** Common in larger solutions. The current scale does not justify the overhead.

**Tradeoffs accepted:**

- Directory depth grows with namespace depth. This is a reasonable cost for the navigational benefit.

**Current status:** Established and in use. One test class exists: `DataFrameTableStorageTests`. The `UnitTest1.cs` placeholder has not yet been removed.

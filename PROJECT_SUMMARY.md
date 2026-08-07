# SQLease Project Summary

## Overview

SQLease is an educational database-engine project built in C# on .NET 9. The current implementation is best described as an in-memory table store with a small database facade, a storage abstraction, and a CLI demo. The solution is organized as if it will grow into a fuller SQL engine, but most of the working behavior currently lives inside `SQLease.Core`.

The most meaningful implemented feature is row storage and mutation using `Microsoft.Data.Analysis.DataFrame` as the backing structure. Tables can be created, rows inserted, rows updated, rows soft-deleted through a tombstone column, and compacted later to physically remove deleted rows.

## What The Project Gets Right

- The solution is split early into `Core`, `Engine`, `Storage`, `CLI`, and `Tests`, which shows good architectural intent.
- Storage is hidden behind `ITableStorage`, which is a useful seam for future experimentation with file-backed or indexed implementations.
- Deletion uses a tombstone model instead of immediate physical removal. That is a reasonable design for future persistence, compaction, and recovery workflows.
- The current test coverage is focused on the only part of the system that actually has meaningful logic: `DataFrameTableStorage`.
- The project is clearly aimed at learning by building, and the current scope reflects that rather than pretending to be production-ready.

## Technical Decisions And Tradeoffs

### 1. .NET 9 and C#

Choosing modern .NET is a strong fit for this kind of systems-style educational project:

- Good type system and runtime safety
- Strong testing tooling
- Convenient project structure for multi-assembly solutions
- Easy path toward CLI tooling and future libraries

Tradeoffs:

- `net9.0` is modern but not the most conservative target if the goal is maximum portability or easier contributor setup.
- For a learning project, the ecosystem is strong, but many low-level database examples and reference implementations are more common in Rust, Go, Java, or C++.

### 2. `Microsoft.Data.Analysis.DataFrame` as the in-memory table engine

This is the most consequential choice in the repo.

Why it helps:

- Gives columnar storage quickly
- Avoids building custom row/column storage too early
- Makes typed columns straightforward for primitive data
- Lets the project focus on table behavior before query parsing or persistence

Tradeoffs:

- `DataFrame` is designed more for analytics/data tooling than for OLTP-style database-engine semantics.
- The project inherits library constraints around mutation, indexing, null handling, and row iteration instead of defining its own storage model.
- The storage engine becomes tightly coupled to supported CLR types and `DataFrameColumn` implementations.
- It is harder to evolve this into a serious SQL engine with transactions, constraints, efficient predicate execution, or storage/page abstractions.

This was a pragmatic bootstrap decision, but it also creates a ceiling. It is useful for proving ideas, not as the likely final storage core.

### 3. Tombstone-based deletes plus compaction

Soft delete through a hidden `__Deleted` column is a sensible decision because it:

- Keeps delete operations simple
- Avoids reshaping the underlying data structure on every delete
- Creates a path toward later compaction

Tradeoffs:

- The hidden metadata column leaks into table internals and storage concerns.
- The design currently relies on a magic string and mutable static state for the deleted-column name.
- Without stronger encapsulation, system columns can become a source of bugs once more features are added.

### 4. Schema represented as `Dictionary<string, Type>`

This keeps table creation easy and readable.

Tradeoffs:

- It is too weak for real schema modeling.
- There is no notion of nullability, default values, keys, uniqueness, constraints, identity columns, or user-defined types.
- The runtime type system is doing all schema work, which becomes limiting very quickly.

### 5. Delegating behavior directly from `Table` to storage

`Table` is currently a thin wrapper over `ITableStorage`.

Why it helps:

- Low complexity
- Easy to understand
- Minimal indirection while the project is still forming

Tradeoffs:

- Domain logic and storage logic are not yet clearly separated.
- `Table` exposes operational methods, but there is no execution layer between API intent and storage mutations.
- The `Engine` project exists conceptually, but not behaviorally.

## Challenges In The Current Design

### 1. The architecture is ahead of the implementation

The solution structure suggests a future SQL engine, but `Engine` and `Storage` are mostly placeholders while `Core` contains both data structures and the only real storage engine. That is a common early-project pattern, but it means the current boundaries are aspirational rather than enforced.

### 2. The model objects are not yet aligned with actual runtime behavior

`Row` exists but is not meaningfully integrated. `Column` is mutable even though schema should typically become stable after creation. `Database` and `Table` are useful facades, but they do not yet express richer database rules such as constraints, indexing, or query planning.

### 3. Type validation is incomplete

The schema records types, but insert and update paths rely heavily on the underlying `DataFrame` behavior. There is little explicit validation at the database layer, which means error handling and invariants are not fully controlled by the project itself.

### 4. The CLI is a scripted demo, not a database interface

The CLI proves that the API works, but it is not yet interactive and does not parse SQL-like input. That makes it good for smoke testing, but not yet representative of the project’s stated goal.

### 5. Tests cover the best part, but not the whole system

The storage tests are a good start, but there are still visible gaps:

- No tests around `Database`
- No tests around `Table`
- No integration-level tests for CLI behavior
- A leftover placeholder `UnitTest1` still exists

## Mistakes Or Weak Spots

These are the main issues I would call mistakes, or at least choices worth correcting early:

### 1. Placeholder projects were created before their responsibilities were implemented

Creating `Engine` and `Storage` early is fine, but leaving them as empty `Class1.cs` projects while working behavior stays in `Core` makes the architecture look more mature than it is. That can slow future refactoring because it obscures where the real boundaries should be.

### 2. Storage-specific concerns leaked into higher-level abstractions

The `__Deleted` implementation detail is coordinated across `Database`, `Table`, and storage. This is manageable now, but it is the kind of coupling that gets expensive later.

### 3. The deleted-column name is stored as mutable static state

`DataFrameTableStorage` uses a static field for the deleted column name. That means one table’s configuration can affect another table instance. Even if the current usage pattern avoids trouble, this is a correctness risk and the wrong ownership model.

### 4. Schema metadata is duplicated

`Table` tracks `List<Column>`, while `DataFrameTableStorage` separately tracks a `_schema` dictionary. Duplication like this usually drifts unless one side becomes authoritative.

### 5. The project structure promises SQL before a real execution model exists

The README mentions DDL, DML, parsing, storage, and indexing, but the implemented system is still a programmatic table API. That is not a problem in itself, but the project would benefit from sharper language about what is actually complete.

## Things I Would Do Differently

### 1. Define the storage boundary more strictly

I would keep `ITableStorage`, but narrow it to storage primitives instead of passing around `Dictionary<string, object?>` everywhere. A better approach would be:

- Introduce explicit schema and row value objects
- Keep system columns fully internal to storage
- Make one layer authoritative for schema metadata

### 2. Delay or simplify the multi-project split until the execution layer exists

If the goal is learning and iteration speed, there are two good options:

- Keep the split, but move real responsibilities into each project immediately
- Or collapse temporarily into fewer projects until parser/engine/storage boundaries become real

Right now the structure is reasonable, but a little premature.

### 3. Replace `DataFrame` once the learning objective shifts from prototype to engine design

For a prototype, `DataFrame` is fine. For a real database engine learning exercise, I would eventually switch to a custom storage model:

- Explicit pages or segments
- Row IDs
- Better ownership of type coercion
- Constraint enforcement
- Predictable update/delete behavior

That change would teach more about database internals than continuing to build on top of a general-purpose analytics structure.

### 4. Build the parser and execution pipeline earlier

The project is named and framed like a SQL engine, so I would prioritize:

- A minimal command grammar
- Parsed command objects
- An execution layer in `SQLease.Engine`
- Clear separation between user intent and physical storage mutation

That would make later features like validation, planning, and optimization much easier to place.

### 5. Raise the testing bar around behavior and invariants

I would add tests for:

- Duplicate table creation
- Case-insensitive table lookup behavior
- Invalid updates and type mismatches
- Hidden/system-column behavior
- Compaction invariants
- End-to-end command execution once parsing exists

## Recommended Next Steps

If this project continues, the most valuable near-term sequence would be:

1. Clean up the current core: remove placeholder files, eliminate static deleted-column state, and centralize schema ownership.
2. Move command execution logic into `SQLease.Engine` with a tiny non-SQL command model first if necessary.
3. Decide whether `DataFrame` is still the right storage foundation for the next phase.
4. Replace the CLI demo with an actual REPL or scripted command runner.
5. Expand tests around invariants before adding more features.

## Final Assessment

SQLease is a credible early-stage database-learning project with one real technical core: typed in-memory table storage backed by `Microsoft.Data.Analysis`. The strongest decision was creating a storage seam and implementing tombstone-based mutation behavior with tests. The weakest decisions are the premature architectural split, leakage of storage details into higher layers, and reliance on `DataFrame` for behavior that a database engine will eventually want to control directly.

As a prototype, the project is on a reasonable path. As a future SQL engine, it needs the next phase to shift from structure-first scaffolding toward execution-model clarity, schema rigor, and stronger ownership of storage behavior.

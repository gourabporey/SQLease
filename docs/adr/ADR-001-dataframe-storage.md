# ADR-001 — Use Microsoft.Data.Analysis.DataFrame as In-Memory Storage Backend

**Status:** Accepted

---

## Context

SQLease needs an in-memory columnar storage structure to back `ITableStorage`. Tables consist of named, typed columns and an arbitrary number of rows. The storage layer must support:

- Typed columns (`int`, `string`, `bool`, `double`, `DateTime`)
- Row insertion
- Predicate-based row selection and update
- Soft-delete via a tombstone column
- Physical compaction (rebuilding from surviving rows)

The immediate goal is to have a working storage layer that proves the domain model and `ITableStorage` interface are sound. The long-term goal is to replace this backend when query execution requires capabilities that it cannot provide (indexing, efficient predicate pushdown, write-ahead logging).

---

## Decision

Use `Microsoft.Data.Analysis.DataFrame` (the `Microsoft.Data.Analysis` NuGet package) as the physical store inside `DataFrameTableStorage`. Each column in the table maps to a typed `DataFrameColumn<T>`. Rows are represented as positional entries across all columns.

`DataFrameTableStorage` is the only concrete `ITableStorage` implementation at this stage.

---

## Alternatives Considered

**Custom row-based store (list of dictionaries)**
A `List<Dictionary<string, object?>>` per table. Simple to implement and understand. No external dependency. No type enforcement at the column level. Requires manual type checking on every read and write. Does not provide a columnar view, which complicates future aggregations or column-level operations. Viable for an early prototype but provides little learning value about storage internals.

**Custom columnar store (parallel typed arrays)**
Manually manage one typed array per column. Most faithful to real columnar database design. Highest implementation effort at this stage. Appropriate for a later milestone when storage internals are the focus. Building this before the query engine exists inverts the dependency: the storage design would drive the domain model rather than the domain model driving storage requirements.

**SQLite via `Microsoft.Data.Sqlite`**
Use an embedded relational database rather than building storage. Eliminates the entire storage problem. Defeats the educational purpose of the project — the goal is to understand how such a system works, not to delegate it.

**MemoryTable using `System.Linq`**
Use LINQ over in-memory collections. Simple to write, clear to read. Not a storage engine. Provides no path toward persistence, indexing, or physical layout control.

---

## Consequences

**Beneficial:**

- Working typed columnar storage without implementing a custom layout.
- Compaction, type dispatch, and column management are non-trivial enough to produce meaningful learning.
- `ITableStorage` is validated by a real implementation before any other layer depends on it.
- Allows the rest of the pipeline (parser, planner, executor) to be designed without waiting for custom storage.

**Costs and limitations:**

- `Microsoft.Data.Analysis` is designed for data analysis, not OLTP database semantics. Its mutation model (column-by-position, no row-level addressing) is at odds with database-style operations.
- Adding a new column type requires a new `DataFrameColumn<T>` branch in a switch expression. The type system is not open for extension.
- Full scans are required for all predicate operations; no indexing is possible within this backend.
- The library's `DataFrame` does not support row deletion in place; `Compact()` must rebuild the entire structure, which is O(n) in the number of surviving rows.
- A mutable static field (`_deletedColumn`) was introduced during implementation and is a known deficiency that must be corrected.
- This backend will eventually be replaced or supplemented. The `ITableStorage` interface must remain stable across that transition.

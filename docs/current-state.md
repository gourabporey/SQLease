# SQLease — Current State

This document provides a snapshot of the project's operational state. Its purpose is to allow a new contributor or coding agent to orient quickly: what has been done, what is in progress, and what comes next.

Update this document whenever a milestone is completed, a new one begins, or significant work in progress changes.

---

## Completed Milestones

### M0 — Solution Scaffolding

- Multi-project solution established: `SQLease.CLI`, `SQLease.Core`, `SQLease.Engine`, `SQLease.Storage`, `SQLease.Tests`
- Dependency direction enforced: CLI → Core, Engine → Core, Storage → Core
- NuGet dependency `Microsoft.Data.Analysis` added to `SQLease.Core`
- Test project configured with xUnit

### M1 — In-Memory Table Store

- Domain model defined: `Database`, `Table`, `Column`, `Row`
- `ITableStorage` interface defined as the primary storage boundary
- `DataFrameTableStorage` implemented:
  - Typed columnar storage for `int`, `string`, `bool`, `double`, `DateTime`
  - `InsertRow` — appends a row to the DataFrame
  - `GetAllRows` — returns all non-deleted rows; optionally includes tombstoned rows
  - `UpdateRows` — predicate-based in-place update
  - `DeleteRows` — tombstone-based soft delete via `__Deleted` column
  - `Compact` — physically removes tombstoned rows by rebuilding the DataFrame
  - Duplicate column detection on `AddColumn`
- Unit tests for all `DataFrameTableStorage` operations
- Architecture, design principles, coding standards, and SQL language spec documented
- CLI scripted demo demonstrates the programmatic API end-to-end

---

## Current Milestone

**M2 — Domain Model Cleanup**

Resolve known deficiencies in the domain model before the parser is introduced. A clean foundation avoids propagating these problems through all future layers.

Target outcomes:

- `Column` is immutable (name and type are set at construction; no setters)
- `Table` accepts `ITableStorage` via constructor injection rather than constructing `DataFrameTableStorage` internally
- `Table.PrintAllRows()` removed; row display is the CLI's responsibility
- `Database` no longer owns the `__Deleted` column name; that knowledge is encapsulated inside `ITableStorage`
- `_deletedColumn` static field eliminated from `DataFrameTableStorage`; each instance holds its own value
- Schema duplication between `Table.Columns` and storage resolved with a single authoritative source
- `UnitTest1.cs` placeholder removed from `SQLease.Tests`

---

## Next Milestone

**M3 — SQL Lexer and Parser**

Introduce the SQL parsing layer. Target outcomes:

- Lexer tokenises SQL input into a token stream
- Parser produces an AST from the token stream
- AST covers the DDL and DML subset defined in `docs/sql-language-spec.md`
- Parser lives in `SQLease.Core` or a dedicated `SQLease.Parser` project
- Parsing is a pure function: no side effects, no I/O, fully unit-testable
- Parser errors produce structured error types, not exceptions

---

## Work in Progress

No active work at this time. The project is between milestones.

---

## Incomplete Features

| Feature              | Notes                                                           |
| -------------------- | --------------------------------------------------------------- |
| SQL text parsing     | No lexer or parser exists; all operations are programmatic      |
| Interactive CLI REPL | CLI is a scripted demo; no read-eval-print loop                 |
| Semantic binder      | Not started; depends on parser                                  |
| Query planner        | Not started; depends on binder                                  |
| Query executor       | Not started; depends on planner                                 |
| File-backed storage  | `SQLease.Storage` is a stub; no persistence layer               |
| Typed result sets    | `GetAllRows` returns `IEnumerable<Dictionary<string, object?>>` |

---

## Technical Debt

| Item                                     | Severity | Location                                          | Description                                                             |
| ---------------------------------------- | -------- | ------------------------------------------------- | ----------------------------------------------------------------------- |
| `_deletedColumn` is static mutable       | High     | `DataFrameTableStorage`                           | Creates cross-instance interference; breaks test isolation              |
| `Table` hardcodes storage implementation | High     | `Table` constructor                               | `new DataFrameTableStorage()` prevents injection and fake-based testing |
| Schema duplication                       | Medium   | `Table.Columns` + `DataFrameTableStorage._schema` | Two sources of truth that can diverge                                   |
| `Column` is mutable                      | Medium   | `Column`                                          | `Name` and `DataType` have public setters; schema should be immutable   |
| `Database` owns storage column name      | Medium   | `Database.DeletedColumnName`                      | Storage concern leaking above the storage boundary                      |
| `Table.PrintAllRows()` performs I/O      | Low      | `Table`                                           | Domain model classes must not perform I/O                               |
| `UnitTest1.cs` placeholder               | Low      | `SQLease.Tests`                                   | Leftover scaffolding; must be removed                                   |

---

## Planned Improvements

The following improvements are scoped and ready to implement in priority order:

1. Make `Column` immutable — change `Name` and `DataType` to init-only or constructor-set properties
2. Inject `ITableStorage` into `Table` — remove `new DataFrameTableStorage()` from the constructor
3. Encapsulate `__Deleted` column management inside `ITableStorage` — remove `AddDeletedColumn` from the public interface or make it internal to the storage layer
4. Eliminate `_deletedColumn` static field — move it to instance state
5. Designate `Table.Columns` as the single schema source of truth — remove `_schema` from `DataFrameTableStorage` or derive `Table.Columns` from it
6. Remove `Table.PrintAllRows()` — move display logic to the CLI
7. Delete `UnitTest1.cs`

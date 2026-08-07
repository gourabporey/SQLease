# SQLease — Project Memory

This document is the authoritative engineering memory of the SQLease project. It summarises decisions, constraints, open questions, and future direction. It is not a changelog. Update it when the project's architecture, philosophy, or constraints change in a meaningful way.

---

## Current Development Focus

The in-memory table store is the only implemented layer. Active work is consolidating its foundations — fixing known deficiencies, ensuring the domain model is clean and well-tested — before introducing the SQL parser.

The next major work is implementing a SQL lexer and parser that produces an AST. That AST will then feed the semantic binder, query planner, and executor in sequence.

---

## Project Philosophy

SQLease is an educational database engine. It is not production software. Decisions are made to maximise learning value and architectural clarity, not raw performance.

Priorities, in order:

1. **Correctness** — incorrect behaviour is never acceptable
2. **Simplicity** — the simplest solution that is correct is preferred
3. **Maintainability** — optimise for the reader
4. **Extensibility** — design for predictable extension, not speculative generalisation
5. **Testability** — treat untestable code as a design signal
6. **Performance** — only when a measured bottleneck justifies the complexity

This ordering means performance is deliberately last. An architectural choice that sacrifices clarity for speed requires explicit justification.

---

## Active Architectural Decisions

| Decision                       | Summary                                                                                                                           | ADR                                         |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| DataFrame as in-memory storage | `Microsoft.Data.Analysis.DataFrame` provides typed columnar storage without requiring custom row management                       | [ADR-001](adr/ADR-001-dataframe-storage.md) |
| Tombstone-based soft delete    | Rows are marked deleted via a `__Deleted` boolean column; physical removal happens only on `Compact()`                            | —                                           |
| Interface-bounded layers       | `ITableStorage` is the layer boundary between the domain model and storage; no layer above it imports a concrete storage class    | —                                           |
| Layered pipeline architecture  | SQL text → Parser → Binder → Planner → Executor → Core domain → Storage; each stage communicates only with its adjacent neighbour | —                                           |

---

## Important Constraints

- **No SQL parsing exists yet.** All operations are expressed as C# API calls. The CLI is a scripted demo, not an interactive REPL.
- **`Microsoft.Data.Analysis.DataFrame` constrains the type system.** Only `int`, `string`, `bool`, `double`, and `DateTime` are supported as column types. Adding new types requires adding a new `DataFrameColumn<T>` branch.
- **The `DataFrame` library provides no row-level addressing.** Predicate-based queries require full scans. Efficient indexing or predicate pushdown is not possible within the current storage backend.
- **Nullable reference types are enabled.** Null must be handled explicitly at all boundaries.

---

## Known Limitations

| Limitation                                      | Location                                            | Notes                                                                                            |
| ----------------------------------------------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `_deletedColumn` is a mutable static field      | `DataFrameTableStorage`                             | Creates cross-instance coupling; each instance should own its own deleted-column name            |
| `Column` is mutable                             | `SQLease.Core.Models.Column`                        | Name and DataType should be immutable after construction                                         |
| `Table` hardcodes `new DataFrameTableStorage()` | `SQLease.Core.Models.Table`                         | Prevents dependency injection and testing with a fake storage implementation                     |
| Schema duplication                              | `Table.Columns` and `DataFrameTableStorage._schema` | Both track schema independently; they can diverge                                                |
| `Database` owns the `__Deleted` column name     | `SQLease.Core.Models.Database`                      | The tombstone column name is a storage concern and should not be visible above the storage layer |
| `Table.PrintAllRows()` performs I/O             | `SQLease.Core.Models.Table`                         | Console I/O does not belong in a domain model class                                              |
| `UnitTest1.cs` placeholder exists               | `SQLease.Tests`                                     | Must be replaced before any real feature is considered shipped                                   |

---

## Open Questions

- **When should the DataFrame backend be replaced?** The current backend is appropriate for the table-store phase but constrains the query engine. The right trigger is when the first planner or executor component requires capabilities the DataFrame cannot provide.
- **Should `SQLease.Storage` be activated as a real project now?** Currently a stub. It is the intended home for file-backed implementations. Activating it before the domain model is stable could result in premature coupling.
- **How should result sets be represented?** `GetAllRows()` currently returns `IEnumerable<Dictionary<string, object?>>`. This is not typesafe and exposes mutable internal state. A typed result object is needed before the executor layer is built.
- **Where does the semantic binder live?** The architecture document places it in `SQLease.Core`. This should be confirmed before the parser is introduced.

---

## Recent Architectural Changes

- **Initial domain model established.** `Database`, `Table`, `Column`, `Row`, and `ITableStorage` defined in `SQLease.Core`.
- **`DataFrameTableStorage` implemented and tested.** Supports insert, predicate-based update and delete (tombstone), physical compaction, and duplicate column detection.
- **Multi-project solution structure established.** `SQLease.CLI`, `SQLease.Core`, `SQLease.Engine`, `SQLease.Storage`, and `SQLease.Tests` created with correct dependency direction.
- **SQL language specification drafted.** `docs/sql-language-spec.md` defines the intended SQL dialect as a reference for parser contributors.

---

## Future Considerations

The following represent the intended evolution of the system, roughly in order:

1. **Fix known domain model deficiencies** — immutable `Column`, injected storage in `Table`, remove `PrintAllRows`, eliminate schema duplication.
2. **SQL Lexer and Parser** — tokenise SQL input; produce an AST rooted at `Statement` with subtypes per statement kind.
3. **Semantic Binder** — resolve identifiers, validate types, annotate the AST.
4. **Query Planner** — translate the bound AST into a logical execution plan.
5. **Query Executor** — execute the logical plan against the Core domain API.
6. **Interactive CLI REPL** — replace the scripted demo with a proper read-eval-print loop.
7. **File-backed storage** — implement `ITableStorage` in `SQLease.Storage` with durability.
8. **Typed result sets** — replace `Dictionary<string, object?>` returns with a structured `ResultSet` type.

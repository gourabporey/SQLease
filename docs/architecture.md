# SQLease — Architecture

This document describes the architecture of SQLease: how the system is structured, what each component is responsible for, how layers communicate, and where new functionality belongs.

---

## 1. Architectural Goals

SQLease is designed as a layered pipeline that transforms SQL text into data operations. The architecture prioritises these properties:

- **Strict layer separation.**
- **Independent testability.**
- **Incremental buildability.**
- **Minimal coupling.**

---

## 2. System Pipeline

A complete SQL request travels through the following stages:

```
User Input (text)
      │
      ▼
┌──────────────────────────────────────────────────────────────┐
│  SQLease.CLI                                                  │
│  Accepts input, invokes the parser, displays results          │
└──────────────────────────────┬───────────────────────────────┘
                               │ raw SQL string
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  SQL Parser  (planned — SQLease.Core or SQLease.Parser)       │
│  Lexes and parses SQL text into an Abstract Syntax Tree (AST) │
└──────────────────────────────┬───────────────────────────────┘
                               │ AST (Statement nodes)
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  Semantic Analyser / Binder  (planned — SQLease.Core)         │
│  Resolves identifiers, validates types, annotates the AST     │
└──────────────────────────────┬───────────────────────────────┘
                               │ bound, type-annotated AST
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  Query Planner  (planned — SQLease.Engine)                    │
│  Translates the bound AST into a logical execution plan       │
└──────────────────────────────┬───────────────────────────────┘
                               │ logical plan tree
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  Query Executor  (planned — SQLease.Engine)                   │
│  Executes the logical plan by invoking Core domain operations │
└──────────────────────────────┬───────────────────────────────┘
                               │ domain API calls
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  SQLease.Core                                                 │
│  Database, Table, ITableStorage — the authoritative domain    │
└──────────────────────────────┬───────────────────────────────┘
                               │ storage primitives
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  SQLease.Storage / DataFrameTableStorage                      │
│  Physical or in-memory storage implementation                 │
└──────────────────────────────────────────────────────────────┘
```

**Current state:** Only `SQLease.Core` (domain model + `DataFrameTableStorage`) and `SQLease.CLI` (scripted demo) contain real behaviour. The parser, planner, and executor do not yet exist.

---

## 3. Projects and Responsibilities

### 3.1 SQLease.Core

**Status:** Actively implemented.

`SQLease.Core` is the heart of the system. It owns the canonical domain model and the storage abstraction. All other projects depend on `Core`; `Core` depends on nothing except its own NuGet dependencies.

| Namespace              | Contents                                 | Responsibility                                              |
| ---------------------- | ---------------------------------------- | ----------------------------------------------------------- |
| `SQLease.Core.Models`  | `Database`, `Table`, `Column`, `Row`     | Domain entities representing the logical database structure |
| `SQLease.Core.Storage` | `ITableStorage`, `DataFrameTableStorage` | Storage contract and current in-memory implementation       |

#### Database

`Database` is the top-level container. It holds a case-insensitive dictionary of named tables and enforces uniqueness. It is also the place where system-level concerns such as the tombstone column are co-ordinated at creation time.

`Database` is the correct entry point for any operation that spans multiple tables or requires catalog-level knowledge.

#### Table

`Table` is a named, schematised collection of rows. It holds the column list as the authoritative schema metadata and delegates all physical operations to an `ITableStorage` implementation. Table does not interpret row data semantically; it delegates that entirely to storage.

A known deficiency: `Table.Columns` and `DataFrameTableStorage._schema` both track schema, which creates the potential for drift. The correct fix is to make one authoritative and derive the other.

#### Column

`Column` is a schema descriptor: a name and a CLR type. Currently mutable, which is incorrect; a column's definition should be immutable after the table is created. This should be corrected before the schema model is extended.

#### Row

`Row` is a thin wrapper around a `Dictionary<string, object?>`. It exists as a named type to make the intent legible. It is not yet meaningfully integrated into insert and query paths.

#### ITableStorage

`ITableStorage` is the primary extension point for storage. It defines the contract that all storage implementations must satisfy:

```
AddColumn(name, type)
AddDeletedColumn(columnName)
InsertRow(rowValues)
GetAllRows(includeDeleted)
UpdateRows(predicate, newValues)
DeleteRows(predicate)
Compact()
```

Any new storage backend — file-backed, page-based, indexed — implements this interface. No layer above storage should import a concrete storage class.

#### DataFrameTableStorage

`DataFrameTableStorage` is the current implementation of `ITableStorage`, backed by `Microsoft.Data.Analysis.DataFrame`. It supports:

- Typed columnar storage for `int`, `string`, `bool`, `double`, and `DateTime`.
- Soft delete via a `__Deleted` boolean tombstone column.
- Physical compaction that rebuilds the DataFrame from surviving rows.
- Predicate-based updates and deletes expressed as `Func<Dictionary<string, object?>, bool>`.

**Known limitations:**

- `_deletedColumn` is a mutable static field, which creates cross-instance coupling. Each instance should own its own deleted-column name.
- System columns such as `__Deleted` are partially visible above the storage boundary (the `Database` class knows the column name). The storage layer should encapsulate this entirely.
- The `DataFrame` library constrains type support and provides no row-level addressing, making future indexing and predicate pushdown harder.

---

### 3.2 SQLease.Engine

**Status:** Stub — `Class1.cs` placeholder only.

`SQLease.Engine` is the future home of:

- **Query Executor:** Receives a logical plan from the planner and executes it by calling `SQLease.Core` domain operations.
- **Query Planner:** Translates a bound AST into an ordered sequence of physical operations.

`SQLease.Engine` depends on `SQLease.Core` for the domain model but must not reference any concrete storage class. When the engine calls `Table.InsertRow(...)`, it does not know or care whether the backing store is a DataFrame, a file, or a network service.

---

### 3.3 SQLease.CLI

**Status:** Scripted demo.

`SQLease.CLI` is the user-facing entry point. In its eventual form it will:

1. Read SQL input from stdin or a REPL loop.
2. Pass raw SQL to the parser.
3. Receive a result set or error response from the engine.
4. Format and print results to stdout.

`SQLease.CLI` must contain no business logic. It owns I/O only. It must not reference `DataFrameTableStorage` or any other concrete implementation.

---

### 3.4 SQLease.Storage

**Status:** Stub — placeholder only.

`SQLease.Storage` is intended as the project for implementations of `ITableStorage` that involve physical persistence: file-backed storage, write-ahead logging, page management, and eventually indexing. It is separate from `SQLease.Core` so that persistence concerns do not leak into the domain model.

When file-backed storage is introduced, `DataFrameTableStorage` may remain in `SQLease.Core` as the default in-memory implementation, or may move here.

---

### 3.5 SQLease.Tests

**Status:** Active — covers `DataFrameTableStorage`.

`SQLease.Tests` contains all unit and integration tests. Tests are organised to mirror the namespace they cover:

```
SQLease.Tests/
└── SQLease.Core/
    └── Storage/
        └── DataFrameTableStorageTests.cs
```

The `UnitTest1.cs` placeholder is a known leftover and must be replaced before shipping any real feature.

---

## 4. Dependency Direction

Dependencies flow downward only. Higher layers depend on lower layers; lower layers never depend on higher layers.

```
SQLease.CLI
    └── depends on → SQLease.Core (planned: also SQLease.Engine)
SQLease.Engine
    └── depends on → SQLease.Core
SQLease.Tests
    └── depends on → SQLease.Core
SQLease.Core
    └── depends on → Microsoft.Data.Analysis (NuGet)
SQLease.Storage
    └── depends on → SQLease.Core (interface)
```

No circular dependencies are permitted. If a lower layer appears to need a higher-layer type, the type belongs in the lower layer or in a shared abstractions layer.

---

## 5. Planned Components

The following components do not yet exist but are required for SQLease to become a functioning SQL engine.

### 5.1 Lexer

Responsibility: Convert a SQL string into a flat sequence of typed tokens.

Location: `SQLease.Core/Parsing/` or a new `SQLease.Parser` project.

The lexer must be a pure function: given identical input, it always produces identical output. It has no state between calls and performs no schema resolution.

### 5.2 Parser

Responsibility: Convert a token sequence into an Abstract Syntax Tree (AST).

Location: Same project as the lexer.

The parser must produce an AST that faithfully represents the input SQL without making semantic decisions. It should not resolve table names, validate types, or execute anything. Parser errors are syntax errors; they do not represent runtime failure.

### 5.3 AST

Responsibility: Provide a strongly typed, immutable representation of SQL statements.

Location: `SQLease.Core/Ast/`

AST nodes should be records or sealed classes. They are data containers; they contain no logic. Visitors or pattern-matching drive AST traversal.

```
Statement
├── CreateTableStatement { TableName, Columns }
├── InsertStatement      { TableName, Values }
├── SelectStatement      { TableName, Columns, Where }
├── UpdateStatement      { TableName, Sets, Where }
└── DeleteStatement      { TableName, Where }
```

### 5.4 Semantic Analyser / Binder

Responsibility: Validate and annotate the AST using catalog information.

Location: `SQLease.Core/` or `SQLease.Engine/`

The binder resolves identifiers to concrete schema objects, verifies that column names exist, checks type compatibility of expressions, and annotates the AST with resolved types. It reads from the catalog (i.e., `Database`) but does not mutate it.

### 5.5 Query Planner

Responsibility: Translate a bound AST into a logical plan.

Location: `SQLease.Engine/`

A logical plan is an ordered tree of operations. For the current SQL subset, plans are simple:

```
InsertPlan  { TargetTable, Values }
ScanPlan    { SourceTable, Filter }
UpdatePlan  { TargetTable, Filter, Assignments }
DeletePlan  { TargetTable, Filter }
```

### 5.6 Query Executor

Responsibility: Execute a logical plan by invoking `SQLease.Core` domain operations.

Location: `SQLease.Engine/`

The executor is the only component permitted to mutate database state. It receives a plan, calls the appropriate `Table` methods, and returns a result: a set of rows, a count, or an acknowledgement.

### 5.7 Catalog

Responsibility: Provide a queryable view of database schema for use during planning and binding.

Location: `SQLease.Core/`

The catalog exposes table names, column names, and column types. In the current implementation, `Database` partially serves this role. As the system grows, a dedicated `ICatalog` interface will provide a cleaner read-only view.

---

## 6. Data Flow Examples

### CREATE TABLE

```
CLI          → "CREATE TABLE users (id INT, name TEXT)"
Parser       → CreateTableStatement { Name="users", Columns=[...] }
Binder       → validates no conflicts with existing tables
Planner      → CreateTablePlan { Name="users", Schema={...} }
Executor     → database.CreateTable("users", schema)
Core         → Table created, schema stored, storage initialised
```

### INSERT

```
CLI          → "INSERT INTO users VALUES (1, 'Alice')"
Parser       → InsertStatement { Table="users", Values=[1, "Alice"] }
Binder       → resolves table, validates column count and types
Planner      → InsertPlan { Table=<resolved>, Row={...} }
Executor     → table.InsertRow(row)
Storage      → appends row to DataFrame with __Deleted=false
```

### SELECT (with filter)

```
CLI          → "SELECT * FROM users WHERE id = 1"
Parser       → SelectStatement { Table="users", Where=BinaryExpr{...} }
Binder       → resolves columns, infers filter types
Planner      → ScanPlan { Table=<resolved>, Filter=<compiled predicate> }
Executor     → table.GetAllRows() filtered by predicate
Result       → rows returned to CLI for display
```

---

## 7. Extension Points

| Extension             | Interface / Location                     | Notes                                                     |
| --------------------- | ---------------------------------------- | --------------------------------------------------------- |
| Alternative storage   | `ITableStorage`                          | Implement the interface; inject into `Table`              |
| New column types      | `DataFrameTableStorage.AddColumn` switch | Extend the type dispatch; add corresponding test coverage |
| SQL syntax extensions | AST node hierarchy                       | Add new statement types; extend parser and executor       |
| Query optimisation    | Planner pipeline                         | Insert optimisation passes between planning and execution |
| Result formatting     | CLI output layer                         | Format results independently of execution                 |
| Persistence           | `SQLease.Storage` project                | Implement `ITableStorage` with file I/O                   |

---

## 8. Design Rationale

### Why `ITableStorage`?

Hiding the storage implementation behind an interface decouples the domain model from the persistence mechanism. `Table` can be tested with a fake storage implementation. The backing store can change from `DataFrame` to a page-based engine without modifying a single line of domain code.

### Why `Microsoft.Data.Analysis.DataFrame` as the current implementation?

`DataFrame` provided typed columnar storage quickly, allowing development of the domain model and test coverage before a custom storage engine existed. It is a pragmatic bootstrap, not the intended final form.

The `DataFrame` library is scoped to `DataFrameTableStorage`. Nothing above that class imports `Microsoft.Data.Analysis`. When the implementation changes, the impact is contained.

### Why tombstone-based deletion?

Physical deletion requires restructuring the underlying data store immediately. Tombstones defer that cost, simplify the delete path, and provide a natural foundation for future recovery, multi-version concurrency, and write-ahead logging patterns. Compaction is the mechanism by which deleted rows are eventually reclaimed.

### Why a multi-project solution?

Project boundaries enforce architectural boundaries. When `SQLease.Engine` cannot import `DataFrameTableStorage` because the class is in `SQLease.Core` and the project has no reference to it, the architecture is structurally enforced rather than relying on convention alone.

The current state — where `Engine` and `Storage` are stubs — is a transitional phase, not a design defect. The boundaries are correct; the implementations are not yet written.

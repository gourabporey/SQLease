# SQLease — SQL Language Specification

This document describes the SQL dialect supported by SQLease, the parsing and execution philosophy, the assumptions the engine makes, and the roadmap for future language extensions.

This is a living document. As the engine evolves, this specification should be updated to reflect the current supported feature set and the intended future direction. Implementors should treat this document as the authoritative description of language intent.

---

## Status Notice

**As of the current implementation, no SQL parsing exists.** The current system accepts programmatic API calls through C# code rather than SQL text. The CLI is a scripted demo.

This specification defines the SQL dialect that the engine is being built to support. It describes the intended parsing and execution model so that contributors implementing the parser, binder, planner, and executor have a clear shared understanding of what the system should eventually accept and how it should behave.

---

## 1. Terminology

| Term           | Definition                                                                                            |
| -------------- | ----------------------------------------------------------------------------------------------------- |
| **Statement**  | A complete SQL command: `SELECT`, `INSERT`, `CREATE TABLE`, etc.                                      |
| **DDL**        | Data Definition Language — statements that define schema: `CREATE TABLE`, `DROP TABLE`                |
| **DML**        | Data Manipulation Language — statements that manipulate data: `INSERT`, `SELECT`, `UPDATE`, `DELETE`  |
| **Expression** | A value-producing construct: a literal, a column reference, or an operator applied to sub-expressions |
| **Predicate**  | A boolean expression used to filter rows, typically in a `WHERE` clause                               |
| **Identifier** | A name that refers to a table or column                                                               |
| **Literal**    | A constant value: `42`, `'Alice'`, `true`, `3.14`, `'2024-01-01'`                                     |
| **Schema**     | The collection of column definitions (name and type) for a table                                      |
| **Catalog**    | The collection of all table definitions in a database                                                 |
| **Tombstone**  | A hidden boolean column used to mark rows as logically deleted without immediate physical removal     |
| **Compaction** | The process of physically removing tombstoned rows from storage                                       |
| **Binding**    | The phase in which identifiers are resolved to concrete schema objects and types are verified         |
| **Plan**       | A structured representation of the work required to execute a statement                               |

---

## 2. Supported SQL Subset

SQLease targets a focused subset of ANSI SQL. The goal is correctness within a defined scope rather than broad but shallow compatibility.

### 2.1 Data Types

The following types are supported in column definitions and literal expressions:

| SQL Type   | CLR Type      | Notes                          |
| ---------- | ------------- | ------------------------------ |
| `INT`      | `int` (Int32) | 32-bit signed integer          |
| `TEXT`     | `string`      | Variable-length Unicode string |
| `BOOLEAN`  | `bool`        | `true` or `false`              |
| `DOUBLE`   | `double`      | 64-bit IEEE floating-point     |
| `DATETIME` | `DateTime`    | Date and time without timezone |

Type names are case-insensitive. `INT`, `int`, and `Int` are equivalent.

**Not yet supported:** `BIGINT`, `DECIMAL`, `FLOAT`, `VARCHAR(n)`, `CHAR(n)`, `DATE`, `TIME`, `TIMESTAMP WITH TIME ZONE`, `BLOB`, `JSON`, `NULL` as a standalone type.

### 2.2 DDL Statements

#### CREATE TABLE

```sql
CREATE TABLE <table_name> (
    <column_name> <type>,
    <column_name> <type>,
    ...
);
```

Rules:

- Table names are case-insensitive. `Users` and `users` refer to the same table.
- Column names within a table are case-sensitive.
- Duplicate table names in the same database are rejected.
- Duplicate column names within the same `CREATE TABLE` are rejected.
- System columns (such as `__Deleted`) must not be specified by the user.

Example:

```sql
CREATE TABLE users (
    id INT,
    username TEXT,
    email TEXT,
    date_of_birth DATETIME
);
```

**Not yet supported:** `PRIMARY KEY`, `NOT NULL`, `UNIQUE`, `DEFAULT`, `FOREIGN KEY`, `CHECK`, `IF NOT EXISTS`.

#### DROP TABLE

```sql
DROP TABLE <table_name>;
```

Rules:

- Dropping a non-existent table is an error.
- `DROP TABLE IF EXISTS` is a planned extension.

**Status:** Not yet implemented.

### 2.3 DML Statements

#### INSERT

```sql
INSERT INTO <table_name> (<column_list>) VALUES (<value_list>);
```

Rules:

- The column list must include every non-nullable column.
- The value list must have the same number of elements as the column list.
- Values must be type-compatible with their target columns.
- System columns may not be specified in an insert.

Example:

```sql
INSERT INTO users (id, username, email, date_of_birth)
VALUES (1, 'alice', 'alice@example.com', '2001-12-12');
```

**Not yet supported:** Positional inserts without a column list, multi-row inserts, `INSERT INTO ... SELECT ...`.

#### SELECT

```sql
SELECT <column_list | *>
FROM <table_name>
[WHERE <predicate>];
```

Rules:

- `*` selects all user-visible columns. System columns are never returned in a `SELECT *`.
- An explicit column list must name only columns that exist in the target table.
- `WHERE` predicates are boolean expressions (see Section 3).
- Column aliases (`AS`) are a planned extension.

Example:

```sql
SELECT id, username FROM users WHERE id = 1;
SELECT * FROM users;
```

**Not yet supported:** `JOIN`, `GROUP BY`, `HAVING`, `ORDER BY`, `LIMIT`, `OFFSET`, subqueries, `DISTINCT`, aggregate functions.

#### UPDATE

```sql
UPDATE <table_name>
SET <column_name> = <expression> [, ...]
[WHERE <predicate>];
```

Rules:

- Without a `WHERE` clause, all rows are updated.
- The `SET` list must reference only existing, user-visible columns.
- System columns may not be updated by the user.
- Expression values must be type-compatible with their target columns.

Example:

```sql
UPDATE users SET email = 'new@example.com' WHERE id = 1;
```

**Not yet supported:** Subquery expressions in `SET`, `UPDATE ... FROM`.

#### DELETE

```sql
DELETE FROM <table_name>
[WHERE <predicate>];
```

Rules:

- Without a `WHERE` clause, all rows are deleted.
- Deletion is tombstone-based: rows are marked logically deleted and remain physically in storage until compaction.

Example:

```sql
DELETE FROM users WHERE id = 1;
```

**Not yet supported:** `TRUNCATE TABLE` (which bypasses tombstones and physically removes all rows immediately).

---

## 3. Expression and Predicate Language

### 3.1 Literals

| Type     | Example                                    |
| -------- | ------------------------------------------ |
| Integer  | `42`, `-7`, `0`                            |
| Text     | `'Alice'`, `'hello world'`                 |
| Boolean  | `true`, `false`                            |
| Double   | `3.14`, `-0.5`                             |
| DateTime | `'2024-01-15'` (parsed from ISO 8601 text) |
| Null     | `NULL` (planned)                           |

### 3.2 Column References

A column reference is an unquoted identifier that resolves to a column in the current statement's target table.

```sql
WHERE id = 1
WHERE username = 'alice'
```

Multi-table references (`table.column`) are not required for the single-table subset but should be supported as the language grows.

### 3.3 Comparison Operators

| Operator     | Meaning               |
| ------------ | --------------------- |
| `=`          | Equal                 |
| `<>` or `!=` | Not equal             |
| `<`          | Less than             |
| `>`          | Greater than          |
| `<=`         | Less than or equal    |
| `>=`         | Greater than or equal |

Comparisons between incompatible types are a binding error, not a runtime error. The binder must catch type mismatches before execution.

### 3.4 Logical Operators

| Operator | Meaning                           |
| -------- | --------------------------------- |
| `AND`    | Both operands must be true        |
| `OR`     | At least one operand must be true |
| `NOT`    | Negate a boolean expression       |

Operator precedence (highest to lowest): `NOT`, `AND`, `OR`. Parentheses may override precedence.

### 3.5 Arithmetic Operators (Planned)

`+`, `-`, `*`, `/` on numeric types. Division by zero is a runtime error.

### 3.6 String Operations (Planned)

`LIKE` for pattern matching with `%` (any sequence) and `_` (single character).

---

## 4. Parsing Philosophy

### The parser is a pure transformer

The SQL parser converts text into an AST. It has no knowledge of the database state. It does not know whether a table exists, whether a column name is valid, or whether a type is compatible. All of that belongs to the binder.

A parser that returns a correctly structured AST for syntactically valid input and a syntax error for invalid input is a correct parser, regardless of whether the identifiers in the SQL correspond to real tables.

### Parse errors are syntax errors

A parse error means the input does not conform to the grammar. It is not a semantic failure. Parse errors are reported with the position in the input (line and column number) and a description of what was expected.

### The grammar is unambiguous

The SQLease grammar must be unambiguous at every point. Ambiguous grammars require lookahead heuristics and produce surprising error messages. Where SQL permits ambiguous constructs, SQLease resolves the ambiguity by restricting the syntax to the most common form.

### The parser does not recover speculatively

When a syntax error is encountered, the parser reports the error and stops. It does not attempt to "fix" the input or continue from a guessed recovery point. Error recovery is a future concern and must not be introduced speculatively.

### Keywords are case-insensitive

`SELECT`, `select`, and `Select` are equivalent. User-provided identifiers (table and column names) have their own case sensitivity rules (table names: case-insensitive; column names: case-sensitive within a table).

---

## 5. Execution Philosophy

### Execution never receives raw SQL

The executor receives a bound, typed plan. It does not parse SQL. It does not make schema decisions. Its only job is to carry out a plan by calling domain operations.

### The executor is the only component that mutates state

All state mutations — row insertion, row deletion, row update, table creation — are initiated by the executor. The parser, binder, and planner are all read-only with respect to the database state.

### Execution errors are distinct from parse and bind errors

- **Parse errors:** The input does not conform to the grammar.
- **Bind errors:** The input is syntactically valid but semantically incorrect (unknown column, type mismatch).
- **Execution errors:** The plan is valid but a runtime condition prevents it (constraint violation, storage failure).

These three error categories have different recovery implications and must be reported distinctly.

### Result sets are read-only values

A `SELECT` query returns a result set. The result set is a value — a collection of rows — not a cursor into live storage. Mutations to the database after the query executes do not affect an already-returned result.

---

## 6. Identifier Rules

- **Table names** are case-insensitive. `Users`, `users`, and `USERS` all refer to the same table.
- **Column names** are case-sensitive within a table definition. `Id` and `id` are different columns if both are declared.
- **Reserved words** may not be used as unquoted identifiers. A future extension may support quoted identifiers (e.g., `"select"` as a column name) using double quotes.
- **System columns** begin with `__` (double underscore). User-defined column names must not begin with `__`.

---

## 7. System Columns

System columns are implementation details of the storage layer. They must not be visible to SQL users.

| Column      | Type      | Purpose                                |
| ----------- | --------- | -------------------------------------- |
| `__Deleted` | `BOOLEAN` | Tombstone marker for soft-deleted rows |

Rules:

- System columns must not appear in `SELECT *` output.
- System columns must not be named in `INSERT`, `UPDATE`, or `CREATE TABLE` statements by users.
- The system column namespace (`__`-prefixed names) is reserved.

---

## 8. Error Reporting

All errors must include:

- The error category (syntax, semantic, execution).
- A human-readable description of the problem.
- For parse errors: the position in the input (line and column).
- For bind errors: the name of the unresolved identifier or incompatible expression.
- For execution errors: the operation that failed and why.

Errors must not expose internal implementation details such as stack traces or storage class names.

---

## 9. Assumptions and Constraints

These assumptions define the scope of the current implementation and the simplifications that were made deliberately.

- **Single-database scope.** SQLease currently manages one database. Multi-database support (`USE database_name`) is not planned.
- **No transactions.** There is no `BEGIN`, `COMMIT`, or `ROLLBACK`. All operations are immediately applied. This will change when persistence is introduced.
- **No concurrency.** The engine assumes a single-threaded access model. Thread safety is not a current requirement.
- **No constraints.** `NOT NULL`, `UNIQUE`, `PRIMARY KEY`, and `FOREIGN KEY` are not enforced. Constraint support is a future milestone.
- **No indexing.** All queries are full scans. Index support is deferred.
- **No persistence.** All data exists in memory and is lost when the process exits. File-backed storage is planned.
- **No query optimisation.** Plans are executed as produced. No cost estimation, join reordering, or predicate pushdown is performed.

---

## 10. Feature Roadmap Placeholders

The following features are part of the long-term scope of SQLease. They are listed here to ensure that early design decisions account for them without implementing them prematurely.

### Near-term

- [ ] SQL parser (lexer + parser + AST)
- [ ] Semantic binder (identifier resolution, type checking)
- [ ] Query planner (AST → logical plan)
- [ ] Query executor (logical plan → domain operations)
- [ ] REPL in the CLI (interactive SQL prompt)
- [ ] `SELECT` with `WHERE`
- [ ] `DROP TABLE`
- [ ] `SELECT` with column projection (named columns instead of `*` only)

### Medium-term

- [ ] `NOT NULL` constraint enforcement
- [ ] `PRIMARY KEY` constraint with uniqueness enforcement
- [ ] `CREATE TABLE IF NOT EXISTS` / `DROP TABLE IF EXISTS`
- [ ] Multi-row `INSERT`
- [ ] `TRUNCATE TABLE`
- [ ] `ORDER BY` clause
- [ ] `LIMIT` and `OFFSET`
- [ ] Aggregate functions: `COUNT`, `SUM`, `MIN`, `MAX`, `AVG`
- [ ] `GROUP BY` and `HAVING`

### Long-term

- [ ] File-backed persistent storage
- [ ] Write-ahead log for crash recovery
- [ ] B-tree index structure
- [ ] `JOIN` (initially `INNER JOIN`)
- [ ] Subqueries in `WHERE` (`IN`, `EXISTS`)
- [ ] `UNIQUE` and `FOREIGN KEY` constraints
- [ ] `DEFAULT` values
- [ ] Multi-statement transactions (`BEGIN`, `COMMIT`, `ROLLBACK`)
- [ ] `EXPLAIN` plan output

---

## 11. Future Extensibility

The language specification is designed to grow without breaking the existing execution model.

### Adding a new statement type

1. Define the grammar production in the parser.
2. Add a corresponding AST node (a new record or sealed class in the AST hierarchy).
3. Add a binder rule to resolve identifiers in the new statement.
4. Add a new plan node in the planner.
5. Add an execution handler in the executor.
6. Add tests at each layer.

No existing statement's handler should need modification to accommodate a new statement type.

### Adding a new expression type

1. Add the token in the lexer.
2. Add the grammar production in the parser.
3. Add an AST node for the expression.
4. Add type-inference rules in the binder.
5. Add evaluation logic in the executor.
6. Add tests.

### Adding a new data type

1. Add the SQL keyword to the type parser.
2. Map it to a CLR type in the schema model.
3. Extend `DataFrameTableStorage` to support the new `DataFrameColumn` type.
4. Add literal parsing support in the expression parser.
5. Add type-compatibility rules in the binder.
6. Add tests.

---

## 12. Non-Goals

The following are explicitly out of scope for SQLease and should not be pursued:

- Full ANSI SQL compliance.
- Production-grade performance or throughput.
- Distributed query execution.
- Column store analytics optimisation.
- Compatibility with existing SQL drivers (ODBC, ADO.NET provider).
- Multi-user authentication and authorisation.

SQLease is an educational engine. Scope discipline is essential to its value as a learning tool.

# SQLease — Coding Standards

This document defines engineering quality expectations for code written in this repository.

---

## 1. Naming

Names are the primary mechanism for communicating intent. A good name eliminates the need for a comment.

### Principles

- **Names describe purpose, not implementation.**
- **Names use domain language.**
- **Names are as long as necessary, but no longer.** `UpdateRows` is clear. `UpdateAllRowsInTheStorageBackend` is not clearer, just noisier.
- **Booleans are affirmative predicates.** `includeDeleted`, `isNullable`, `tableExists`. Not `noDeleted`, `notNullable`.
- **Interfaces are named for the role they play.**

### Consistency

- Names must be consistent with existing names in the same layer. If storage operations are called `InsertRow`, `UpdateRows`, `DeleteRows`, a new operation is `SelectRows` or `GetAllRows` — not `FetchData` or `QueryRecords`.
- Abbreviations are not permitted unless they are standard domain terminology (`SQL`, `AST`, `DDL`, `DML`).

---

## 2. Function and Method Size

**A method should do one thing.** If a method requires a comment to explain a section of its own body, that section is a candidate for extraction into a named method.

### Guidelines

- Methods that exceed 20 lines are worth reviewing. They may be doing too much.
- Methods that require 2 levels of nested conditionals or loops are worth simplifying.
- Helper methods extracted from a single caller are acceptable when they improve readability or allow independent testing.
- Do not extract methods purely to reduce line count if the extraction adds no clarity.

### In SQLease

`DataFrameTableStorage.Compact()` is a reasonable size and reads clearly. The type dispatch switch expression is repetitive but intentionally explicit. A future improvement might extract the column-construction logic into a shared method to eliminate the duplication, but the current form is acceptable.

---

## 3. Class Responsibilities

**Each class has one reason to change.**

### Guidelines

- Classes in `Models/` are data containers or domain entities.
- Classes in `Storage/` are responsible for physical data manipulation.
- Classes in `CLI/` are responsible for input parsing (not SQL parsing) and output formatting. They should not call storage directly.
- Test classes test exactly one subject class. Do not share test classes across multiple subjects.

### Current Deficiencies

- `Table` constructs its own `DataFrameTableStorage` internally (`new DataFrameTableStorage()`). This hardcodes the implementation, prevents testing with a fake, and violates the dependency inversion principle. The implementation should be injected.
- `Database` co-ordinates the `__Deleted` column name. This is a storage concern and should be encapsulated fully inside `ITableStorage`.

---

## 4. Public APIs

Public APIs are commitments. Once callers exist, breaking changes impose cost on those callers.

### Guidelines

- **Minimise surface area.** Expose only what callers need.
- **Design the API before the implementation.** Write the method signature and its test before writing the body.
- **Return types should reflect intent.** `GetAllRows()` returns `IEnumerable<Dictionary<string, object?>>`. This is acceptable today but should eventually return a typed result object that does not expose internal implementation details (dictionaries are mutable and untyped).
- **Do not expose internal state.** `_tables` is correctly `private` with a `IReadOnlyDictionary` property on `Database`.
- **Method parameters that act as output should be avoided.** Prefer returning a result object over `out` parameters.

---

## 5. Documentation and Comments

Code should communicate its intent through names and structure. Comments are for intent that cannot be expressed in code.

### When to write a comment

- A constraint exists that is not obvious from the code.
- A non-obvious design decision was made.
- A known deficiency exists and cannot be fixed immediately. Mark it with a `// TODO:` comment that explains the problem and the intended fix.

### When not to write a comment

- Do not restate what the code does.
- Do not write apology comments.
- Do not write historical comments.

### TODO comments

`TODO` comments are acceptable only when they describe a concrete, bounded problem. A good `TODO` includes:

- What the problem is.
- What the correct fix is.
- Why the fix is deferred.

Example:

```csharp
// TODO: _deletedColumn is static, causing cross-instance coupling.
// Make it an instance field initialised in the constructor.
```

---

## 6. Error Handling

Errors fall into two categories: expected failures from external input and unexpected failures from internal logic.

### Expected failures

- Invalid SQL syntax, missing columns in an insert, and duplicate table names are expected failures.
- Use typed exceptions (`InvalidOperationException`, `ArgumentException`) with descriptive messages that name the offending input. "Table Users already exists" is correct. "Error" is not.
- Do not swallow exceptions.

### Unexpected failures

- A `NotSupportedException` for an unsupported type is acceptable. The error message must name the unsupported value.
- Null reference failures arising from missing null checks in internal code are internal bugs, not user-facing errors. Fix the null check rather than wrapping in a catch.

### What not to do

- Do not use exceptions for control flow.
- Do not return null from a method that could return a valid empty result.
- Do not throw generic `Exception` with a string message. Use the most specific exception type that describes the failure.

---

## 7. Logging

Logging does not currently exist in the codebase. When introduced, follow these guidelines.

- **Log at the boundary.** Log incoming requests and outgoing results at the CLI and engine boundary. Do not log inside storage loops.
- **Log the decision, not the data.** "Executing INSERT into Users" is useful. Dumping all row values is expensive and leaks user data.
- **Do not log inside hot paths.** Row-level storage operations must not produce log output per row.
- **Structured logging over string formatting.** When a logging framework is introduced, use structured properties rather than interpolated strings.

---

## 8. Dependency Management

Dependencies are liabilities as well as assets. Every external library is a security surface, a version conflict risk, and a maintenance burden.

### Guidelines

- **Add dependencies with justification.** Before introducing a new NuGet package, confirm that the functionality is not already available in the standard library or in an existing dependency.
- **Keep dependencies at their appropriate layer.**
- **Use the minimum required version.** Do not pull in a large framework when a small, focused library suffices.
- **Test dependencies belong in test projects only.** xUnit, Moq, and coverlet must not appear as references in production projects.

### Current state

| Package                   | Project         | Justification                                  |
| ------------------------- | --------------- | ---------------------------------------------- |
| `Microsoft.Data.Analysis` | `SQLease.Core`  | Backs `DataFrameTableStorage` columnar storage |
| `xunit`                   | `SQLease.Tests` | Test framework                                 |
| `coverlet.collector`      | `SQLease.Tests` | Code coverage collection                       |

---

## 9. Abstractions

An abstraction earns its existence by serving two or more concrete uses or by isolating a volatile decision.

### Guidelines

- **Do not create an interface for a concept that has only one implementation and no test double.**
- **Interfaces should be narrow.**
- **Prefer concrete types internally.**
- **Do not hide simplicity.** A one-line operation does not need a wrapping service class.

### In SQLease

`ITableStorage` is correct: it has a single concrete implementation (`DataFrameTableStorage`) plus a test-double use case. The interface is appropriately scoped to storage operations. When `SQLease.Storage` gains a file-backed implementation, the abstraction will have served two real purposes.

---

## 10. Code Duplication

Duplication of knowledge — not code — is the real problem.

### Guidelines

- **Do not deduplicate code that changes for different reasons.**
- **Do deduplicate knowledge.**
- **Constants belong close to the concept they name.**

---

## 11. Configuration

- **Do not hardcode values that might change between environments.**
- **Do not use magic strings for structural names.**
- **Constants are preferable to literals.**

---

## 12. Testing Standards

Tests are first-class code. They must be held to the same quality standards as production code.

### Structure

- One test class per subject class, placed in the corresponding namespace under `SQLease.Tests`.
- Test method names follow the pattern: `SubjectAction_ExpectedOutcome`. Examples: `InsertRow_ShouldInsertAndReturnOneRow`, `DeleteRows_ShouldTombstoneRows`.
- Each test exercises one behaviour. Multiple `Assert` calls are acceptable when they jointly verify one logical condition.

### Setup and isolation

- Each test creates its own state. Tests must not share mutable state across methods.
- Do not rely on test execution order. xUnit does not guarantee order.
- Use factory methods (like the existing `CreateSampleStorage()`) to reduce setup boilerplate while keeping tests independent.

### Coverage expectations

- All public methods of a class must have at least one test for their primary success path.
- Error conditions must be tested: duplicate columns, missing values, non-existent tables.
- Compaction invariants must be tested with `includeDeleted=true` to verify physical removal.

### What to avoid

- Do not write tests that only verify internal state. Tests should observe behaviour through the public API.
- Do not suppress test output or skip tests with `[Skip]` without a documented, time-bounded reason.
- Do not leave the `UnitTest1.cs` placeholder in the test project. It must be replaced or deleted.

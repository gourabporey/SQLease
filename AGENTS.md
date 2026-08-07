# SQLease — Agent Instructions

This document is the canonical guide for any automated coding agent working in this repository. Read it in full before modifying any code. When in doubt, re-read the relevant section rather than guessing.

---

## 1. Project Overview

SQLease is an educational SQL database engine implemented in C# on .NET 9. The goal is to build — from scratch — a system that accepts SQL text, parses it into a structured representation, plans and optimises execution, then carries out data operations against a storage layer.

The project is deliberately scoped to be a learning vehicle. It is not a production database. However, it is expected to be engineered as if it were: clean boundaries, tested behaviour, honest documentation, and decisions that can be defended.

The current state of the codebase is a **functional in-memory table store** with a programmatic API. A SQL parser, AST, query planner, and execution engine have not yet been built. The multi-project solution structure is already in place to guide where those components belong.

---

## 2. Project Goals

| Priority | Goal                                                                                      |
| -------- | ----------------------------------------------------------------------------------------- |
| 1        | Build a correct, well-tested SQL engine from foundational principles                      |
| 2        | Demonstrate clean architecture in a real C# project                                       |
| 3        | Serve as a reference implementation for database internals education                      |
| 4        | Keep the codebase simple enough that a single contributor can understand the whole system |
| 5        | Support incremental extension without large-scale rewrites                                |

Performance optimisation is intentionally last. Do not introduce complexity in pursuit of speed unless a bottleneck has been identified and measured.

---

## 3. Architecture Overview

SQLease is structured as a layered pipeline. Each layer has a single responsibility and communicates with adjacent layers only through explicitly defined interfaces. Layers never reach across boundaries.

```
┌─────────────────────────────┐
│         SQLease.CLI          │  User-facing entry point
└────────────┬────────────────┘
             │ text commands
┌────────────▼────────────────┐
│       SQL Parser (planned)   │  Text → AST
└────────────┬────────────────┘
             │ AST nodes
┌────────────▼────────────────┐
│     Query Planner (planned)  │  AST → Logical Plan
└────────────┬────────────────┘
             │ logical plan
┌────────────▼────────────────┐
│      SQLease.Engine          │  Plan → Physical Operations
└────────────┬────────────────┘
             │ domain model operations
┌────────────▼────────────────┐
│        SQLease.Core          │  Database, Table, Column, Row, ITableStorage
└────────────┬────────────────┘
             │ storage primitives
┌────────────▼────────────────┐
│       SQLease.Storage        │  Physical storage implementations
└─────────────────────────────┘
```

**Currently implemented:** `SQLease.Core` contains the domain model and the only real storage implementation (`DataFrameTableStorage`). All other layers are stubs or placeholders.

See [docs/architecture.md](docs/architecture.md) for full detail on module responsibilities, dependency direction, and extension points.

---

## 4. Engineering Philosophy

The priorities below apply in order. When two options satisfy higher-priority properties equally, prefer the lower-priority one.

1. **Correctness** — Incorrect behaviour is never acceptable, regardless of simplicity or performance gains.
2. **Simplicity** — The simplest implementation that is correct is preferred. Complexity is a liability.
3. **Maintainability** — Code is read more often than it is written. Optimise for the reader.
4. **Extensibility** — Design for predictable extension, not speculative generalisation.
5. **Testability** — Testable code is almost always better-designed code; use it as a quality signal.
6. **Performance** — Optimise only when a measured bottleneck justifies the added complexity.

These priorities mean: do not sacrifice correctness for speed, do not add abstractions speculatively, and do not introduce performance tricks before profiling.

See [docs/design-principles.md](docs/design-principles.md) for detailed rationale.

---

## 5. Decision Framework

Whenever multiple implementation options exist, evaluate them in this order:

| Step                     | Question                                                                         |
| ------------------------ | -------------------------------------------------------------------------------- |
| 1. Correctness           | Does this produce accurate results for all valid inputs and reject invalid ones? |
| 2. Existing architecture | Does this fit within the current layer boundaries and interfaces?                |
| 3. Simplicity            | Is there a simpler approach that satisfies steps 1 and 2?                        |
| 4. Testability           | Can behaviour be verified in isolation without complex test setup?               |
| 5. Maintainability       | Will a future contributor understand this code without additional context?       |
| 6. Extensibility         | Will this allow predictable extension without requiring a rewrite?               |
| 7. Performance           | Is this measurably faster and is that improvement worth the added complexity?    |

If options are equivalent through step 6, choose the one with the higher performance only if the complexity cost is zero or near-zero.

Preserve consistency with the existing codebase unless there is a compelling architectural reason not to. Deviation requires an explicit comment or commit note explaining the rationale.

---

## 6. Repository Structure

```
SQLease.sln                   Solution root
├── SQLease.CLI/              Entry point — CLI, user interaction
├── SQLease.Core/             Domain model — Table, Row, Column, Database, ITableStorage
│   ├── Models/               Value objects and domain entities
│   └── Storage/              ITableStorage interface and current DataFrame implementation
├── SQLease.Engine/           Query execution (stub — future home of the executor)
├── SQLease.Storage/          Physical storage implementations (stub — future persistence layer)
├── SQLease.Tests/            All test projects, mirroring source structure
│   └── SQLease.Core/
│       └── Storage/          Tests for DataFrameTableStorage
├── docs/                     Architecture and engineering documentation
└── AGENTS.md                 This file
```

### Placement Rules

- **New domain types** (schema objects, value objects, catalog entries) belong in `SQLease.Core/Models/`.
- **New storage implementations** belong in `SQLease.Core/Storage/` until `SQLease.Storage` is activated as a real project.
- **SQL parsing logic** belongs in `SQLease.Core` or a dedicated `SQLease.Parser` project when introduced; it must never exist in `SQLease.Engine` or `SQLease.CLI`.
- **Execution logic** belongs in `SQLease.Engine`.
- **CLI logic** belongs in `SQLease.CLI`; it must never contain business logic.
- **All tests** belong under `SQLease.Tests`, mirroring the source namespace they test.

---

## 7. Development Workflow

Before making any change, follow these steps in order:

1. **Understand the architecture.** Read [docs/architecture.md](docs/architecture.md) and locate the layer that owns the change.
2. **Inspect related modules.** Read every file that the changed code will interact with. Do not guess at behaviour.
3. **Identify existing abstractions.** Prefer extending an existing interface over introducing a new one.
4. **Scope the change narrowly.** Implement only what is required. Unrelated improvements belong in a separate change.
5. **Write tests first or alongside.** New behaviour must be accompanied by tests. Bug fixes must be accompanied by regression tests.
6. **Verify the build.** Run `dotnet build` from the solution root and confirm zero errors and zero new warnings.
7. **Run the full test suite.** Run `dotnet test` from the solution root and confirm all tests pass.

### Building and Testing

```bash
# Build the full solution
dotnet build SQLease.sln

# Run all tests
dotnet test SQLease.sln

# Run a specific test project
dotnet test SQLease.Tests/SQLease.Tests.csproj

# Run the CLI
dotnet run --project SQLease.CLI
```

---

## 8. Coding Expectations

- **Each type has one responsibility.** If a class requires two explanatory sentences, it likely has two responsibilities.
- **Interfaces define contracts, not implementation details.** `ITableStorage` expresses what storage can do, not how it does it.
- **Do not introduce concrete dependencies between layers.** The CLI must not reference `DataFrameTableStorage`. The engine must not reference `Microsoft.Data.Analysis`.
- **Nullable reference types are enabled.** Null must not flow across boundaries silently. Handle or propagate explicitly.
- **Avoid mutable static state.** The `_deletedColumn` static field in `DataFrameTableStorage` is a known deficiency; do not repeat the pattern.
- **Magic strings are not acceptable in new code.** Constants or well-named types must represent structural names such as system column names.
- **Schema metadata must have a single authoritative source.** The current duplication between `Table.Columns` and `DataFrameTableStorage._schema` is a known deficiency.

See [docs/coding-standards.md](docs/coding-standards.md) for detailed guidance.

---

## 9. Testing Expectations

- **Every public behaviour must be testable in isolation.** If a unit cannot be tested without starting the CLI, the design has a boundary problem.
- **Tests describe behaviour, not implementation.** Test names should read as statements: `DeleteRows_ShouldTombstoneRows`, not `TestDelete`.
- **Each test verifies one logical assertion.** Multiple `Assert` calls are acceptable when they jointly verify a single invariant.
- **The `UnitTest1.cs` placeholder must be replaced** before shipping any real feature. Its existence indicates an incomplete setup.
- **Regression tests are mandatory for bug fixes.** The test must fail before the fix and pass after.
- **Tests must not depend on execution order.** Each test must set up its own state independently.
- **Deleting or disabling tests is forbidden** unless the behaviour they test has been intentionally removed and the removal is documented.

---

## 10. Architectural Invariants

These properties must hold at all times. Violating them constitutes an architectural defect, not a style issue.

| Invariant                                          | Rationale                                                                                                   |
| -------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Parsing never executes queries                     | Parsing is a pure transformation from text to structure; side effects invalidate error recovery and testing |
| Execution never parses SQL                         | The execution layer receives a structured plan, not raw text; mixing these collapses testability            |
| Storage is independent of the CLI                  | The storage layer must be usable without a terminal; this is required for testing and future embedding      |
| Layers communicate only through defined interfaces | Direct concrete dependencies couple layers and make substitution impossible                                 |
| Illegal states must be difficult to represent      | Prefer types that cannot encode invalid data over runtime validation of valid-looking types                 |
| System columns are fully internal to storage       | Columns such as `__Deleted` must not be visible above the storage layer                                     |
| Public APIs must remain stable                     | Callers must not be broken by internal refactoring; API changes require explicit review                     |
| Schema metadata has one authoritative source       | Duplicated schema state will diverge; designate one location as the source of truth                         |
| Behaviour must be deterministic                    | Identical inputs must always produce identical outputs; avoid hidden state and uncontrolled randomness      |
| Side effects must be minimised and localised       | State mutations and I/O must not leak into layers that should be pure                                       |

---

## 11. Forbidden Actions

The following are prohibited regardless of context. If a task seems to require one of these, stop and reconsider the approach.

- **Unrelated refactoring.** Do not improve code that is not directly related to the task at hand.
- **Speculative abstractions.** Do not introduce interfaces, base classes, or generic wrappers for cases that do not yet exist.
- **Dead code.** Do not leave unreachable code, commented-out logic, or unused methods in the codebase.
- **Duplicate implementations.** Do not create a second version of logic that already exists; extend or fix the existing one.
- **Disabling or deleting tests.** Tests may only be removed when the behaviour they cover has been explicitly removed.
- **Suppressing compiler warnings instead of fixing them.** Warnings indicate real problems; suppress only with a documented, permanent justification.
- **Introducing new mutable static state.** Static mutable state creates hidden coupling and unpredictable behaviour across tests.
- **Crossing layer boundaries directly.** The CLI must not call storage methods. The engine must not call parsing methods.
- **Adding NuGet packages without justification.** Each dependency is a maintenance liability; add only what is required and document why.
- **Breaking public API contracts without a documented reason.** Method signatures and interface contracts are commitments to callers.
- **Mixing responsibilities across layers.** Business logic does not belong in the CLI; parsing logic does not belong in the engine.

---

## 12. Output Expectations

When modifying this codebase, the expected output is:

- A compiling solution with zero new errors (`dotnet build` succeeds).
- All existing tests pass (`dotnet test` succeeds).
- New behaviour is covered by new tests.
- No new compiler warnings unless they were pre-existing and documented.
- Code that is consistent in style, naming, and structure with the surrounding files.
- No unrelated changes to files outside the scope of the task.
- A clear understanding of which architectural invariants were considered and how the change respects them.

If a task cannot be completed without violating an invariant or a forbidden action, document the conflict explicitly rather than proceeding silently.

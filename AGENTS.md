# SQLease — Agent Instructions

SQLease is an educational SQL database engine in C# on .NET 9. The goal is to build, from scratch, a system that accepts SQL text, parses it into a structured representation, plans execution, and carries out data operations against a storage layer. It is engineered as if it were production software: clean boundaries, tested behaviour, honest documentation.

**Current state:** a functional in-memory table store with a programmatic C# API. A SQL parser, AST, query planner, and executor have not yet been built.

---

## Engineering Priorities

In order of precedence — when two options satisfy all higher priorities equally, prefer the lower one:

1. **Correctness** — incorrect behaviour is never acceptable
2. **Simplicity** — the simplest correct solution is preferred; complexity is a liability
3. **Maintainability** — optimise for the reader
4. **Extensibility** — predictable extension, not speculative generalisation
5. **Testability** — untestable code is a design signal
6. **Performance** — only when a measured bottleneck justifies the complexity

See [docs/design-principles.md](docs/design-principles.md) for detailed rationale and the decision framework for choosing between options.

---

## Architectural Invariants

These must hold at all times. Violation is an architectural defect, not a style issue.

| Invariant                                          | Rationale                                                                         |
| -------------------------------------------------- | --------------------------------------------------------------------------------- |
| Parsing never executes queries                     | Pure text→structure transformation; side effects break error recovery and testing |
| Execution never parses SQL                         | The executor receives a structured plan, not raw text                             |
| Storage is independent of the CLI                  | Storage must be usable without a terminal                                         |
| Layers communicate only through defined interfaces | Concrete cross-layer dependencies make substitution impossible                    |
| Illegal states must be difficult to represent      | Prefer types that cannot encode invalid data                                      |
| System columns are fully internal to storage       | `__Deleted` and similar must not be visible above the storage layer               |
| Public APIs must remain stable                     | API changes require explicit review; callers must not be broken silently          |
| Schema metadata has one authoritative source       | Duplicated schema state will diverge                                              |
| Behaviour must be deterministic                    | Identical inputs must always produce identical outputs                            |
| Side effects must be minimised and localised       | I/O and mutations must not leak into pure layers                                  |

---

## Forbidden Actions

If a task appears to require one of these, stop and reconsider the approach.

- **Unrelated refactoring** — scope changes narrowly to what is asked
- **Speculative abstractions** — no interfaces or base classes for cases that do not yet exist
- **Dead code** — no unreachable code, commented-out logic, or unused members
- **Duplicate implementations** — extend or fix existing logic, do not copy it
- **Disabling or deleting tests** — only when the tested behaviour has been explicitly removed
- **Suppressing compiler warnings** — fix the cause; suppress only with documented justification
- **Introducing mutable static state** — creates hidden coupling and test interference
- **Crossing layer boundaries directly** — CLI must not call storage; engine must not call the parser
- **Adding NuGet packages without justification** — each dependency is a maintenance liability
- **Breaking public API contracts without a documented reason**

---

## Build and Test

```bash
dotnet build SQLease.sln            # zero errors and zero new warnings required
dotnet test SQLease.sln             # all existing tests must pass
dotnet run --project SQLease.CLI
```

New behaviour requires new tests. Bug fixes require a regression test that fails before the fix and passes after.

---

## Code Placement

| What                                             | Where                                                                           |
| ------------------------------------------------ | ------------------------------------------------------------------------------- |
| New domain types (schema objects, value objects) | `SQLease.Core/Models/`                                                          |
| New storage implementations                      | `SQLease.Core/Storage/` until `SQLease.Storage` is activated                    |
| SQL parsing logic                                | `SQLease.Core` or a dedicated `SQLease.Parser` project — never in Engine or CLI |
| Query execution logic                            | `SQLease.Engine`                                                                |
| CLI logic (I/O only, no business logic)          | `SQLease.CLI`                                                                   |
| Tests                                            | `SQLease.Tests/`, mirroring the source namespace                                |

---

## Documentation Map

Read the document that covers the area you are working in. Do not load documents that are not relevant to the current task.

| Document                                               | Purpose                                                                            |
| ------------------------------------------------------ | ---------------------------------------------------------------------------------- |
| [docs/architecture.md](docs/architecture.md)           | Layer pipeline, project responsibilities, dependency direction, extension points   |
| [docs/design-principles.md](docs/design-principles.md) | Engineering philosophy, decision framework, principle rationale                    |
| [docs/coding-standards.md](docs/coding-standards.md)   | Naming, method size, class responsibilities, API design, testing standards         |
| [docs/sql-language-spec.md](docs/sql-language-spec.md) | Supported SQL subset, type system, parsing model, language roadmap                 |
| [docs/project-memory.md](docs/project-memory.md)       | Active decisions, constraints, known limitations, open questions, future direction |
| [docs/current-state.md](docs/current-state.md)         | Completed milestones, current milestone, technical debt, planned improvements      |
| [docs/design-rationale.md](docs/design-rationale.md)   | Long-lived reasoning behind significant design choices                             |
| [docs/adr/README.md](docs/adr/README.md)               | ADR index, format guide, and lifecycle                                             |

---

## Repository Memory Workflow

### Before significant work

For changes that touch architecture, module boundaries, layer responsibilities, storage, or project direction:

1. Read `docs/project-memory.md` — active decisions and constraints.
2. Read `docs/current-state.md` — current milestone and known technical debt.
3. Read relevant ADRs in `docs/adr/`.

Not required for single bug fixes, renames, test additions, or small self-contained changes.

### After significant work

Determine whether long-term repository knowledge has changed. Update documentation when the change:

- introduces a new subsystem or significant abstraction
- changes module responsibilities or layer boundaries
- makes an important architectural tradeoff
- reverses or supersedes a previous decision
- accepts significant technical debt
- completes or begins a milestone
- discovers an important limitation or constraint
- changes project priorities or roadmap direction

Do **not** update documentation for formatting, refactoring, renames, comments, dependency updates, routine bug fixes, or test additions.

Include documentation updates in the same change that warrants them.

### What to update

| Change type                                               | Document                   |
| --------------------------------------------------------- | -------------------------- |
| Active decision, constraint, or limitation changed        | `docs/project-memory.md`   |
| Milestone completed or begun; tech debt added or resolved | `docs/current-state.md`    |
| Long-lived design reasoning changed                       | `docs/design-rationale.md` |
| Specific bounded architectural decision made              | New ADR in `docs/adr/`     |

Repository memory assists contributors — it does not replace engineering judgment.

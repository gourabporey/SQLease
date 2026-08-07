# SQLease — Copilot Instructions

SQLease is an educational SQL database engine built in C# on .NET 9. It is a layered pipeline project — SQL text in, data operations out — with most behaviour currently living in `SQLease.Core`. A SQL parser, query planner, and executor have not yet been built.

Read [`AGENTS.md`](../AGENTS.md) first. It contains the engineering priorities, architectural invariants, forbidden actions, build commands, code placement rules, and documentation map. The instructions below summarise the workflow for this surface.

---

## Before Making Significant Changes

For changes that affect architecture, module boundaries, layer responsibilities, storage, or project direction:

1. Read [`docs/project-memory.md`](../docs/project-memory.md) to understand active decisions and constraints.
2. Read [`docs/current-state.md`](../docs/current-state.md) to understand what is in progress and what technical debt exists.
3. Check [`docs/adr/`](../docs/adr/) for any ADRs relevant to the area being changed.
4. Verify that the proposed change is consistent with the architectural invariants in `AGENTS.md`.

This review is not required for small, self-contained changes such as fixing a single bug, renaming a variable, or adding a test for existing behaviour.

---

## Repository Memory

After completing significant work, determine whether long-term project knowledge has changed. Suggest updating the appropriate document when the change:

- introduces a new subsystem, project, or significant abstraction
- changes module responsibilities or layer boundaries
- makes an important architectural tradeoff
- reverses or supersedes a previous design decision
- accepts significant technical debt
- completes or begins a milestone
- discovers an important limitation or constraint
- changes project priorities or roadmap direction

Do not suggest documentation updates for formatting, refactoring, variable renames, dependency bumps, comment changes, routine bug fixes, or test-only changes.

Include documentation updates in the same change that warrants them. See the Repository Memory Workflow in `AGENTS.md` for which document to update.

---

## Build

```bash
dotnet build SQLease.sln     # zero errors and zero new warnings
dotnet test SQLease.sln      # all tests must pass
```

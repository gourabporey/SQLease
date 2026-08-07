# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for SQLease.

An ADR documents a significant architectural decision: what was decided, why it was decided that way, what alternatives were considered, and what consequences the decision carries. ADRs are immutable records. Once accepted, an ADR is not edited — if a decision is reversed or superseded, a new ADR is written that explicitly supersedes the earlier one.

---

## Purpose

ADRs exist to answer the question: _"Why does the system work this way?"_

They are not implementation guides and do not replace code documentation. They capture the reasoning at the moment a decision was made, which is context that cannot be recovered from the code alone.

---

## When to Write an ADR

Write an ADR when:

- A new subsystem or significant abstraction is introduced
- A technology or library is chosen over alternatives
- A non-obvious design pattern is adopted
- A decision reverses or supersedes a previous one
- A tradeoff is made that future contributors might question

Do not write an ADR for:

- Routine implementation details
- Library version bumps
- Formatting or naming conventions (those belong in `coding-standards.md`)
- Decisions that are self-evidently correct and have no meaningful alternatives

---

## ADR Lifecycle

| Status                    | Meaning                                                                     |
| ------------------------- | --------------------------------------------------------------------------- |
| **Proposed**              | Under discussion; not yet adopted                                           |
| **Accepted**              | Decision has been made and is in effect                                     |
| **Deprecated**            | The decision is no longer actively followed but has not been reversed       |
| **Superseded by ADR-NNN** | This ADR has been replaced; see the referenced ADR for the current decision |

---

## Format

Each ADR is a Markdown file named `ADR-NNN-short-title.md` where `NNN` is a zero-padded three-digit sequence number.

```
# ADR-NNN — Title

**Status:** Accepted | Proposed | Deprecated | Superseded by ADR-NNN

## Context

The situation that required a decision. What problem exists, what constraints apply, and what goals must be satisfied. Keep this focused on the *why* — what pressures or needs drove the decision.

## Decision

The decision that was made. State it clearly in one or two sentences, then elaborate as needed. Avoid implementation detail; describe the choice at the level of principle or architecture.

## Alternatives Considered

The options that were evaluated and rejected. For each alternative, explain why it was not chosen. This section prevents future contributors from re-litigating decided questions.

## Consequences

The effects of this decision. Include both beneficial consequences and costs or limitations that are accepted. Be honest about the tradeoffs.
```

---

## Index

| ADR                                     | Title                                                              | Status   |
| --------------------------------------- | ------------------------------------------------------------------ | -------- |
| [ADR-001](ADR-001-dataframe-storage.md) | Use Microsoft.Data.Analysis.DataFrame as in-memory storage backend | Accepted |

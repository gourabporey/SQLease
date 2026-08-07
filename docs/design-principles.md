# SQLease — Design Principles

This document describes the engineering principles that guide decisions in the SQLease codebase. Each principle explains what it means, why it applies here, when to apply it, and when exceptions are reasonable.

These principles are not style preferences. They reflect engineering decisions that have a measurable effect on correctness, maintainability, and the ability to extend the system over time.

---

## 1. Separation of Concerns

**What it means:** Each component has one clearly defined responsibility. A component that handles parsing does not also handle execution. A component that manages storage does not also format output.

**Why it exists:** When a component has multiple responsibilities, changes to one responsibility risk breaking the other. Debugging becomes harder because the cause of a failure is distributed across concerns. Testing requires understanding all intertwined responsibilities at once.

**When it applies:** Always. Every class, every method, every project should be examinable and answerable to: "What is the one thing this does?"

**In SQLease:** The layer structure enforces this principle at the project level. `SQLease.CLI` handles I/O. `SQLease.Core` owns the domain model. `SQLease.Engine` handles execution. The parser handles syntax. The planner handles structure. The executor handles mutation. These responsibilities must not migrate.

**When exceptions are acceptable:** During very early prototyping, concerns may be temporarily combined to validate an idea. That temporary state must be documented and reversed before the code is treated as a reference implementation.

---

## 2. Composition Over Inheritance

**What it means:** Build complex behaviour by combining simple components rather than by deriving new types from existing ones.

**Why it exists:** Inheritance creates tight coupling between a base class and its subclasses. Changes to the base affect all derived types. Inheritance also tends to model "is-a" relationships that do not hold as systems evolve.

**When it applies:** Whenever you consider creating a subclass of a domain type. Ask whether the relationship is truly "is-a" or whether the new type simply "uses" or "delegates to" the existing one.

**In SQLease:** `Table` holds an `ITableStorage` rather than extending `DataFrameTableStorage`. This means different storage implementations can be substituted without modifying `Table`. The storage contract is expressed as an interface, not a class hierarchy.

**When exceptions are acceptable:** Inheritance is appropriate for closed type hierarchies where every subtype is known and the Liskov Substitution Principle is satisfied. AST node hierarchies are a reasonable example: `Statement` as a base with known subtypes such as `SelectStatement` and `InsertStatement`.

---

## 3. Immutability Where Appropriate

**What it means:** Value objects and schema descriptors should be immutable once created. An object that cannot change cannot be observed in an inconsistent state.

**Why it exists:** Mutable objects shared across components require careful synchronisation and defensive copying. Immutable objects can be passed freely without risk of accidental modification.

**When it applies:** Schema objects (columns, table definitions, data types), AST nodes, query plans, and result rows are candidates for immutability. Objects whose identity is defined by mutation — such as the database state itself — are not.

**In SQLease:** `Column` is currently mutable (both `Name` and `DataType` have setters). This is a known deficiency. A column's definition should not change after the table is created. AST nodes, when introduced, should be immutable records.

**When exceptions are acceptable:** Objects that represent mutable state by design — such as `Database` and `Table` — are appropriately mutable. Immutability should not be forced onto objects whose mutation is the point.

---

## 4. Deterministic Behaviour

**What it means:** Identical inputs must always produce identical outputs. A function that inserts the same row should always produce the same state. A query over the same data should always return the same result.

**Why it exists:** Non-deterministic behaviour makes testing unreliable, debugging unpredictable, and user trust impossible. If the same SQL produces different results on repeated execution without any intervening state change, the system is broken.

**When it applies:** All operations. Parsing, planning, execution, storage. Every operation in the pipeline must be a deterministic function of its inputs and the current database state.

**In SQLease:** The current implementation is largely deterministic. The main risk is the mutable static `_deletedColumn` field in `DataFrameTableStorage`, which could cause cross-instance interference. This must be eliminated.

**When exceptions are acceptable:** Random data generation for testing purposes is intentionally non-deterministic but should be seeded for reproducibility. Future features such as `RANDOM()` or `NOW()` in SQL are non-deterministic by design but must be explicitly marked as such.

---

## 5. Explicit Interfaces

**What it means:** Layer boundaries are defined by interfaces, not by convention or documentation. If two components can interact, the interaction is expressed in a typed contract that the compiler enforces.

**Why it exists:** Implicit contracts — "please only call these methods" — are not enforced. Interfaces make the intended seam explicit and allow alternative implementations to be substituted without risk.

**When it applies:** At every layer boundary. The storage layer exposes `ITableStorage`. When an executor exists, it should receive the domain model through an interface. When the catalog is formalised, it should be expressed as `ICatalog`.

**In SQLease:** `ITableStorage` is the current primary example. `Table` holds `ITableStorage`, not `DataFrameTableStorage`. This means `Table` can be tested with a fake storage implementation and can be backed by a different storage engine transparently.

**When exceptions are acceptable:** Internal implementation details within a single layer do not require interfaces. Creating interfaces purely for testability where a simple class would suffice is over-engineering.

---

## 6. Modularity

**What it means:** The system is composed of modules that can be understood, built, and tested independently. A change in one module requires no changes in unrelated modules.

**Why it exists:** A non-modular system forces understanding the whole to change any part. Development slows and the risk of unintended side effects increases with every change.

**When it applies:** Project structure, namespace organisation, and class design all contribute to modularity. Each project in the solution represents a module boundary.

**In SQLease:** The multi-project solution structure is the primary modularity mechanism. `SQLease.Core` can be built and tested without `SQLease.CLI` or `SQLease.Engine`. Tests reference only `SQLease.Core` because that is where the tested behaviour lives.

**When exceptions are acceptable:** Over-modularising creates unnecessary boilerplate. Not every related class needs its own project. The current project boundaries reflect real responsibility differences, not artificial decomposition.

---

## 7. Low Coupling

**What it means:** A component depends on as few other components as possible, and those dependencies are expressed at the most abstract level available.

**Why it exists:** High coupling means a change in one component propagates through many others. Low coupling limits the blast radius of any individual change.

**When it applies:** When choosing how to express a dependency: prefer an interface over a concrete class, prefer a narrow interface over a wide one, and prefer passing dependencies in rather than constructing them internally.

**In SQLease:** `Table` depends on `ITableStorage`, not on `DataFrameTableStorage`. This is correct low coupling. A future violation to watch for: allowing `SQLease.Engine` to import `Microsoft.Data.Analysis` — that would couple the execution layer to a storage library.

**When exceptions are acceptable:** Within a single tightly cohesive module, coupling to concrete classes is acceptable. `DataFrameTableStorage` can freely use `DataFrame` internally because that coupling is contained within the class.

---

## 8. High Cohesion

**What it means:** A component groups together the things that change together and separates things that change for different reasons.

**Why it exists:** Low cohesion produces god classes that are impossible to test or modify safely. High cohesion makes it clear what a component is for and when it needs to change.

**When it applies:** When reviewing a class or method, ask whether all the logic inside it changes for the same reason. If a change to the parsing rules also requires changes to the storage code, cohesion has been violated somewhere.

**In SQLease:** `DataFrameTableStorage` is reasonably cohesive: all its methods exist because of the storage responsibility. `Database` currently coordinates the tombstone column name during table creation, which is slightly less cohesive — that concern might belong fully in storage. This is worth revisiting as the system grows.

**When exceptions are acceptable:** Perfect cohesion is an ideal. Real systems have co-ordination logic that spans concerns. The goal is to minimise it, not to eliminate all contact between concepts.

---

## 9. Maintainability

**What it means:** Code is written so that a future reader can understand it without access to the author. Variable names are descriptive. Methods are small. Complex logic is explained. Surprising behaviour is documented.

**Why it exists:** Most of the time spent on a codebase is reading existing code, not writing new code. Code that is hard to read is expensive to maintain and prone to errors introduced by misunderstanding.

**When it applies:** Always, but especially for logic that is non-obvious. A straightforward loop does not need a comment. A tombstone-based delete model that must survive compaction does.

**In SQLease:** The tombstone model is a good example. The `__Deleted` column name, the `Compact` method, and the interaction between them form a multi-step invariant. This invariant should be documented at the site of the most important constraint, not scattered across files.

**When exceptions are acceptable:** Extremely simple code should not be over-commented. `return _tables.ContainsKey(tableName)` does not need an explanation.

---

## 10. Extensibility

**What it means:** New requirements can be satisfied by adding new components rather than by modifying existing ones. The system is open for extension and closed for modification where the boundaries are well-defined.

**Why it exists:** Systems that require widespread modification for every new feature become increasingly expensive to extend and increasingly risky to change.

**When it applies:** At layer boundaries and interface definitions. `ITableStorage` allows a new storage backend to be added without touching `Table` or `Database`. Adding a new SQL statement requires adding a new AST node, a new planner case, and a new executor handler — but none of these change the existing statements.

**In SQLease:** The primary extension point today is `ITableStorage`. As the system grows, the AST hierarchy, the planner, and the executor will each become extension points.

**When exceptions are acceptable:** Do not design for extensibility you cannot describe. If you do not know what concrete extension will be needed, do not add abstractions for it. Extensibility is earned by having a real second use case, not by anticipating one.

---

## 11. Simplicity

**What it means:** Prefer the simplest solution that satisfies the requirements. Complexity is a cost. Every abstraction, every indirection, every generalisation must justify itself.

**Why it exists:** Complex code is harder to understand, harder to test, harder to debug, and harder to modify. Complexity introduced today becomes technical debt tomorrow.

**When it applies:** When choosing between two correct options, prefer the simpler one. When introducing an abstraction, ask whether it solves a real problem that exists now, not a hypothetical problem.

**In SQLease:** The tombstone model is simpler than building an in-place deletion mechanism. The current schema representation as `Dictionary<string, Type>` is simpler than a full schema type. Simplicity drove these choices, and that was correct for the current stage.

**When exceptions are acceptable:** When simplicity conflicts with correctness or with a real, demonstrated need for extension, choose the design that is correct and extensible even if it is more complex. Complexity introduced to solve a real problem is justified.

---

## 12. Readability

**What it means:** Code should communicate its intent to the reader clearly and directly. Names should match the domain. Structure should match the mental model.

**Why it exists:** Readable code is self-documenting. When the code names its concepts using the same language as the domain — `InsertRow`, `Compact`, `DeleteRows` — a reader familiar with database concepts immediately understands the intent.

**When it applies:** Naming, method structure, file organisation, and test descriptions. Tests in particular should read like a specification: `DeleteRows_ShouldTombstoneRows` communicates both the action and the expected outcome.

**In SQLease:** The current naming is largely good. `ITableStorage`, `DataFrameTableStorage`, `DeleteRows`, `Compact`, `GetAllRows` are all readable. Areas for improvement: `Class1.cs` in stub projects should be replaced with meaningful names the moment any real code is added.

**When exceptions are acceptable:** Performance-critical inner loops sometimes sacrifice readability for speed. This is acceptable only when the performance need is demonstrated and the critical section is clearly bounded and documented.

---

## 13. Decision Framework

When multiple implementation options exist, evaluate them in this order. An option that fails an earlier step cannot be rescued by performing well on a later one.

| Step                     | Question                                                                         |
| ------------------------ | -------------------------------------------------------------------------------- |
| 1. Correctness           | Does this produce accurate results for all valid inputs and reject invalid ones? |
| 2. Existing architecture | Does this fit within the current layer boundaries and interfaces?                |
| 3. Simplicity            | Is there a simpler approach that satisfies steps 1 and 2?                        |
| 4. Testability           | Can behaviour be verified in isolation without complex test setup?               |
| 5. Maintainability       | Will a future contributor understand this without additional context?            |
| 6. Extensibility         | Will this allow predictable extension without requiring a rewrite?               |
| 7. Performance           | Is this measurably faster, and is that improvement worth the added complexity?   |

If options are equivalent through step 6, choose the higher-performing option only if the complexity cost is zero or near-zero.

Prefer consistency with the existing codebase unless there is a compelling architectural reason not to. Deliberate deviation requires a comment or commit note explaining the rationale.

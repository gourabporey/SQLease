# SQLease

**SQLease** is a lightweight, custom-built database engine implemented in C# and .NET. It aims to support core SQL-like functionality, including DDL and DML operations such as table creation, data insertion, updates, and deletions.

---

## 🚀 Project Goals

- Build a minimalist SQL engine from scratch for educational and experimental purposes.
- Implement core features:
    - In-memory table management
    - Basic DDL (CREATE TABLE)
    - Basic DML (INSERT, UPDATE, DELETE)
- Explore custom storage, indexing, and query parsing.
- Design a modular and testable architecture.

---

## 🧱 Project Structure

**SQLease**

- **SQLease.Core**  
  Core data structures: Table, Row, Column, etc.

- **SQLease.Engine**  
  Logic for executing DDL and DML operations

- **SQLease.Storage** *(Planned)*  
  Persistent storage layer (planned for later implementation)

- **SQLease.CLI**  
  Command-line interface for interacting with the database

- **SQLease.Tests**  
  Unit tests and validation

- **SQLease.sln**  
  The main solution file for the .NET project


---

## 🛠 Getting Started

```bash
git clone https://github.com/gourabporey/SQLease.git
cd SQLease
dotnet build
dotnet run --project SQLease.CLI
```

---

## ✅ Current Capabilities
- Modular solution structure 
- CLI stub in place
- (In Progress) In-memory data model for tables

---

## 📌 TODO (Upcoming Features)
- In-memory data representation 
- Type validation on insert 
- Command parsing (e.g., SQL-like syntax)
- Storage engine (file-based)
- Indexing and query optimization
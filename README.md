# SqlPerformanceLab

[![CI](https://github.com/dennismorina/SqlPerformanceLab/actions/workflows/ci.yml/badge.svg)](https://github.com/dennismorina/SqlPerformanceLab/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2025-CC2927)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED)
![License](https://img.shields.io/badge/License-MIT-green)

A reproducible **SQL Server performance lab** demonstrating common query-performance problems and the impact of targeted optimizations.

SqlPerformanceLab focuses on **query behavior, indexing, SARGability and SQL Server execution characteristics** rather than application CRUD.

## Highlights

- .NET 10 / C#
- SQL Server 2025
- SARGable vs. non-SARGable predicates
- Implicit `varchar` / `nvarchar` conversions
- Functions applied to indexed columns
- Covering indexes
- Deep `OFFSET` pagination vs. keyset pagination
- SARGable join predicates
- SQL Server logical-read measurement with `SET STATISTICS IO`
- Deterministic benchmark data
- Automated benchmark execution
- Markdown result reports
- Docker / Docker Compose
- Unit tests
- GitHub Actions
- Real SQL Server benchmark smoke testing in CI
- Dependabot

## Architecture

```text
sql/
├── setup/
│   ├── 00_create_database.sql
│   ├── 01_schema.sql
│   └── 02_seed.sql
└── scenarios/
    ├── 01_date_sargability.sql
    ├── 02_implicit_conversion.sql
    ├── 03_function_on_column.sql
    ├── 04_covering_index.sql
    ├── 05_pagination.sql
    └── 06_join_predicate.sql

src/
├── SqlPerformanceLab.Core/
└── SqlPerformanceLab.Runner/

tests/
└── SqlPerformanceLab.Tests/

.github/
├── workflows/
│   └── ci.yml
└── dependabot.yml

docker-compose.yml
SqlPerformanceLab.sln
README.md
```

Each benchmark scenario is split into four explicit sections:

```sql
-- @setup
-- @bad
-- @good
-- @teardown
```

The runner executes scenario setup, warms both query variants, measures execution time and logical reads, and removes scenario-specific indexes afterwards.

## Benchmark Dataset

The setup creates a deterministic SQL Server dataset containing:

- 50,000 customers
- 250,000 orders
- four years of distributed order dates
- deterministic customer codes and external references
- multiple order statuses
- realistic lookup and filtering patterns

The dataset is large enough to make query-plan and logical-read differences visible while remaining practical for local development and CI.

## Scenarios

### 1. Date Range SARGability

A function applied to the indexed date column prevents an efficient index seek.

Bad:

```sql
WHERE DATEDIFF(day, OrderDate, @TargetDate) = 0
```

Better:

```sql
WHERE OrderDate >= @TargetDate
  AND OrderDate < DATEADD(day, 1, @TargetDate)
```

The optimized predicate keeps the indexed column unchanged and expresses the condition as a searchable range.

---

### 2. Implicit Conversion

An `nvarchar` parameter is compared with an indexed `varchar` column.

The type mismatch can force SQL Server to convert the indexed column during query execution.

The optimized version uses a parameter type matching the database column:

```text
varchar → varchar
```

instead of:

```text
nvarchar → varchar
```

This allows SQL Server to use the index more efficiently.

---

### 3. Function on Indexed Column

Bad:

```sql
WHERE LOWER(Email) = LOWER(@Email)
```

Better:

```sql
WHERE Email = @Email
```

The dataset is normalized during ingestion, so the query does not need to transform the indexed column at runtime.

Keeping functions away from indexed search columns allows SQL Server to use indexes more effectively.

---

### 4. Covering Index

The optimized query uses an index aligned with both its filter and projection:

```sql
(Status, OrderDate)
INCLUDE (CustomerId, TotalAmount)
```

This can eliminate additional lookups because all required columns are available directly from the index.

The bad variant deliberately uses the clustered primary key to provide a stable and reproducible baseline for the benchmark.

---

### 5. Pagination

Deep offset pagination:

```sql
OFFSET 200000 ROWS
FETCH NEXT 50 ROWS ONLY
```

is compared with keyset pagination:

```sql
WHERE Id > @LastSeenId
ORDER BY Id
```

As page depth increases, `OFFSET` requires SQL Server to process increasingly large numbers of rows before returning the requested page.

Keyset pagination instead continues from the last known key.

---

### 6. Join Predicate

Bad:

```sql
ON UPPER(o.CustomerCode) = c.CustomerCode
```

Better:

```sql
ON o.CustomerCode = c.CustomerCode
```

Applying a function to the join column can prevent efficient index usage.

The optimized query compares the normalized values directly.

## Measurements

The runner measures two signals:

```text
Elapsed Time
Logical Reads
```

Elapsed time is useful but can vary depending on:

- hardware
- CPU load
- background processes
- cache state
- container resources

For that reason, the runner also captures SQL Server logical reads through:

```sql
SET STATISTICS IO ON
```

Logical reads usually provide a more stable indication of how much work SQL Server performs for equivalent queries.

Example output:

```text
[01_date_sargability] Date range SARGability

BAD :    12.41 ms | 1,245 logical reads
GOOD:     1.17 ms |     8 logical reads

Gain: 10.61x faster | 99.4% fewer logical reads
```

Exact benchmark numbers vary by machine.

## Requirements

For local execution:

- .NET 10 SDK
- Docker Desktop

No local SQL Server installation is required.

## Quick Start

Start SQL Server:

```powershell
docker compose up -d sqlserver
```

Create the database, schema and benchmark dataset:

```powershell
dotnet run --project src/SqlPerformanceLab.Runner -- setup
```

List available scenarios:

```powershell
dotnet run --project src/SqlPerformanceLab.Runner -- list
```

Run all benchmarks:

```powershell
dotnet run --project src/SqlPerformanceLab.Runner -- `
  run `
  --scenario all `
  --iterations 3 `
  --output results/latest.md
```

## Local SQL Server Connection

The default development connection is:

```text
Server=localhost,1435
Database=master
User Id=sa
Password=SqlPerfLab_2026!
```

The lab deliberately uses host port:

```text
1435
```

instead of the default SQL Server port `1433` to avoid conflicts with other local environments.

## Fully Containerized Execution

The benchmark runner can also execute completely inside Docker.

Build the runner:

```powershell
docker compose build lab
```

Start SQL Server:

```powershell
docker compose up -d sqlserver
```

Create the benchmark environment:

```powershell
docker compose run --rm --no-deps lab setup
```

Run all scenarios:

```powershell
docker compose run --rm --no-deps lab `
  run `
  --scenario all `
  --iterations 3
```

Clean up:

```powershell
docker compose down -v
```

> `-v` removes the SQL Server volume and therefore the generated benchmark database.

## Testing

Run the automated tests:

```powershell
dotnet test --solution SqlPerformanceLab.sln --configuration Release
```

The tests focus on runner and benchmark infrastructure behavior.

The actual SQL performance scenarios are additionally executed against a real SQL Server instance in GitHub Actions.

## Continuous Integration

Every push and pull request targeting `main` runs two GitHub Actions jobs:

```text
Build & Test
      |
      v
SQL Server Benchmark Smoke Test
```

### Build & Test

The first job performs:

```text
Restore
   ↓
Release Build
   ↓
Unit Tests
```

### SQL Server Benchmark Smoke Test

The second job:

```text
Build Docker benchmark runner
        ↓
Start SQL Server 2025
        ↓
Create database and schema
        ↓
Seed benchmark data
        ↓
Run every SQL scenario
        ↓
Clean up containers
```

This verifies that the benchmark environment works against a real SQL Server instance rather than relying only on mocked or in-memory infrastructure.

## Dependency Management

Dependabot checks project dependencies regularly.

Configured ecosystems include:

- NuGet
- GitHub Actions
- Docker
- Docker Compose

Dependency updates run through the same CI pipeline as normal code changes.

## Technology Stack

| Area | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 10 |
| Database | SQL Server 2025 |
| Database Client | Microsoft.Data.SqlClient |
| Performance Metrics | `SET STATISTICS IO` |
| Benchmarking | Custom .NET runner |
| Testing | xUnit v3 |
| Containers | Docker / Docker Compose |
| CI | GitHub Actions |
| Dependency Updates | Dependabot |

## Design Goals

SqlPerformanceLab demonstrates several SQL performance concepts frequently encountered in real business applications:

- writing SARGable predicates
- avoiding implicit conversions
- avoiding functions on indexed search columns
- designing covering indexes
- choosing scalable pagination strategies
- optimizing join predicates
- measuring logical reads instead of relying only on execution time
- reproducing performance scenarios with deterministic data
- validating database behavior against a real SQL Server instance
- automating database performance scenarios in CI

The project deliberately remains focused on **SQL performance analysis and query optimization** instead of becoming another application or CRUD API.

## Important Note

This repository is a reproducible performance lab.

Benchmark results should not be interpreted as universal performance guarantees.

Real production optimization should always consider:

- actual execution plans
- production data distribution
- statistics
- parameter values
- indexes
- workload concurrency
- hardware and infrastructure
- measured application behavior

Query hints are avoided except where deliberately used to create a stable benchmark baseline.

## License

This project is licensed under the MIT License.

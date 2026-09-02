# SqlPerformanceLab

[![CI](https://github.com/dennismorina/SqlPerformanceLab/actions/workflows/ci.yml/badge.svg)](https://github.com/dennismorina/SqlPerformanceLab/actions/workflows/ci.yml)

A reproducible SQL Server performance lab for demonstrating common query-performance problems and the impact of targeted fixes.

The project is intentionally focused on **query behavior, indexing and SQL Server execution characteristics** rather than application CRUD.

## What it demonstrates

- SARGable vs. non-SARGable predicates
- implicit `varchar` / `nvarchar` conversions
- functions applied to indexed columns
- covering indexes
- deep `OFFSET` pagination vs. keyset pagination
- SARGable join predicates
- SQL Server logical-read measurement with `SET STATISTICS IO`
- deterministic benchmark data
- automated benchmark execution
- Markdown result reports
- SQL Server 2022 in Docker
- GitHub Actions smoke testing

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
```

Each scenario contains four explicit sections:

```sql
-- @setup
-- @bad
-- @good
-- @teardown
```

The runner executes the setup, warms both query variants, measures elapsed time and logical reads, and then removes scenario-specific indexes.

## Requirements

- .NET 10 SDK
- Docker Desktop

No local SQL Server installation is required.

## Quick start

Start SQL Server:

```powershell
docker compose up -d sqlserver
```

Create the database, schema and benchmark data:

```powershell
dotnet run --project src/SqlPerformanceLab.Runner -- setup
```

List scenarios:

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

The default local connection is:

```text
Server=localhost,1435
Database=master
User Id=sa
Password=SqlPerfLab_2026!
```

The lab uses host port **1435** so it does not occupy the default SQL Server port `1433`.

## Fully containerized

```powershell
docker compose build lab
docker compose up -d sqlserver
docker compose run --rm --no-deps lab setup
docker compose run --rm --no-deps lab run --scenario all --iterations 3
```

Clean up:

```powershell
docker compose down -v
```

## Benchmark dataset

The setup creates:

- 50,000 customers
- 250,000 orders
- four years of distributed order dates
- deterministic customer codes and external references
- multiple order statuses and realistic lookup patterns

The dataset is intentionally large enough to make plan differences visible while still being practical for local development and CI.

## Scenarios

### 1. Date range SARGability

Bad:

```sql
WHERE DATEDIFF(day, OrderDate, @TargetDate) = 0
```

Better:

```sql
WHERE OrderDate >= @TargetDate
  AND OrderDate < DATEADD(day, 1, @TargetDate)
```

### 2. Implicit conversion

An `nvarchar` parameter is compared with an indexed `varchar` column.

The optimized form uses the matching `varchar` type so SQL Server does not need to convert the indexed column.

### 3. Function on indexed column

Bad:

```sql
WHERE LOWER(Email) = LOWER(@Email)
```

Better:

```sql
WHERE Email = @Email
```

The dataset is normalized during ingestion, so the query does not need to transform the indexed column.

### 4. Covering index

The optimized query can use an index aligned to its filter and projection:

```sql
(Status, OrderDate)
INCLUDE (CustomerId, TotalAmount)
```

The bad variant intentionally forces the clustered primary key to produce a stable baseline for this lab scenario.

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

### 6. Join predicate

Bad:

```sql
ON UPPER(o.CustomerCode) = c.CustomerCode
```

Better:

```sql
ON o.CustomerCode = c.CustomerCode
```

## Interpreting results

Elapsed time can vary depending on hardware, background load and cache state.

For that reason the runner also captures **logical reads** from SQL Server. Logical reads are usually the more useful signal when comparing two logically equivalent query shapes.

Example:

```text
[01_date_sargability] Date range SARGability
  BAD :    12.41 ms |      1,245 logical reads
  GOOD:     1.17 ms |          8 logical reads
  Gain: 10.61x faster | 99.4% fewer logical reads
```

Exact numbers vary by machine.

## CI

GitHub Actions runs two jobs:

- **Build & Test**
- **SQL Server Benchmark Smoke Test**

The SQL Server job starts SQL Server 2022, creates the benchmark database, seeds the test data and executes every scenario once.

## Notes

This is an educational performance lab. Query hints are avoided except in the covering-index scenario, where a clustered-index hint is used deliberately to create a stable and reproducible baseline.

Production optimization should always be based on the real workload, actual execution plans, statistics, data distribution and measured system behavior.

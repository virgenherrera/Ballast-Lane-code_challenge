# Handoff: EP01-B1-03a — Infrastructure: EF Core Persistence + Migration

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-03a                               |
| Task Name     | Infrastructure: EF Core Persistence + Migration |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.1, AC-004.7, AC-004.10)   |
| Persona       | Kelsey Hightower — Infrastructure / Persistence |
| Model Tier    | sonnet                                    |

**Split rationale**: Originally bundled with SeedCurrentUserContext. TL, QA, QA-Auto, and Infra roles independently flagged two issues: (1) no test gate before the API layer consumes Infrastructure, (2) an identity shim is an unrelated failure domain from EF Core schema work. Split into 03a (persistence) and 03b (identity shim).

## 2. Objective

Implement `TaskItemConfiguration` (EF Core fluent config with `ValueGeneratedNever()` on Id), `TaskRepository`, `AppDbContext`, and the first-ever migration for the Tasks table against real PostgreSQL. Two irreversible schema decisions are frozen here: snake_case column naming via explicit `HasColumnName()` calls, and `TaskStatus` stored as `smallint` via `HasConversion<int>()`. A dedicated repository-level integration test gate proves the schema works before EP01-B1-04b consumes it over HTTP.

## 3. Pre-Conditions

- [ ] EP01-B1-01 and EP01-B1-02 report STATUS: DONE and all their Quality Gates pass
- [ ] `dotnet build src/TaskFlow.Application/` exits 0
- [ ] `ITaskRepository` interface exists at `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs`
- [ ] Docker is running: `docker ps` succeeds
- [ ] Microsoft.EntityFrameworkCore 10.0.4, Microsoft.EntityFrameworkCore.Design 10.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 resolvable on NuGet
- [ ] Testcontainers.PostgreSql (pin exact 4.x version compatible with .NET 10 at implementation time) resolvable on NuGet
- [ ] **DECISION (frozen)**: Column naming = snake_case via explicit `HasColumnName()` per property (no EFCore.NamingConventions package — keeps dependency count minimal)
- [ ] **DECISION (frozen)**: TaskStatus stored as `smallint` via `HasConversion<int>()` (smaller footprint, faster indexing; no external raw-SQL consumers in Delivery 1)

If any pre-condition fails (including Docker unavailability), report BLOCKED.

## 4. Context Bundle

| File                                                       | Lines     | Why                                          |
| ---------------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`                  | 13-27     | DOR — ValueGeneratedNever, column naming, enum storage risks |
| `docs/user-stories/US-004-create-task.md`                  | 173-185   | Risks section — MEDIUM risks on schema decisions |
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | all  | Contract this task implements                 |
| `src/TaskFlow.Domain/Entities/TaskItem.cs`                 | all       | Entity being persisted                        |
| `src/TaskFlow.Domain/Constants/FieldLengths.cs`            | all       | HasMaxLength values                           |
| `docs/architecture/clean-architecture.md`                  | 145-164   | Infrastructure references Application only    |

## 5. Deliverables

### Files to Create

| File Path                                                                        | Contents |
| -------------------------------------------------------------------------------- | -------- |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs`                        | DbContext with `DbSet<TaskItem> Tasks` |
| `src/TaskFlow.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs`| IEntityTypeConfiguration with ValueGeneratedNever, HasMaxLength, HasConversion |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs`         | Implements `ITaskRepository.AddAsync` |
| `src/TaskFlow.Infrastructure/Persistence/Migrations/{timestamp}_AddTasksTable.cs`| Generated via `dotnet ef migrations add` |
| `tests/TaskFlow.IntegrationTests/Persistence/TaskRepositoryTests.cs`             | 2 repository-level integration tests |

### Files to Modify

| File Path                                              | Change                                    |
| ------------------------------------------------------ | ----------------------------------------- |
| `src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj` | Add EF Core + Npgsql PackageReferences |
| `tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj` | Add Testcontainers.PostgreSql PackageReference |

### Expected Signatures

```csharp
// TaskItemConfiguration.cs
public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        // DECISION: column naming = snake_case via explicit HasColumnName()
        // DECISION: TaskStatus stored as smallint via HasConversion<int>()
        builder.ToTable("tasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(FieldLengths.TitleMaxLength).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(FieldLengths.DescriptionMaxLength);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(x => x.DueDate).HasColumnName("due_date");
        builder.Property(x => x.OwnerId).HasColumnName("owner_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
```

**Required Test Names** (repository-level, Testcontainers.PostgreSql directly, bypassing HTTP):

1. `AddAsync_PersistsTask_PreservesClientGeneratedId` — construct TaskItem via `Create()`, capture its Id, persist, re-query, assert same Id survives SaveChangesAsync (proves ValueGeneratedNever works)
2. `AddAsync_PersistsAndRetrieves_StatusEnumRoundTripsCorrectly` — persist task with Status == Pending, re-query, assert Status deserializes back to Pending

**Migration command** (document verbatim):
```
dotnet ef migrations add AddTasksTable --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API --output-dir Persistence/Migrations
```

## 6. Quality Gates

| #  | Gate                          | Command                                                                                             | Pass Criteria         |
| -- | ----------------------------- | --------------------------------------------------------------------------------------------------- | --------------------- |
| G1 | Compilation                   | `dotnet build src/TaskFlow.Infrastructure/`                                                         | exit 0                |
| G2 | Repository integration tests  | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~TaskRepositoryTests"`    | exit 0, 2 passed      |
| G3 | Migration applies             | `dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API`| exit 0 (against local/Testcontainers PostgreSQL) |
| G4 | No forbidden providers        | Search for "InMemory" or "Sqlite" in src/ and tests/ — must find zero matches                       | no matches            |
| G5 | Decisions documented          | TaskItemConfiguration.cs contains at least 2 lines with "DECISION:" comments                        | verified              |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Implement `SeedCurrentUserContext` or any `ICurrentUserContext` — that is EP01-B1-03b
- Build the WebApplicationFactory/Testcontainers shared harness for other stories — a local, task-specific Testcontainers usage is sufficient here; EP01-B1-04a builds the shared harness
- Change `ValueGeneratedNever()` to `ValueGeneratedOnAdd()` — this is the exact MEDIUM risk the story warns against
- Use `postgres:latest` — pin `postgres:17-alpine` in the Testcontainers fixture
- Add EFCore.NamingConventions package — explicit `HasColumnName()` is the chosen approach per frozen decision
- Wire DI registration in Program.cs — that belongs to EP01-B1-04a

### SCOPE BOUNDARY — Stop when:

- All 5 deliverable files created, migration applies, both repository tests pass
- All quality gates pass
- Do NOT proceed to SeedCurrentUserContext (EP01-B1-03b) or API work

## 8. Anti-Patterns

| Anti-Pattern                                         | Why It Fails                                              | Do Instead                                   |
| ---------------------------------------------------- | --------------------------------------------------------- | -------------------------------------------- |
| Letting EF Guid-PK convention apply ValueGeneratedOnAdd | EF silently regenerates the Domain-constructed UUID v7    | Explicit `.ValueGeneratedNever()` + test     |
| Generating migration before documenting decisions    | Creates an irreversible schema commit                      | Write DECISION comments first, then generate |
| Testing persistence only via future API integration tests | No isolated failure signal for schema bugs            | Write `TaskRepositoryTests.cs` here directly |
| Using `postgres:latest` in Testcontainers            | Non-reproducible CI runs                                  | Pin `postgres:17-alpine`                          |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G5)
2. If G1 fails: check EF Core/Npgsql package version compatibility (EF Core 10.0.4 + Npgsql 10.0.3)
3. If G2 (`PreservesClientGeneratedId`) fails: check `ValueGeneratedNever()` is set — most likely root cause
4. If G3 (migration) fails: check connection string and Docker container; do NOT delete and regenerate blindly — read the EF CLI error first
5. If G4 fails: remove any InMemory/SQLite reference — PostgreSQL only
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED. If G3 is the persistent failure and Docker itself won't start, report BLOCKED (infra-availability), not FAILED (code bug).

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests: Domain invariants + Application use cases (mocked repos)
- Integration tests: API level (AAA pattern), PRIMARY confidence layer
- E2E: Playwright, final quality gate
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not jump ahead
- Every decision must trace back to a requirement or AC
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B1-03a
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {MUST include: exact Testcontainers.PostgreSql version pinned, confirmation of column-naming and enum-storage decisions applied}
```

# Handoff: EP02-B2-01 — EF Core User Configuration + Migration

> [📚 INDEX](../../INDEX.md) / [EP02 — User Management](../../epics/EP02-user-management.md) / EP02-B2-01

## 1. Metadata

| Field         | Value                                        |
| ------------- | --------------------------------------------- |
| Task ID       | EP02-B2-01                                     |
| Task Name     | EF Core User Configuration + Migration        |
| Batch         | 2 of 6 (EP02 Batches 1-6 Plan)                |
| Epic          | EP02 — User Management                        |
| User Stories  | US-001 (AC-001.1, AC-001.2, AC-001.6)         |
| Persona       | Uncle Bob — Infrastructure / Persistence      |
| Model Tier    | sonnet                                         |

## 2. Objective

Add `DbSet<User>` to `AppDbContext`, implement `UserConfiguration` (EF Core fluent config
mapping the `Email` and `PasswordHash` value objects to plain string columns via
`HasConversion`), and generate the `AddUsersTable` migration with a case-insensitive
unique index on `email` using PostgreSQL `LOWER()`. This is the schema foundation
`EP02-B2-02` (UserRepository) builds on — no repository code in this task.

## 3. Pre-Conditions

- [ ] EP02-B1-01 (`User` entity, `Email` VO, `PasswordHash` VO) reports DONE
- [ ] `src/TaskFlow.Domain/Entities/User.cs` exists with properties: `Id` (Guid), `Email`
      (Email VO), `Name` (string), `PasswordHash` (PasswordHash VO), `CreatedAt` (DateTime)
- [ ] `src/TaskFlow.Domain/ValueObjects/Email.cs` and
      `src/TaskFlow.Domain/ValueObjects/PasswordHash.cs` exist, each exposing a single
      string-typed `Value` accessor
- [ ] `dotnet build src/TaskFlow.Domain/` exits 0
- [ ] `dotnet build src/TaskFlow.Infrastructure/` exits 0 (Tasks table already migrated —
      EP01-B1-03a)
- [ ] Docker is running: `docker ps` succeeds
- [ ] No file named `UserConfiguration.cs` exists under
      `src/TaskFlow.Infrastructure/Persistence/Configurations/`
- [ ] No migration file matching `*_AddUsersTable.cs` exists under
      `src/TaskFlow.Infrastructure/Persistence/Migrations/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                                             | Lines | Why                                             |
| --------------------------------------------------------------------------------- | ----- | ------------------------------------------------ |
| `docs/user-stories/US-001-user-registration.md`                                  | 85-98 | Expected Deliverables (Domain layer entity/VO shape) |
| `docs/epics/EP02-engineering-addenda.md`                                         | 49-67 | Decisions #4 (email casing/uniqueness) and #5 (UUID v7 strategy) |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs`                       | all   | Existing DbContext — add `DbSet<User>` here      |
| `src/TaskFlow.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs`| all   | Pattern to follow: `IEntityTypeConfiguration<T>`, `HasColumnName`, `ValueGeneratedNever` |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContextFactory.cs`                | all   | Design-time factory used by `dotnet ef` commands |
| `src/TaskFlow.Domain/Entities/User.cs`                                          | all   | Entity being persisted (created in EP02-B1-01)   |
| `src/TaskFlow.Domain/ValueObjects/Email.cs`                                     | all   | VO to convert — read its `Value` accessor shape  |
| `src/TaskFlow.Domain/ValueObjects/PasswordHash.cs`                              | all   | VO to convert — read its `Value` accessor shape  |

## 5. Deliverables

### Files to Create

| File Path                                                                          | Contents                                            |
| ------------------------------------------------------------------------------------ | ---------------------------------------------------- |
| `src/TaskFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs`       | `IEntityTypeConfiguration<User>` with VO conversions and unique index |
| `src/TaskFlow.Infrastructure/Persistence/Migrations/{timestamp}_AddUsersTable.cs`  | Generated via `dotnet ef migrations add`             |

### Files to Modify

| File Path                                                     | Change                                    |
| ---------------------------------------------------------------- | -------------------------------------------- |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs`       | Add `public DbSet<User> Users => Set<User>();` |

### Expected Signatures

```csharp
// UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // DECISION: column naming = snake_case via explicit HasColumnName()
        // DECISION: Email/PasswordHash VOs stored as plain string via HasConversion
        // DECISION: Id generation = none — client-generated UUID v7 (ValueGeneratedNever)
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasConversion(
                hash => hash.Value,
                value => PasswordHash.Create(value))
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Case-insensitive uniqueness per Decision #4 — PostgreSQL LOWER()
        // functional index. Cannot use a plain HasIndex on Email here because
        // the application already rejects uppercase at the VO boundary; this
        // index is the final integrity backstop against race conditions
        // (TOCTOU gap acknowledged in US-001 DOR).
        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_users_email_lower")
            .HasMethod("btree")
            .HasOperators(default) // remove if HasOperators is unavailable; use raw SQL index instead (see Anti-Patterns)
            .IsUnique();
    }
}
```

**IMPORTANT**: EF Core's fluent API does not have first-class support for a `LOWER(email)`
functional unique index. Use a migration-level raw SQL index instead — see the Anti-Patterns
section for the exact approach. Do NOT rely on `HasOperators`/`HasMethod` above; that
snippet only illustrates intent, not the final implementation. The migration file itself
must contain a `migrationBuilder.Sql(...)` call creating:

```sql
CREATE UNIQUE INDEX "IX_users_email_lower" ON "users" (LOWER("email"));
```

Because the entity property itself already stores the value with `HasConversion` (plain
string column, name `email`), the `UserConfiguration.Configure` method should NOT call
`.HasIndex(x => x.Email).IsUnique()` (that produces a plain B-tree index on the raw column,
not a case-insensitive one). Instead: omit the C#-level unique index declaration entirely
and add the raw SQL index directly inside the generated migration's `Up()` method (and drop
it in `Down()`).

**Migration command** (document verbatim):

```bash
dotnet ef migrations add AddUsersTable --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API --output-dir Persistence/Migrations
```

After generation, manually edit the migration's `Up()` method to append:

```csharp
migrationBuilder.Sql(
    "CREATE UNIQUE INDEX \"IX_users_email_lower\" ON \"users\" (LOWER(\"email\"));");
```

And the `Down()` method to prepend:

```csharp
migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_users_email_lower\";");
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
| -- | ----------------------- | --------------------------------------------------------------------------------------------------- | ---------------------- |
| G1 | Compilation | `dotnet build src/TaskFlow.Infrastructure/` | exit 0 |
| G2 | Migration applies | `dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API` | exit 0 against real PostgreSQL |
| G3 | Unique index present | `psql` or integration test confirms `IX_users_email_lower` exists and rejects a second row differing only by case | insert `a@b.com` then `A@B.com` → second insert fails with unique violation |
| G4 | No forbidden providers | Search for `InMemory` or `Sqlite` in `src/TaskFlow.Infrastructure/` — must find zero matches | no matches |
| G5 | Decisions documented | `UserConfiguration.cs` contains at least 2 lines with `DECISION:` comments | verified |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Implement `UserRepository` — that is `EP02-B2-02`
- Implement `BcryptPasswordHasher` or `JwtTokenService` — those are `EP02-B2-03`/`EP02-B2-04`
- Wire DI registration (`AddScoped<IUserRepository, UserRepository>`) in `Program.cs` — that
  belongs to a later API batch
- Add a `.HasIndex(...).IsUnique()` on the plain `email` column expecting case-insensitivity
  — plain B-tree indexes are case-sensitive in PostgreSQL; the raw SQL `LOWER()` index is
  mandatory
- Add EFCore.NamingConventions package — explicit `HasColumnName()` is the established
  convention from EP01
- Change `ValueGeneratedNever()` to `ValueGeneratedOnAdd()` — UUID v7 is client-generated
  per Decision #5, same as `TaskItem.Id`

### SCOPE BOUNDARY — Stop when

- `AppDbContext` has `DbSet<User>`, `UserConfiguration.cs` exists, and the `AddUsersTable`
  migration applies cleanly with the case-insensitive unique index present
- All quality gates in Section 6 pass
- Do NOT proceed to `EP02-B2-02` (UserRepository)

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
| ---------------------------------------------------- | ----------------------------------------------------------------- | ---------------------------------------------- |
| `builder.HasIndex(x => x.Email).IsUnique()` alone | Produces a case-SENSITIVE unique index; `Foo@x.com` and `foo@x.com` both insert successfully | Add raw SQL `CREATE UNIQUE INDEX ... (LOWER(email))` inside the migration `Up()` |
| Letting EF's Guid-PK convention apply `ValueGeneratedOnAdd` | EF silently regenerates the Domain-constructed UUID v7 | Explicit `.ValueGeneratedNever()` |
| Storing `Email`/`PasswordHash` as owned entity types (`OwnsOne`) | Adds an unnecessary extra table/complex-type mapping for a single-value VO | Use `HasConversion` to a plain string column |
| Generating the migration before editing `Configure()` | Migration snapshot won't reflect the final column/index shape | Write `UserConfiguration.cs` completely first, then generate |
| Using `postgres:latest` anywhere in local testing | Non-reproducible runs | Pin `postgres:17.5` (already the pinned image in `docker-compose.yml`) |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G5)
2. If G1 fails: check that `Email.Create`/`PasswordHash.Create` (or equivalent factory)
   signatures from EP02-B1-01 match what `HasConversion` expects — a private constructor
   without a public factory will not compile inside the conversion lambda
3. If G2 (migration apply) fails: check the connection string env vars
   (`DB_HOST`/`DB_PORT`/`DB_USER`/`DB_PASSWORD`/`DB_NAME` — NOT a single `DATABASE_URL`,
   see Compact Rules) and that Docker's `postgres:17.5` container is reachable
4. If G3 (unique index) fails: confirm the raw `migrationBuilder.Sql(...)` line was added
   to `Up()` — the C#-level `HasIndex` alone will NOT produce case-insensitive behavior
5. If G4 fails: remove any InMemory/SQLite reference
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP
   and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in
   each attempt

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
- Docker Compose: `postgres:17.5`, `taskflow-api`, `taskflow-web`
- Env vars are discrete (`DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME`), never a
  single `DATABASE_URL` connection string — this project's `EnvVarValidator` and
  `Program.cs` build the Npgsql connection string from these five variables

## 11. Status Protocol

Include this block EXACTLY in your final response. No variations.

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B2-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {MUST include: confirmation the raw SQL LOWER() unique index was added to the migration, and that ValueGeneratedNever is set on Id}
```

# Handoff: EP02-B2-02 — UserRepository Implementation

> [📚 INDEX](../../INDEX.md) / [EP02 — User Management](../../epics/EP02-user-management.md) / EP02-B2-02

## 1. Metadata

| Field         | Value                                        |
| ------------- | --------------------------------------------- |
| Task ID       | EP02-B2-02                                     |
| Task Name     | UserRepository Implementation                 |
| Batch         | 2 of 6 (EP02 Batches 1-6 Plan)                |
| Epic          | EP02 — User Management                        |
| User Stories  | US-001 (AC-001.1, AC-001.2), US-002 (AC-002.1, AC-002.2) |
| Persona       | Uncle Bob — Infrastructure / Repository Layer |
| Model Tier    | sonnet                                         |

## 2. Objective

Implement `UserRepository`, the concrete `IUserRepository` adapter over `AppDbContext`,
using LINQ-only queries against `DbSet<User>`. Prove it against real PostgreSQL with a
Testcontainers-backed integration test suite, following the exact pattern already
established by `TaskRepositoryTests`. This closes the persistence gap that
`RegisterUserHandler` and `AuthenticateUserHandler` (EP02-B1-03/04) depend on.

## 3. Pre-Conditions

- [ ] EP02-B2-01 reports DONE — `DbSet<User>`, `UserConfiguration`, and the
      `AddUsersTable` migration (with case-insensitive unique index) exist and apply
      cleanly
- [ ] EP02-B1-02 reports DONE — `IUserRepository` interface exists at
      `src/TaskFlow.Domain/Interfaces/IUserRepository.cs` (or equivalent Application-layer
      path — confirm exact path via context bundle) with signatures: `GetByEmailAsync`,
      `GetByIdAsync`, `AddAsync`, `ExistsAsync`, all accepting `CancellationToken`
- [ ] `dotnet build src/TaskFlow.Infrastructure/` exits 0
- [ ] Docker is running: `docker ps` succeeds
- [ ] No file named `UserRepository.cs` exists under
      `src/TaskFlow.Infrastructure/Persistence/Repositories/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                                       | Lines   | Why                                                    |
| ------------------------------------------------------------------------- | ------- | -------------------------------------------------------- |
| `docs/user-stories/US-001-user-registration.md`                          | 85-98   | `IUserRepository` method shapes expected                |
| `docs/user-stories/US-002-user-login.md`                                 | 60-76   | `GetByEmailAsync` consumption contract from login flow  |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | all     | Pattern to follow: constructor injection of `AppDbContext`, LINQ only, `CancellationToken` on every method |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs`                | all     | `DbSet<User> Users` added in EP02-B2-01                 |
| `tests/TaskFlow.IntegrationTests/Persistence/TaskRepositoryTests.cs`     | all     | Pattern to follow: `IAsyncLifetime`, Testcontainers.PostgreSql, direct `CreateDbContext()` helper, no HTTP |
| `tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj`       | all     | Confirm `Testcontainers.PostgreSql` package already referenced (no new package needed) |

## 5. Deliverables

### Files to Create

| File Path                                                                     | Contents                                             |
| -------------------------------------------------------------------------------- | ------------------------------------------------------- |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/UserRepository.cs`        | Implements `IUserRepository` — 4 methods, LINQ only    |
| `tests/TaskFlow.IntegrationTests/Persistence/UserRepositoryTests.cs`           | Testcontainers-backed repository-level integration tests |

### Files to Modify

None — this task only adds new files. DI registration in `Program.cs` is out of scope
(belongs to a later API batch).

### Expected Signatures

```csharp
// UserRepository.cs
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces; // adjust to actual IUserRepository namespace from EP02-B1-02
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _dbContext.Users.AddAsync(user, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Email email, CancellationToken ct)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Email == email, ct);
    }
}
```

**NOTE**: `u.Email == email` relies on EF Core translating the VO comparison through the
`HasConversion` mapping set up in `EP02-B2-01`. If `Email` does not implement value
equality that EF can translate (or if the exact `IUserRepository` signature from
EP02-B1-02 takes a raw `string` instead of the `Email` VO), adapt the parameter type to
match the frozen interface exactly — the interface signature is the source of truth, not
this snippet. Read `IUserRepository.cs` before writing this file.

**Required Test Names** (repository-level, Testcontainers.PostgreSql directly, bypassing
HTTP — mirror `TaskRepositoryTests` structure exactly):

1. `AddAsync_PersistsUser_PreservesClientGeneratedId` — construct `User` via its
   Domain factory, capture `Id`, persist, re-query, assert same `Id` survives
   `SaveChangesAsync` (proves `ValueGeneratedNever` works)
2. `GetByEmailAsync_ExistingEmail_ReturnsUser` — persist a user, query by the same email,
   assert non-null result with matching `Id`
3. `GetByEmailAsync_NonExistentEmail_ReturnsNull` — query an email never persisted,
   assert null
4. `GetByIdAsync_ExistingId_ReturnsUser` — persist, query by `Id`, assert match
5. `GetByIdAsync_NonExistentId_ReturnsNull` — query a random `Guid`, assert null
6. `ExistsAsync_EmailAlreadyRegistered_ReturnsTrue`
7. `ExistsAsync_EmailNotRegistered_ReturnsFalse`
8. `AddAsync_DuplicateEmailDifferentCasing_ThrowsOnUniqueConstraintViolation` — proves the
   `EP02-B2-01` case-insensitive index works end-to-end from the repository layer; persist
   `alice@example.com`, then attempt to persist a second user with a `PasswordHash`/`Name`
   that differs but the SAME email value normalized (this test only makes sense if the
   `Email` VO itself already rejects uppercase per Decision #4 — if so, adapt this test to
   instead prove the DB index rejects a raw-SQL-inserted duplicate, or drop it and note in
   `NOTES` why it was not applicable at the repository layer, since the VO boundary makes
   uppercase input impossible to construct)

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
| -- | ------------------- | ----------------------------------------------------------------------------------------------- | ---------------------- |
| G1 | Compilation | `dotnet build src/TaskFlow.Infrastructure/` | exit 0 |
| G2 | Repository integration tests | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~UserRepositoryTests"` | exit 0, all named tests passed |
| G3 | Existing tests still pass | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~TaskRepositoryTests"` | exit 0, 0 regressions |
| G4 | No forbidden providers | Search for `InMemory` or `Sqlite` in `src/TaskFlow.Infrastructure/` and this task's test file — must find zero matches | no matches |
| G5 | LINQ only | `UserRepository.cs` contains zero occurrences of `FromSqlRaw`, `ExecuteSqlRaw`, `NpgsqlCommand` | verified |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Implement `BcryptPasswordHasher` or `JwtTokenService` — those are `EP02-B2-03`/`EP02-B2-04`
- Wire DI registration in `Program.cs` — belongs to a later API batch
- Build a new shared Testcontainers harness — reuse the exact local, task-specific pattern
  already used by `TaskRepositoryTests` (a shared `WebApplicationFactory`-based harness is
  `TaskFlowWebApplicationFactory`, used by HTTP-level tests, not repository-level tests)
- Add raw SQL anywhere in `UserRepository.cs` — LINQ only per Decision #11
- Change the frozen `IUserRepository` interface signature — if the signature does not
  match this handoff's Expected Signatures, follow the interface, not this file

### SCOPE BOUNDARY — Stop when

- `UserRepository.cs` implements all 4 `IUserRepository` methods and the named integration
  tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to `EP02-B2-03` (BCrypt) or `EP02-B2-04` (JWT)

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
| -------------------------------------------------------- | -------------------------------------------------------------- | ---------------------------------------------- |
| Comparing `Email` via `.Value == emailString` manually in C# before querying | Bypasses EF Core translation, forces full table load into memory | Let EF Core translate the VO equality via the `HasConversion` mapping from `EP02-B2-01` |
| Using `AsNoTracking()` on `AddAsync`/mutation paths | Breaks change tracking needed for `SaveChangesAsync` | Reserve `AsNoTracking()` for read-only queries only (see `TaskRepository.ListAsync` for the pattern) |
| Writing a brand-new WebApplicationFactory subclass for this test | Duplicates `TaskFlowWebApplicationFactory`/`TaskRepositoryTests` patterns unnecessarily | Follow `TaskRepositoryTests`'s local, lightweight `IAsyncLifetime` + `CreateDbContext()` pattern |
| Using `postgres:latest` in the Testcontainers fixture | Non-reproducible CI runs | Pin `postgres:17.5` |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G5)
2. If G1 fails: confirm the `IUserRepository` interface's exact method signatures from
   `EP02-B1-02` — do not guess parameter types
3. If G2 fails: check whether the failure is an EF Core translation error on the `Email`
   VO comparison (`u.Email == email`) — if EF cannot translate the VO equality, add an
   explicit `.HasConversion` read on both sides or compare via `.Value` inside the LINQ
   expression (EF can translate member access on converted properties)
4. If G3 fails (regression on existing `TaskRepositoryTests`): revert this task's
   `AppDbContext`/migration touches — this task should not modify `AppDbContext.cs` beyond
   what `EP02-B2-01` already added
5. If G4/G5 fail: remove any raw SQL or InMemory/SQLite reference
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
- Query mechanism: LINQ only via `IQueryable<T>` — raw SQL (`FromSqlRaw`, `ExecuteSqlRaw`,
  `NpgsqlCommand`) is forbidden (Decision #11)

## 11. Status Protocol

Include this block EXACTLY in your final response. No variations.

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B2-02
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {MUST include: confirmation the IUserRepository interface signature was read and matched exactly, and whether the duplicate-email-casing test was applicable or replaced}
```

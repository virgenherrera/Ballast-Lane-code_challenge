> [📚 INDEX](../../INDEX.md) / [EP02](../../epics/EP02-user-management.md) / EP02-B1-02

# Handoff: EP02-B1-02 — Domain: Interfaces + Exceptions

## 1. Metadata

| Field         | Value                                                      |
| ------------- | -------------------------------------------------------------- |
| Task ID       | EP02-B1-02                                                       |
| Task Name     | Domain: Interfaces + Exceptions                                  |
| Batch         | 1 of 6 (EP02)                                                    |
| Epic          | EP02 — User Management                                          |
| User Stories  | US-001 (AC-001.2), US-002 (AC-002.2, AC-002.4)                   |
| Persona       | Uncle Bob — Clean Architecture, DDD                              |
| Model Tier    | sonnet                                                           |

## 2. Objective

Define the three Domain-owned interfaces that Application use cases depend on
(`IUserRepository`, `IPasswordHasher`, `ITokenService`) and two Domain exceptions
(`DuplicateEmailException`, `InvalidCredentialsException`). These are contracts only —
no concrete implementation. Infrastructure (Batch 2) implements them; Application
(EP02-B1-03, EP02-B1-04) consumes them.

## 3. Pre-Conditions

- [ ] `dotnet build src/TaskFlow.Domain/` exits 0
- [ ] `dotnet build tests/TaskFlow.Domain.Tests/` exits 0
- [ ] `src/TaskFlow.Domain/Exceptions/DomainException.cs` exists (concrete class, NOT
  abstract, constructor `DomainException(string message)`)
- [ ] No file named `IUserRepository.cs`, `IPasswordHasher.cs`, `ITokenService.cs`,
  `DuplicateEmailException.cs`, or `InvalidCredentialsException.cs` exists under
  `src/TaskFlow.Domain/`
- [ ] `src/TaskFlow.Domain/Entities/User.cs` exists (produced by EP02-B1-01 — this task can
  run in parallel with EP02-B1-01, but the `User` type must be present before this task's
  own build/test gates are run; if EP02-B1-01 has not yet merged, coordinate with the
  orchestrator before starting `IUserRepository`, which references `User`)

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                      | Lines   | Why                                                |
| ----------------------------------------------------------- | ------- | ----------------------------------------------------- |
| `docs/user-stories/US-001-user-registration.md`               | 79-95   | Expected Deliverables — `IUserRepository`, `DuplicateEmailException` shape |
| `docs/user-stories/US-002-user-login.md`                      | 63-77   | Expected Deliverables — `InvalidCredentialsException` shape         |
| `docs/user-stories/US-002-user-login.md`                      | 1-20    | DOR — `ITokenService`/`IPasswordHasher` signature freeze, generic message rule |
| `src/TaskFlow.Domain/Exceptions/DomainException.cs`           | all     | Existing base exception — concrete class, ctor(string)              |

## 5. Deliverables

### Files to Create

| File Path                                                       | Contents                                                            |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------ |
| `src/TaskFlow.Domain/Interfaces/IUserRepository.cs`                 | `GetByEmailAsync`, `GetByIdAsync`, `AddAsync`, `ExistsAsync` — all with `CancellationToken` |
| `src/TaskFlow.Domain/Interfaces/IPasswordHasher.cs`                 | `Hash(string) -> PasswordHash`, `Verify(string, PasswordHash) -> bool`     |
| `src/TaskFlow.Domain/Interfaces/ITokenService.cs`                   | `GenerateToken(User) -> string`                                            |
| `src/TaskFlow.Domain/Exceptions/DuplicateEmailException.cs`         | Inherits `DomainException`, no HTTP status codes                          |
| `src/TaskFlow.Domain/Exceptions/InvalidCredentialsException.cs`     | Inherits `DomainException`, generic message constant, no HTTP status codes |
| `tests/TaskFlow.Domain.Tests/Exceptions/DuplicateEmailExceptionTests.cs` | Unit tests for AC-001.2                                              |
| `tests/TaskFlow.Domain.Tests/Exceptions/InvalidCredentialsExceptionTests.cs` | Unit tests for AC-002.2                                          |

### Expected Signatures

```csharp
// Interfaces/IUserRepository.cs
namespace TaskFlow.Domain.Interfaces;

using TaskFlow.Domain.Entities;
using TaskFlow.Domain.ValueObjects;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken);
}

// Interfaces/IPasswordHasher.cs
namespace TaskFlow.Domain.Interfaces;

using TaskFlow.Domain.ValueObjects;

public interface IPasswordHasher
{
    PasswordHash Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, PasswordHash passwordHash);
}

// Interfaces/ITokenService.cs
namespace TaskFlow.Domain.Interfaces;

using TaskFlow.Domain.Entities;

public interface ITokenService
{
    string GenerateToken(User user);
}

// Exceptions/DuplicateEmailException.cs
namespace TaskFlow.Domain.Exceptions;

public sealed class DuplicateEmailException : DomainException
{
    public DuplicateEmailException()
        : base("An account with this email already exists.")
    {
    }
}

// Exceptions/InvalidCredentialsException.cs
namespace TaskFlow.Domain.Exceptions;

public sealed class InvalidCredentialsException : DomainException
{
    // Generic message: MUST NOT contain the words "email", "password", or "user" —
    // verified by a dedicated test. Prevents field-hinting / user enumeration.
    public const string GenericMessage = "Invalid credentials.";

    public InvalidCredentialsException()
        : base(GenericMessage)
    {
    }
}
```

**Required Test Names**:

`DuplicateEmailExceptionTests.cs`:

1. `DuplicateEmailException_InheritsDomainException`
2. `DuplicateEmailException_DefaultMessage_IsSet`

`InvalidCredentialsExceptionTests.cs`:

1. `InvalidCredentialsException_InheritsDomainException`
2. `InvalidCredentialsException_Message_DoesNotContainFieldHints` — assert message does
   NOT contain (case-insensitive) `"email"`, `"password"`, or `"user"`
3. `InvalidCredentialsException_Message_IsGenericConstant` — assert message equals
   `InvalidCredentialsException.GenericMessage` exactly

## 6. Quality Gates

| #  | Gate                  | Command                                                                | Pass Criteria        |
| -- | ----------------------- | ---------------------------------------------------------------------------- | ----------------------- |
| G1 | Compilation             | `dotnet build`                                                                | exit 0                    |
| G2 | Domain unit tests       | `dotnet test --filter "FullyQualifiedName~TaskFlow.Domain.Tests"`             | exit 0, 0 failures         |
| G3 | Zero PackageReference   | `dotnet list src/TaskFlow.Domain/TaskFlow.Domain.csproj package`              | output shows no packages   |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add any NuGet package to `TaskFlow.Domain.csproj`
- Implement `IUserRepository`, `IPasswordHasher`, or `ITokenService` — Infrastructure
  concrete classes belong to Batch 2 (`EP02-B2-02`, `EP02-B2-03`, `EP02-B2-04`)
- Create `RegisterUserCommand`/`AuthenticateUserCommand` or any Application-layer type —
  those belong to EP02-B1-03 and EP02-B1-04
- Add HTTP status codes, `[ProblemDetails]`, or any ASP.NET Core reference to either
  exception — Domain exceptions carry no transport concerns
- Change the wording of `InvalidCredentialsException.GenericMessage` once written — both
  failure paths (EP02-B1-04) depend on byte-for-byte identical text

### SCOPE BOUNDARY — Stop when

- All 7 deliverable files exist and all named tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to EP02-B1-01, EP02-B1-03, or EP02-B1-04 work

## 8. Anti-Patterns

| Anti-Pattern                                          | Why It Fails                                                     | Do Instead                                             |
| ---------------------------------------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------ |
| Adding `email`/`password`/`user` to `InvalidCredentialsException`'s message | Leaks which field is wrong, defeats timing/enumeration mitigation (Decision #9) | Use a fully generic string like `"Invalid credentials."` |
| Making `DuplicateEmailException`/`InvalidCredentialsException` carry an HTTP status property | Couples Domain to transport layer, violates Clean Architecture | Let Batch 3's exception-handling middleware map exception type → HTTP status |
| Adding `CancellationToken` as optional with a default value | Hides the requirement, inconsistent with EP01 repository patterns | Require `CancellationToken` as a mandatory parameter on every async method |
| Naming the exception message constant differently per exception (inconsistent casing/punctuation) | Test in EP02-B1-04 asserts exact string equality across two throw sites | Define `GenericMessage` once as `public const string` and reuse it |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G3)
2. If G1 fails because `User`, `Email`, or `PasswordHash` types are missing: confirm
   EP02-B1-01 has completed; if not, report BLOCKED rather than stubbing placeholder types
3. If G2 (unit tests) fails on the field-hint test: re-check the message string for
   substrings `"email"`, `"password"`, `"user"` case-insensitively
4. If G3 fails: remove any accidentally-added package from `TaskFlow.Domain.csproj`
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP
   and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in
   each attempt.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS

- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT

- Respect the current phase — do not jump ahead
- Every decision must trace back to a requirement or acceptance criterion
- Version pinning: ALL dependencies use exact versions, never floating

### TASKFLOW-BUILD-PIPELINE

- PostgreSQL is the ONLY database engine — no InMemory/SQLite (not applicable to this
  Domain-only task, but do not introduce persistence concerns here regardless)

## 11. Status Protocol

```text
Status: [DONE | FAILED | BLOCKED]
Progress: X/Y items (items = deliverables from Section 5)
Quality Gates: G1:PASS G2:PASS G3:PASS (or FAIL with error)
Blocker: (if BLOCKED — describe exactly what prevents progress)
Files Created: (list of new files)
Files Modified: (list of changed files)
```

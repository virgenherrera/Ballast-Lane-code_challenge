> [📚 INDEX](../../INDEX.md) / [EP02](../../epics/EP02-user-management.md) / EP02-B1-01

# Handoff: EP02-B1-01 — Domain: User Entity, Email VO, PasswordHash VO

## 1. Metadata

| Field         | Value                                                |
| ------------- | ----------------------------------------------------- |
| Task ID       | EP02-B1-01                                             |
| Task Name     | Domain: User Entity, Email VO, PasswordHash VO         |
| Batch         | 1 of 6 (EP02)                                          |
| Epic          | EP02 — User Management                                |
| User Stories  | US-001 (AC-001.1, AC-001.5, AC-001.6)                  |
| Persona       | Uncle Bob — Clean Architecture, DDD                    |
| Model Tier    | sonnet                                                 |

## 2. Objective

Create the `User` Domain entity with a private constructor and static `Create()` factory,
plus two immutable value objects: `Email` (rejects uppercase and invalid formats at
construction) and `PasswordHash` (rejects null/empty, hides its raw value). Zero
Infrastructure references, zero EF Core, zero BCrypt — pure C# with unit tests proving
every invariant.

## 3. Pre-Conditions

- [ ] `dotnet build src/TaskFlow.Domain/` exits 0
- [ ] `dotnet build tests/TaskFlow.Domain.Tests/` exits 0
- [ ] `src/TaskFlow.Domain/Exceptions/DomainException.cs` exists (already present from EP01 —
  `public class DomainException : Exception` — NOT abstract, has a public constructor
  taking a `string message`)
- [ ] `src/TaskFlow.Domain/Constants/FieldLengths.cs` exists (already present from EP01)
- [ ] No file named `User.cs`, `Email.cs`, or `PasswordHash.cs` exists under `src/TaskFlow.Domain/`
- [ ] .NET 10.0 SDK installed (`dotnet --version` reports 10.x — needed for `Guid.CreateVersion7()`)

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                      | Lines   | Why                                              |
| ----------------------------------------------------------- | ------- | ------------------------------------------------- |
| `docs/user-stories/US-001-user-registration.md`              | 1-30    | DOR — entity/VO contract, password/email rules     |
| `docs/user-stories/US-001-user-registration.md`               | 79-135  | Expected Deliverables, Validation Rules table      |
| `docs/user-stories/US-001-user-registration.md`               | 96-121  | Test Plan — named test cases this task must satisfy |
| `src/TaskFlow.Domain/Exceptions/DomainException.cs`           | all     | Existing base exception — NOT abstract, has ctor(string) |
| `src/TaskFlow.Domain/Constants/FieldLengths.cs`               | all     | Existing constants class — pattern to follow if a new constant is needed |

## 5. Deliverables

### Files to Create

| File Path                                                   | Contents                                                     |
| -------------------------------------------------------------- | ---------------------------------------------------------------- |
| `src/TaskFlow.Domain/ValueObjects/Email.cs`                    | Immutable VO: rejects uppercase, invalid format, >254 chars; value equality |
| `src/TaskFlow.Domain/ValueObjects/PasswordHash.cs`              | Immutable VO: rejects null/empty; `ToString()` does not expose raw value |
| `src/TaskFlow.Domain/Entities/User.cs`                         | Entity: private ctor + static `Create()` factory                  |
| `tests/TaskFlow.Domain.Tests/ValueObjects/EmailTests.cs`        | Unit tests for AC-001.5, AC-001.6                                  |
| `tests/TaskFlow.Domain.Tests/ValueObjects/PasswordHashTests.cs` | Unit tests for AC-001.1 (immutability, null/empty rejection)       |
| `tests/TaskFlow.Domain.Tests/Entities/UserTests.cs`             | Unit tests for AC-001.1 (entity construction, UUID v7)             |

### Expected Signatures

```csharp
// ValueObjects/Email.cs
namespace TaskFlow.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        // 1. if string.IsNullOrWhiteSpace(value) -> throw DomainException("Email is required.")
        // 2. if value.Length > MaxLength -> throw DomainException("Email exceeds maximum length of 254 characters.")
        // 3. if value != value.ToLowerInvariant() -> throw DomainException("Email must not contain uppercase characters.")
        //    NOTE: check casing BEFORE format, so an uppercase invalid-format email still reports the casing violation
        //    (defense-in-depth per Decision #4 — reject, do not normalize)
        // 4. simplified RFC 5322-subset regex/format check -> throw DomainException("Email format is invalid.") if it fails
        // 5. return new Email(value);
    }

    public override bool Equals(object? obj) => Equals(obj as Email);
    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}

// ValueObjects/PasswordHash.cs
namespace TaskFlow.Domain.ValueObjects;

public sealed class PasswordHash
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static PasswordHash Create(string hashedValue)
    {
        // if string.IsNullOrWhiteSpace(hashedValue) -> throw DomainException("Password hash is required.")
        // return new PasswordHash(hashedValue);
    }

    // Deliberately DOES NOT override ToString() to expose Value — inherits Object.ToString()
    // so accidental interpolation/logging never leaks the hash. Do NOT add a ToString() override.
}

// Entities/User.cs
namespace TaskFlow.Domain.Entities;

using TaskFlow.Domain.ValueObjects;

public sealed class User
{
    public Guid Id { get; }
    public Email Email { get; private set; }
    public string Name { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public DateTime CreatedAt { get; }

    private User(Guid id, Email email, string name, PasswordHash passwordHash, DateTime createdAt)
    {
        Id = id;
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public static User Create(Email email, string name, PasswordHash passwordHash)
    {
        // 1. var trimmedName = name?.Trim(); if string.IsNullOrWhiteSpace(trimmedName) -> throw DomainException("Name is required.")
        //    NOTE: full length/format validation of Name lives in RegisterUserValidator (Application layer, EP02-B1-03).
        //    This factory only guards the entity's own invariant: a non-blank trimmed name must exist.
        // 2. var id = Guid.CreateVersion7();
        // 3. var now = DateTime.UtcNow;
        // 4. return new User(id, email, trimmedName, passwordHash, now);
    }
}
```

**Required Test Names**:

`EmailTests.cs`:

1. `Email_Create_ValidLowercaseAddress_Succeeds`
2. `Email_Create_UppercaseInput_ThrowsDomainException` (AC-001.6)
3. `Email_Create_InvalidFormat_ThrowsDomainException` — parametrized: `"foo"`, `"foo@"`, `"@bar.com"` (AC-001.5)
4. `Email_Create_ExceedsMaxLength_ThrowsDomainException` — 255+ chars
5. `Email_Create_NullOrWhitespace_ThrowsDomainException`
6. `Email_Equals_SameValue_ReturnsTrue`
7. `Email_Equals_DifferentValue_ReturnsFalse`

`PasswordHashTests.cs`:

1. `PasswordHash_Create_ValidValue_Succeeds`
2. `PasswordHash_Create_NullOrEmpty_ThrowsDomainException`
3. `PasswordHash_ToString_DoesNotExposeRawValue`

`UserTests.cs`:

1. `User_Create_ValidData_AssignsUuidV7AndCreatedAt`
2. `User_Create_TrimsNameBeforeStorage` — `"  Alice  "` -> `"Alice"`
3. `User_Create_WhitespaceOnlyName_ThrowsDomainException`
4. `User_Create_TwoInstances_HaveDistinctTimeOrderedIds`

## 6. Quality Gates

| #  | Gate                  | Command                                                                            | Pass Criteria          |
| -- | ----------------------- | -------------------------------------------------------------------------------------- | ------------------------ |
| G1 | Compilation             | `dotnet build`                                                                          | exit 0                    |
| G2 | Domain unit tests       | `dotnet test --filter "FullyQualifiedName~TaskFlow.Domain.Tests"`                       | exit 0, 0 failures         |
| G3 | Zero PackageReference   | `dotnet list src/TaskFlow.Domain/TaskFlow.Domain.csproj package`                        | output shows no packages   |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add any NuGet package to `TaskFlow.Domain.csproj` — zero PackageReferences is a hard DOD item
- Create `IUserRepository`, `IPasswordHasher`, or `ITokenService` — those belong to EP02-B1-02
- Create `DuplicateEmailException` or `InvalidCredentialsException` — those belong to EP02-B1-02
- Implement full Name length/format validation (max 100 chars) — that is
  Application-layer scope in RegisterUserValidator (EP02-B1-03); `User.Create`
  only guards non-blank
- Add EF Core attributes (`[Key]`, `[Required]`) or any persistence-aware annotation
- Modify `DomainException.cs` or `FieldLengths.cs` — read-only context for this task

### SCOPE BOUNDARY — Stop when

- All 6 deliverable files exist and all named tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to EP02-B1-02, EP02-B1-03, or EP02-B1-04 work

## 8. Anti-Patterns

| Anti-Pattern                                     | Why It Fails                                             | Do Instead                                       |
| --------------------------------------------------- | ------------------------------------------------------------ | ---------------------------------------------------- |
| Normalizing email to lowercase instead of rejecting | Contradicts Decision #4 — reject uppercase, never silently lowercase | Throw `DomainException` on any uppercase character     |
| Overriding `PasswordHash.ToString()` to return `Value` | Leaks hash into logs/interpolation, breaks DOD               | Leave `ToString()` un-overridden (inherits `Object.ToString()`) |
| Making `DomainException` abstract or adding a new base class | `DomainException` already exists as a concrete class from EP01 | Reuse the existing `DomainException(string message)` constructor directly |
| Validating full Name constraints (100-char max) in `User.Create` | Duplicates Application-layer validation, violates layering intent for this task | Only guard non-blank in the entity; leave length/format to RegisterUserValidator |
| Checking email format before casing                | An uppercase+invalid-format email would report the wrong violation first | Check casing first, then format (see expected signature comment) |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G3)
2. If G1 (compilation) fails: check `DomainException` constructor signature matches
   `DomainException(string message)` exactly — it is NOT abstract, do not add `protected`
   modifiers expecting an abstract base
3. If G2 (unit tests) fails: re-check ordering of casing vs. format checks in `Email.Create`;
   re-check `<=`/`>` boundary on `MaxLength` (254 valid, 255 invalid)
4. If G3 (package reference) fails: remove any accidentally-added package from
   `TaskFlow.Domain.csproj`
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP
   and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in
   each attempt.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS

- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests cover Domain invariants and Application use cases in isolation (mocked repos)
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

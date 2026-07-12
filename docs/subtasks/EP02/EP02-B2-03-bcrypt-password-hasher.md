# Handoff: EP02-B2-03 — BcryptPasswordHasher Implementation

> [📚 INDEX](../../INDEX.md) / [EP02 — User Management](../../epics/EP02-user-management.md) / EP02-B2-03

## 1. Metadata

| Field         | Value                                        |
| ------------- | --------------------------------------------- |
| Task ID       | EP02-B2-03                                     |
| Task Name     | BcryptPasswordHasher Implementation           |
| Batch         | 2 of 6 (EP02 Batches 1-6 Plan)                |
| Epic          | EP02 — User Management                        |
| User Stories  | US-001 (AC-001.1), US-002 (AC-002.1, AC-002.4) |
| Persona       | Uncle Bob — Infrastructure / Security         |
| Model Tier    | sonnet                                         |

## 2. Objective

Implement `BcryptPasswordHasher`, the concrete `IPasswordHasher` adapter using
`BCrypt.Net-Next`, with a configurable work factor (12 for production per Decision #3, 4
for tests). Prove with unit tests that the hash never equals the plaintext and that
`Verify` correctly accepts matching passwords and rejects non-matching ones. This
unblocks `EP02-B3-01` (register endpoint) and `EP02-B4-01` (login endpoint), both of which
depend on a working concrete hasher instead of the `IPasswordHasher` mock used in
`EP02-B1-03`/`EP02-B1-04`'s unit tests.

## 3. Pre-Conditions

- [ ] EP02-B1-02 reports DONE — `IPasswordHasher` interface exists with signatures
      `Hash(string) -> PasswordHash` and `Verify(string, PasswordHash) -> bool`
- [ ] `src/TaskFlow.Domain/ValueObjects/PasswordHash.cs` exists (from EP02-B1-01)
- [ ] `dotnet build src/TaskFlow.Infrastructure/` exits 0
- [ ] `BCrypt.Net-Next` resolvable on NuGet (pin exact version at implementation time —
      record the resolved version in the Status Protocol `NOTES` field and update
      `README.md`'s Version Manifest)
- [ ] No file named `BcryptPasswordHasher.cs` exists under
      `src/TaskFlow.Infrastructure/Security/` (or equivalent — confirm folder convention
      via context bundle)

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                          | Lines   | Why                                                       |
| -------------------------------------------------------------- | ------- | ------------------------------------------------------------ |
| `docs/epics/EP02-engineering-addenda.md`                     | 36-47   | Decision #3 — BCrypt work factor 12 prod / 4 test, package name |
| `docs/user-stories/US-001-user-registration.md`               | 85-118  | `IPasswordHasher.Hash` contract and DOD (no plaintext in logs/exceptions) |
| `docs/user-stories/US-002-user-login.md`                      | 60-100  | `IPasswordHasher.Verify` contract, timing-attack usage (Decision #9 — dummy verify path) |
| `src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj`  | all     | Existing package references — add `BCrypt.Net-Next` here    |
| `src/TaskFlow.Domain/ValueObjects/PasswordHash.cs`            | all     | VO this hasher constructs and reads (created in EP02-B1-01) |

## 5. Deliverables

### Files to Create

| File Path                                                                        | Contents                                             |
| ------------------------------------------------------------------------------------ | ------------------------------------------------------- |
| `src/TaskFlow.Infrastructure/Security/BcryptPasswordHasher.cs`                      | Implements `IPasswordHasher` — `Hash`, `Verify`         |
| `src/TaskFlow.Infrastructure/Security/BcryptOptions.cs`                             | Options class: `WorkFactor` (int, default 12)           |
| `tests/TaskFlow.Infrastructure.Tests/Security/BcryptPasswordHasherTests.cs`         | Unit tests, work factor 4 for speed                     |

### Files to Modify

| File Path                                                            | Change                                    |
| ------------------------------------------------------------------------ | -------------------------------------------- |
| `src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj`           | Add `BCrypt.Net-Next` PackageReference     |
| `TaskFlow.sln`                                                       | Add `TaskFlow.Infrastructure.Tests` project if it does not already exist — confirm first via `fd -e csproj . tests/` before creating a new test project |

### Expected Signatures

```csharp
// BcryptOptions.cs
namespace TaskFlow.Infrastructure.Security;

public sealed class BcryptOptions
{
    public const string SectionName = "Bcrypt";

    /// <summary>
    /// BCrypt work factor (log2 rounds). 12 in production (Decision #3),
    /// 4 in tests for speed. Bound from configuration, defaults to 12
    /// when unset so production never silently runs at test strength.
    /// </summary>
    public int WorkFactor { get; set; } = 12;
}

// BcryptPasswordHasher.cs
using BCrypt.Net;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces; // adjust to actual IPasswordHasher namespace from EP02-B1-02
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private readonly int _workFactor;

    public BcryptPasswordHasher(IOptions<BcryptOptions> options)
    {
        _workFactor = options.Value.WorkFactor;
    }

    public PasswordHash Hash(string plainTextPassword)
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: _workFactor);
        return PasswordHash.Create(hashed);
    }

    public bool Verify(string plainTextPassword, PasswordHash passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash.Value);
    }
}
```

**NOTE**: If `IPasswordHasher`'s frozen signature from `EP02-B1-02` differs (e.g., a
constructor taking a plain `int workFactor` instead of `IOptions<BcryptOptions>`), match
the frozen interface. The `IOptions<BcryptOptions>` constructor shape above is the
recommended approach so production (`appsettings.json` → 12) and integration tests
(explicit `12` overridden to `4` via `Options.Create(new BcryptOptions { WorkFactor = 4
})`) can both construct the hasher without touching its source.

**Required Test Names** (unit tests, work factor 4, zero real database, zero HTTP):

1. `Hash_ValidPassword_ReturnsHashDifferentFromPlainText` — asserts
   `hash.Value != plainTextPassword`
2. `Hash_SamePasswordTwice_ProducesDifferentHashes` — BCrypt salts each call; two hashes of
   the same input must differ (proves salting, not just hashing)
3. `Verify_CorrectPassword_ReturnsTrue`
4. `Verify_IncorrectPassword_ReturnsFalse`
5. `Verify_EmptyPassword_ReturnsFalse` — does not throw, returns false
6. `Hash_ProducesValidBcryptFormat_StartsWithDollarTwoPrefix` — asserts
   `hash.Value.StartsWith("$2")` (BCrypt version prefix), proving `PasswordHash.Create`
   accepted a genuine BCrypt string, not an arbitrary one
7. `BcryptPasswordHasher_ConstructedWithWorkFactorFour_HashesFasterThanTwelve` — OPTIONAL,
   only include if it can run reliably without flaking on slow CI; if uncertain, omit and
   note the omission in `NOTES`

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
| -- | ------------------------ | ---------------------------------------------------------------------------------------------------- | ---------------------- |
| G1 | Compilation | `dotnet build src/TaskFlow.Infrastructure/` | exit 0 |
| G2 | Unit tests | `dotnet test tests/TaskFlow.Infrastructure.Tests/ --filter "FullyQualifiedName~BcryptPasswordHasherTests"` | exit 0, all named tests passed |
| G3 | No plaintext logging | Search `BcryptPasswordHasher.cs` for any `Console.Write`, `ILogger` call, or string interpolation of the raw password parameter | zero matches |
| G4 | Work factor configurable | `BcryptOptions.WorkFactor` is settable via constructor injection, not hardcoded as a literal inside `Hash`/`Verify` | verified by inspection |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Implement `UserRepository` or `JwtTokenService` — those are `EP02-B2-02`/`EP02-B2-04`
- Wire DI registration (`AddScoped<IPasswordHasher, BcryptPasswordHasher>`,
  `AddOptions<BcryptOptions>`) in `Program.cs` — belongs to a later API batch
- Add password-strength validation logic here — that already lives in
  `RegisterUserValidator` (Application layer, EP02-B1-03), this hasher only hashes/verifies
- Log, print, or include the plaintext password in any exception message — DOD requirement
  from US-001
- Add a dependency on `Microsoft.AspNetCore.Identity` or any other hashing library — BCrypt
  via `BCrypt.Net-Next` only, per Decision #3

### SCOPE BOUNDARY — Stop when

- `BcryptPasswordHasher.cs` and `BcryptOptions.cs` exist, implement `IPasswordHasher`
  exactly, and all named unit tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to `EP02-B2-04` (JWT)

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
| ------------------------------------------------------- | -------------------------------------------------------------- | ------------------------------------------------ |
| Hardcoding `workFactor: 12` inside `Hash()` | Test suite becomes slow (BCrypt work factor 12 ≈ 250ms/hash) and DOD requires configurability | Inject via `IOptions<BcryptOptions>` or constructor param, tests use 4 |
| Catching exceptions from `BCrypt.Verify` and returning `false` silently for ALL exception types | Masks genuine bugs (e.g., malformed hash format) as "wrong password" | Only treat `SaltParseException`/format errors as `false`; let unexpected exceptions propagate, or validate `PasswordHash.Create` already guarantees a well-formed hash |
| Asserting `hash == hash` for the same password twice | BCrypt salts every call — two hashes of the same password are never equal by design; this is what test #2 proves, not a bug | Assert hashes DIFFER across two `Hash()` calls with the same input |
| Testing with work factor 12 in the unit test suite | Each test takes ~250ms, multiplied by every test case — slows the whole suite | Always construct the hasher under test with work factor 4 |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G4)
2. If G1 fails: confirm `BCrypt.Net-Next`'s exact API — the package exposes
   `BCrypt.Net.BCrypt.HashPassword(string, int)` and `BCrypt.Net.BCrypt.Verify(string,
   string)`; verify against the resolved NuGet version's actual signature, older/newer
   versions may differ slightly
3. If G2 fails: check whether `PasswordHash.Create` (from EP02-B1-01) itself rejects
   BCrypt's `$2` prefix format — read `PasswordHash.cs`'s validation rule before assuming
   the hasher is wrong
4. If G3 fails: remove any logging/interpolation of the raw password parameter
5. If G4 fails: refactor to accept the work factor via constructor/options, not a literal
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
- Version pinning: ALL dependencies use exact versions, never floating

### TASKFLOW-BUILD-PIPELINE

- PostgreSQL is the ONLY database engine — no InMemory/SQLite (not applicable to this
  task, listed for completeness since it is a project-wide rule)
- No plaintext password appears in any log, exception message, or committed test fixture

## 11. Status Protocol

Include this block EXACTLY in your final response. No variations.

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B2-03
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {MUST include: exact BCrypt.Net-Next version pinned, confirmation work factor is configurable (not hardcoded), confirmation no plaintext password appears in logs/exceptions}
```

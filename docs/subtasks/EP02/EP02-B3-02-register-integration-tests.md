# Handoff: EP02-B3-02 — Integration Tests for Registration

## 1. Metadata

| Field        | Value                                                       |
| ------------ | ------------------------------------------------------------ |
| Task ID      | EP02-B3-02                                                  |
| Task Name    | Integration Tests: POST /api/auth/register                 |
| Batch        | 3 of 6 (EP02 Batch Plan)                                    |
| Epic         | EP02 — User Management                                     |
| User Stories | US-001 (AC-001.1 through AC-001.7)                          |
| Persona      | Kent C. Dodds — Integration Testing                         |
| Model Tier   | sonnet                                                      |

## 2. Objective

Write `RegisterTests.cs` — a full integration suite for `POST /api/auth/register` run
against a real PostgreSQL Testcontainers instance, proving every AC-001.x acceptance
criterion end-to-end through the real HTTP pipeline: successful registration, duplicate
email conflict, invalid/uppercase email rejection, weak password (all violations reported
together), missing fields (no short-circuit), name length/whitespace rules, and that the
password hash is never exposed in the response.

## 3. Pre-Conditions

- [ ] EP02-B3-01 STATUS: DONE — `AuthController.Register` action and
      `DuplicateEmailExceptionHandler` exist and are wired in `Program.cs`
- [ ] `dotnet build` exits 0
- [ ] `dotnet test --filter "FullyQualifiedName~TaskFlow.IntegrationTests.Tasks"` exits 0
      (existing task integration tests still pass before this task starts)
- [ ] No file named `RegisterTests.cs` exists under `tests/TaskFlow.IntegrationTests/Auth/`
- [ ] Docker is running (Testcontainers requires a live Docker daemon)

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                                     | Lines   | Why                                                       |
| ------------------------------------------------------------------------ | ------- | ----------------------------------------------------------- |
| `docs/user-stories/US-001-user-registration.md`                          | 48-84   | AC-001.1 through AC-001.7 — source of truth for test cases   |
| `docs/user-stories/US-001-user-registration.md`                          | 150-166 | Validation Rules table — exact boundary semantics            |
| `docs/architecture/api-contract.md`                                      | 106-133 | Section 3.1 — exact request/response/error shapes             |
| `docs/epics/EP02-engineering-addenda.md`                                 | 36-47   | Decision #3 — BCrypt work factor 4 for tests                 |
| `src/TaskFlow.API/Controllers/AuthController.cs`                       | all     | Register action under test (produced by EP02-B3-01)          |
| `src/TaskFlow.API/Contracts/RegisterRequest.cs`                        | all     | Request shape                                                 |
| `src/TaskFlow.API/Contracts/RegisterResponse.cs`                       | all     | Response shape                                                 |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs`         | all     | Base class: `Client`, `Services`, `ResetDatabaseAsync()`      |
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs` | all   | WebApplicationFactory + Testcontainers.PostgreSql (postgres:17-alpine); this is where the BCrypt work-factor-4 DI override must be added |
| `tests/TaskFlow.IntegrationTests/Common/AssertErrorResponse.cs`         | all     | Shared 400 assertion helper — reuse for all `*_Returns400` tests |
| `tests/TaskFlow.IntegrationTests/Tasks/CreateTaskTests.cs`              | all     | Integration test style reference: AAA pattern, `PostAsJsonAsync`, `ReadFromJsonAsync<T>` with case-insensitive options |

Also read `IPasswordHasher.cs` and `BcryptPasswordHasher.cs` (produced by EP02-B2-03) to
confirm the constructor shape needed to override the work factor via DI in the test factory.

## 5. Deliverables

### Files to Create

| File Path                                                    | Contents                                          |
| --------------------------------------------------------------- | ---------------------------------------------------- |
| `tests/TaskFlow.IntegrationTests/Auth/RegisterTests.cs`      | 9+ integration tests covering all AC-001.x cases  |

### Files to Modify

| File Path                                                                | Change                                                                    |
| -------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs` | In `ConfigureWebHost`, replace the `IPasswordHasher` DI registration with a work-factor-4 instance so tests hash/verify quickly (see Decision #3) |

### Expected Signatures

```csharp
// TaskFlowWebApplicationFactory.cs — inside ConfigureWebHost's services.ConfigureServices block,
// added alongside the existing AppDbContext override:
var hasherDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPasswordHasher));
if (hasherDescriptor is not null)
{
    services.Remove(hasherDescriptor);
}
services.AddScoped<IPasswordHasher>(_ => new BcryptPasswordHasher(workFactor: 4));
// Exact constructor signature depends on EP02-B2-03's BcryptPasswordHasher — if it takes
// no work-factor parameter, confirm with orchestrator before assuming one; do not invent
// a signature that does not exist.
```

```csharp
// RegisterTests.cs
namespace TaskFlow.IntegrationTests.Auth;

public sealed class RegisterTests : IntegrationTestBase
{
    private const string Endpoint = "/api/auth/register";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ... test methods below
}
```

### Required Test Names (in RegisterTests.cs)

```csharp
// AC-001.1
[Fact]
public async Task Register_ValidData_Returns201WithUserDto()
// Arrange: unique lowercase email, valid name, password meeting all 5 strength rules
// Act: POST /api/auth/register
// Assert: 201, body has {id, email, name, createdAt}, id is non-empty Guid

// AC-001.2
[Fact]
public async Task Register_DuplicateEmail_Returns409Conflict()
// Arrange: register once successfully, then attempt to register the same email again
// Act: POST /api/auth/register (second attempt)
// Assert: 409, body {status:409, error:"CONFLICT", message:"An account with this email already exists.", details:[]}

// AC-001.5
[Fact]
public async Task Register_InvalidEmail_Returns400WithFieldErrors()
// Arrange: email = "not-an-email" (missing @/domain/TLD)
// Act: POST /api/auth/register
// Assert: 400 via AssertErrorResponse.HasValidationErrorAsync with ("email", <issue text>)

// AC-001.6
[Fact]
public async Task Register_UppercaseEmail_Returns400()
// Arrange: email = "Jane@Example.com" (contains uppercase)
// Act: POST /api/auth/register
// Assert: 400 via AssertErrorResponse.HasValidationErrorAsync, field == "email"
// Per Decision #4: reject, do NOT normalize/lowercase server-side

// AC-001.3
[Fact]
public async Task Register_WeakPassword_Returns400WithAllViolations()
// Arrange: password = "abc" — fails min-length, uppercase, digit, special-char rules simultaneously
// Act: POST /api/auth/register
// Assert: 400, body.Details.Count >= 4 (one entry per broken rule, CascadeMode.Continue, no short-circuit)

// AC-001.4
[Fact]
public async Task Register_MissingAllFields_Returns400WithMultipleErrors()
// Arrange: email = "", name = "", password = ""
// Act: POST /api/auth/register
// Assert: 400, body.Details.Count == 3, one entry per field (email, name, password), no short-circuit

// AC-001.7
[Fact]
public async Task Register_NameTooLong_Returns400()
// Arrange: name = new string('a', 101) (101 chars, exceeds 100 max)
// Act: POST /api/auth/register
// Assert: 400 via AssertErrorResponse.HasValidationErrorAsync, field == "name"

// AC-001.7
[Fact]
public async Task Register_NameWhitespaceOnly_Returns400()
// Arrange: name = "   " (whitespace-only)
// Act: POST /api/auth/register
// Assert: 400 via AssertErrorResponse.HasValidationErrorAsync, field == "name"

// AC-001.1 (security)
[Fact]
public async Task Register_PasswordHashNotExposedInResponse()
// Arrange: valid registration payload
// Act: POST /api/auth/register
// Assert: 201; parse raw JSON body (JsonDocument); enumerate properties; assert NO property
// named "password", "passwordHash", or "hash" (case-insensitive) is present; exactly
// {id, email, name, createdAt} — same exhaustive-property-set pattern as
// CreateTaskTests.CreateTask_WithValidPayload_Returns201WithExactExpectedFieldSet
```

## 6. Quality Gates

| #  | Gate                             | Command                                                                                          | Pass Criteria                    |
| -- | --------------------------------- | --------------------------------------------------------------------------------------------------- | ----------------------------------- |
| G1 | Compilation                      | `dotnet build`                                                                                      | exit 0                              |
| G2 | Register integration tests       | `dotnet test --filter "FullyQualifiedName~TaskFlow.IntegrationTests.Auth.RegisterTests"`            | exit 0, 9+ passed, 0 failed         |
| G3 | No password/hash leak            | Code review: `Register_PasswordHashNotExposedInResponse` enumerates raw JSON properties, not just typed deserialization | verified |
| G4 | No short-circuit assertions      | Code review: `Register_MissingAllFields_Returns400WithMultipleErrors` and `Register_WeakPassword_Returns400WithAllViolations` assert `Details.Count` >= expected minimum, not just `>= 1` | verified |
| G5 | Existing task tests still pass   | `dotnet test --filter "FullyQualifiedName~TaskFlow.IntegrationTests.Tasks"`                          | exit 0, 0 failures                  |
| G6 | Full regression                  | `dotnet test`                                                                                        | exit 0                              |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Write tests for `POST /api/auth/login` — that is EP02-B4-02
- Modify `AuthController.cs`, `RegisterUserHandler`, or `RegisterUserValidator` — if a test
  reveals a genuine defect in those, report it to the orchestrator rather than silently
  patching Application/API code from a test task
- Add a new Testcontainers image or change `postgres:17-alpine` — pinned version per
  TASKFLOW-BUILD-PIPELINE
- Change `IntegrationTestBase.ResetDatabaseAsync()` to also truncate the `Users` table
  unless a test genuinely requires cross-test isolation — if needed, extend it explicitly
  and note the change in Files Modified
- Assert only HTTP status codes without checking the standard error body shape

### SCOPE BOUNDARY — Stop when

- `RegisterTests.cs` exists with all 9 required tests passing
- The BCrypt work-factor-4 DI override is in place in `TaskFlowWebApplicationFactory`
- All quality gates in Section 6 pass
- Do NOT proceed to Batch 4 (Login) work

## 8. Anti-Patterns

| Anti-Pattern                                            | Why It Fails                                                       | Do Instead                                                        |
| ---------------------------------------------------------- | ------------------------------------------------------------------ | ---------------------------------------------------------------- |
| Using the default BCrypt work factor (12) in tests        | ~250ms per hash makes the suite slow; Decision #3 mandates factor 4 for tests | Override `IPasswordHasher` registration in `TaskFlowWebApplicationFactory` |
| Asserting `response.StatusCode == 400` without checking body | Misses field-level detail regressions                              | Use `AssertErrorResponse.HasValidationErrorAsync` for all 400 cases |
| Checking only `body.Details.Count >= 1` for multi-violation tests | Would pass even if CascadeMode silently reverted to fail-fast     | Assert the exact expected count (or a documented minimum) per test |
| Deserializing the response with a typed DTO to check for absence of `passwordHash` | A typed DTO simply won't have unmapped fields, giving a false-positive pass even if the raw JSON leaks the hash | Parse with `JsonDocument`/`JsonElement.EnumerateObject()` and assert on the raw property names |
| Reusing the same email across independent test methods without `ResetDatabaseAsync` or a per-test factory | Test order becomes significant; a later test could fail on `409` unexpectedly | Use a fresh unique email per test (e.g., GUID-suffixed), consistent with `IntegrationTestBase`'s per-class factory lifetime |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G6)
2. If G1 fails: verify `RegisterRequest`/`RegisterResponse` field names match exactly what
   EP02-B3-01 produced
3. If G2 fails on a specific AC: re-read the Validation Rules table (Section 4) for the
   exact boundary (e.g., 100 vs. 101 chars for name, 72-byte BCrypt limit for password)
4. If G2 fails on Testcontainers startup: verify Docker is running and the credential
   workaround is in place (scratch `DOCKER_CONFIG` with `credsStore` omitted)
5. If G5 fails: the BCrypt DI override or a new migration must not have broken existing
   Tasks endpoints — check `TaskFlowWebApplicationFactory` service replacement did not
   accidentally remove the `AppDbContext` override
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and
   report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each
   attempt.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS

- Integration tests at API level (AAA pattern) are the PRIMARY confidence layer
- ALL tests must pass before any commit
- Breaking an existing test is a blocking issue — fix before proceeding
- Tests map directly to user story acceptance criteria
- PostgreSQL via Testcontainers (postgres:17-alpine) — never InMemory/SQLite

### TASKFLOW-ANTI-DRIFT

- Every decision must trace back to a requirement or acceptance criterion
- BCrypt test work factor is 4, pinned by Decision #3 — never the production value (12) in tests

### TASKFLOW-BUILD-PIPELINE

- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web
- Docker credential workaround: scratch `DOCKER_CONFIG` with `credsStore` omitted

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B3-02
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm BCrypt work factor 4 override in place; confirm no short-circuit on multi-violation tests; confirm password hash never exposed}
```

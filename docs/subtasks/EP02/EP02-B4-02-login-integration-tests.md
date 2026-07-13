# Handoff: EP02-B4-02 — Integration Tests for Login

## 1. Metadata

| Field        | Value                                            |
| ------------ | --------------------------------------------------- |
| Task ID      | EP02-B4-02                                          |
| Task Name    | Integration Tests: POST /api/auth/login             |
| Batch        | 4 of 6 (EP02 Batch Plan)                            |
| Epic         | EP02 — User Management                             |
| User Stories | US-002 (AC-002.1 through AC-002.4)                  |
| Persona      | Kent C. Dodds — Integration Testing                 |
| Model Tier   | sonnet                                              |

## 2. Objective

Write `LoginTests.cs` — a full integration suite for `POST /api/auth/login` run against a
real PostgreSQL Testcontainers instance, proving every AC-002.x acceptance criterion
end-to-end: successful login with token + user summary, generic 401 for both non-existent
email and wrong password (identical message), required-field validation, weak-password
acceptance at login (no strength check), rate limiting at 429 after 5 attempts/min/IP, and
JWT claim/expiry correctness.

## 3. Pre-Conditions

- [ ] EP02-B4-01 STATUS: DONE — `AuthController.Login` action, rate limiter, and
      `InvalidCredentialsExceptionHandler` exist and are wired in `Program.cs`
- [ ] EP02-B3-02 STATUS: DONE — `RegisterTests.cs` exists and passes (used here to seed a
      registered user via the real `/api/auth/register` endpoint rather than direct DB
      insertion, keeping tests black-box)
- [ ] `dotnet build` exits 0
- [ ] `dotnet test --filter "FullyQualifiedName~TaskFlow.IntegrationTests.Auth.RegisterTests"` exits 0
- [ ] No file named `LoginTests.cs` exists under `tests/TaskFlow.IntegrationTests/Auth/`
- [ ] Docker is running (Testcontainers requires a live Docker daemon)

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                                     | Lines   | Why                                                          |
| ------------------------------------------------------------------------ | ------- | --------------------------------------------------------------- |
| `docs/user-stories/US-002-user-login.md`                                | 38-59   | AC-002.1 through AC-002.4 — source of truth for test cases      |
| `docs/user-stories/US-002-user-login.md`                                | 102-110 | Validation Rules table — presence-only, generic error semantics |
| `docs/architecture/api-contract.md`                                     | 142-172 | Section 3.2 — exact request/response/error shapes incl. 429     |
| `docs/epics/EP02-engineering-addenda.md`                                | 21-34   | Decision #2 — JWT claims (`sub`, `email`, `name`), 15min expiry  |
| `docs/epics/EP02-engineering-addenda.md`                                | 109-124 | Decision #9 — rate limit spec: 5/min/IP, 429 + Retry-After       |
| `src/TaskFlow.API/Controllers/AuthController.cs`                       | all     | `Login` action under test (produced by EP02-B4-01)               |
| `src/TaskFlow.API/Contracts/LoginRequest.cs`                           | all     | Request shape                                                     |
| `src/TaskFlow.API/Contracts/LoginResponse.cs`                          | all     | Response shape                                                     |
| `tests/TaskFlow.IntegrationTests/Auth/RegisterTests.cs`                | all     | Pattern reference for seeding a user via the real register endpoint |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs`        | all     | Base class: `Client`, `Services`, `ResetDatabaseAsync()`         |
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs` | all   | WebApplicationFactory, JWT test env vars (`JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`), BCrypt work-factor-4 override (from EP02-B3-02) |
| `tests/TaskFlow.IntegrationTests/Common/AssertErrorResponse.cs`        | all     | Shared 400 assertion helper — reuse for `*_Returns400` tests      |

## 5. Deliverables

### Files to Create

| File Path                                              | Contents                                                     |
| ----------------------------------------------------------- | ------------------------------------------------------------------ |
| `tests/TaskFlow.IntegrationTests/Auth/LoginTests.cs`   | 10 integration tests covering all AC-002.x cases + rate limiting  |

### Files to Modify

None expected. If a genuine gap is found in `LoginResponse`/`AuthController` that blocks a
required test, report it to the orchestrator rather than silently patching production code
from this test-only task.

### Expected Signatures

```csharp
// LoginTests.cs
namespace TaskFlow.IntegrationTests.Auth;

public sealed class LoginTests : IntegrationTestBase
{
    private const string RegisterEndpoint = "/api/auth/register";
    private const string LoginEndpoint = "/api/auth/login";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Helper: registers a user via the real endpoint, returns (email, password) used,
    // so each test can log in against a known-good account without touching the DB directly.
    private async Task<(string Email, string Password)> RegisterUserAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "ValidPass1!";
        var payload = new { email, name = "Test User", password };
        var response = await Client.PostAsJsonAsync(RegisterEndpoint, payload);
        response.EnsureSuccessStatusCode();
        return (email, password);
    }

    // ... test methods below
}
```

### Required Test Names (in LoginTests.cs)

```csharp
// AC-002.1
[Fact]
public async Task Login_ValidCredentials_Returns200WithTokenAndUser()
// Arrange: RegisterUserAsync()
// Act: POST /api/auth/login with correct email + password
// Assert: 200, body has {accessToken (non-empty), tokenType == "Bearer", expiresIn == 900,
// user: {id, email, name}}

// AC-002.2
[Fact]
public async Task Login_NonExistentEmail_Returns401Generic()
// Arrange: email that was never registered
// Act: POST /api/auth/login
// Assert: 401, body {status:401, error:"UNAUTHORIZED", message:"Invalid email or password.", details:[]}

// AC-002.2
[Fact]
public async Task Login_WrongPassword_Returns401Generic()
// Arrange: RegisterUserAsync(), then submit the correct email with a wrong password
// Act: POST /api/auth/login
// Assert: 401, SAME body shape/message as Login_NonExistentEmail_Returns401Generic

// AC-002.2 (security — cross-test assertion)
[Fact]
public async Task Login_BothFailurePaths_ReturnIdenticalErrorMessage()
// Arrange: one request with a non-existent email, one request with RegisterUserAsync()'s
// email + wrong password
// Act: POST /api/auth/login (both)
// Assert: both responses' "message" field are string-equal — proves no user-enumeration leak

// AC-002.3
[Fact]
public async Task Login_EmptyEmail_Returns400WithFieldError()
// Arrange: email = "", password = "ValidPass1!"
// Act: POST /api/auth/login
// Assert: 400 via AssertErrorResponse.HasValidationErrorAsync, field == "email"

// AC-002.3
[Fact]
public async Task Login_EmptyPassword_Returns400WithFieldError()
// Arrange: email = "user@example.com", password = ""
// Act: POST /api/auth/login
// Assert: 400 via AssertErrorResponse.HasValidationErrorAsync, field == "password"

// AC-002.3 (login validator does not check strength)
[Fact]
public async Task Login_WeakPassword_PassesValidation_FailsAuth()
// Arrange: RegisterUserAsync() to get a real email, then submit that email with password = "a"
// (fails every registration strength rule, but login validator only checks presence)
// Act: POST /api/auth/login
// Assert: response is 401 (auth failure), NOT 400 (validation failure) — proves the login
// validator does not re-run password-strength rules

// Security — rate limiting (Decision #9)
[Fact]
public async Task Login_RateLimit_ExceedsFivePerMinute_Returns429()
// Arrange: any invalid credentials payload (content does not matter — rate limit is by IP)
// Act: POST /api/auth/login 6 times in immediate succession against the same HttpClient
// Assert: the 6th response is 429, includes a "Retry-After" header
// NOTE: TestServer requests typically share a single loopback-like connection context;
// if the rate limiter partitions by RemoteIpAddress and TestServer does not populate it
// consistently, this test may need to hit the endpoint through the WebApplicationFactory's
// real HTTP client (CreateClient()) which does populate connection info — verify against
// actual TestServer behavior before assuming partitioning fails silently.

// AC-002.1 (token structure)
[Fact]
public async Task Login_TokenContainsExpectedClaims()
// Arrange: RegisterUserAsync(), then log in
// Act: decode the JWT's payload segment (base64url decode the middle segment, parse as JSON —
// no signature verification needed for claim inspection)
// Assert: claims include "sub" (matches user.id), "email" (matches registered email),
// "name" (matches registered name)

// AC-002.1 (expiry)
[Fact]
public async Task Login_TokenExpiresIn900Seconds()
// Arrange: RegisterUserAsync(), then log in; capture the request time
// Act: decode the JWT's "exp" claim (Unix timestamp) and its "iat" claim (issued-at)
// Assert: exp - iat == 900 (or within a small tolerance if iat is not present, compare
// exp against request time + 900s with a few seconds' tolerance for test execution time)
```

## 6. Quality Gates

| #  | Gate                              | Command                                                                                     | Pass Criteria                |
| -- | ------------------------------------ | ------------------------------------------------------------------------------------------------ | -------------------------------- |
| G1 | Compilation                        | `dotnet build`                                                                                    | exit 0                          |
| G2 | Login integration tests            | `dotnet test --filter "FullyQualifiedName~TaskFlow.IntegrationTests.Auth.LoginTests"`             | exit 0, 10 passed, 0 failed      |
| G3 | Identical error message assertion  | Code review: `Login_BothFailurePaths_ReturnIdenticalErrorMessage` performs a direct string-equality assertion between the two response messages, not just "both are 401" | verified |
| G4 | Rate limit test isolation          | Code review: `Login_RateLimit_ExceedsFivePerMinute_Returns429` does not run in the same test class fixture window as other tests in a way that pre-exhausts the limiter for subsequent tests (each test class gets its own `TaskFlowWebApplicationFactory` per `IntegrationTestBase`, so this should be self-contained — verify) | verified |
| G5 | Existing register tests still pass | `dotnet test --filter "FullyQualifiedName~TaskFlow.IntegrationTests.Auth.RegisterTests"`          | exit 0, 0 failures               |
| G6 | Existing task tests still pass     | `dotnet test --filter "FullyQualifiedName~TaskFlow.IntegrationTests.Tasks"`                        | exit 0, 0 failures               |
| G7 | Full regression                    | `dotnet test`                                                                                     | exit 0                           |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Modify `AuthController.cs`, `AuthenticateUserHandler`, `AuthenticateUserValidator`, or the
  rate limiter configuration in `Program.cs` — if a test reveals a genuine defect, report it
  to the orchestrator rather than silently patching production code from a test task
- Write tests for `GET /api/auth/me` or JWT bearer authentication middleware — that is Batch 5
- Verify the JWT signature cryptographically — claim/expiry inspection only requires
  base64url-decoding the payload segment, not full signature validation (signature
  validation is Batch 5's JWT middleware concern)
- Add a new Testcontainers image or change `postgres:17-alpine` — pinned version
- Implement account lockout or exponential backoff logic — only the fixed 5/min/IP window
  is tested per Decision #9's Batch 4 scope

### SCOPE BOUNDARY — Stop when

- `LoginTests.cs` exists with all 10 required tests passing
- All quality gates in Section 6 pass
- Do NOT proceed to Batch 5 (Protected Access / JWT middleware) work

## 8. Anti-Patterns

| Anti-Pattern                                                    | Why It Fails                                                              | Do Instead                                                                     |
| -------------------------------------------------------------------- | ---------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| Seeding the test user by inserting directly into the `Users` table   | Bypasses the real registration flow, may not exercise the exact hashing path used in production | Register via `POST /api/auth/register` in a helper, keeping the test suite black-box |
| Asserting only `response.StatusCode == 401` for both failure paths   | Misses the core security assertion — that the messages are IDENTICAL          | Add `Login_BothFailurePaths_ReturnIdenticalErrorMessage` with a direct string comparison |
| Verifying JWT signature/expiry using a hardcoded secret copied from `TaskFlowWebApplicationFactory` | Couples the test to internal wiring instead of testing observable behavior; also unnecessary — claim inspection does not require signature verification | Base64url-decode the payload segment only; do not validate the signature |
| Running the rate-limit test against a shared/reused `HttpClient` across other tests in the same class without isolating state | Could cause the 6th "legitimate" request in a different test to be wrongly throttled, or vice versa mask a bug | Keep the rate-limit test self-contained: it makes its own 6 sequential calls and asserts within itself |
| Treating a 400 as a pass for `Login_WeakPassword_PassesValidation_FailsAuth` | Would silently hide the login validator running strength rules it should not | Assert exactly 401, and explicitly note in a comment that 400 would be a REGRESSION |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G7)
2. If G1 fails: verify `LoginRequest`/`LoginResponse` field names match exactly what
   EP02-B4-01 produced
3. If G2 fails on the rate-limit test: confirm the rate limiter partitions by IP and that
   `TestServer`'s `RemoteIpAddress` is populated for requests made through
   `WebApplicationFactory.CreateClient()` — if not populated, consult the orchestrator
   before attempting a workaround that bypasses testing the real partitioning logic
4. If G2 fails on claims/expiry tests: confirm the JWT's three-segment structure
   (`header.payload.signature`), base64url-decode (not standard base64 — replace `-`/`_`
   and pad) the middle segment only
5. If G6 (existing task tests) fails: the new test file or any accidental production-code
   edit broke an unrelated endpoint — revert any file outside Section 5's deliverables
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
- Generic error message for both failure paths is a frozen security decision — never split it
- expiresIn is 900 seconds (15 min), a named constant — never re-derive it independently in a test

### TASKFLOW-BUILD-PIPELINE

- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web
- Docker credential workaround: scratch `DOCKER_CONFIG` with `credsStore` omitted

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B4-02
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm identical error message assertion; confirm rate-limit test isolation; confirm JWT claim/expiry decode approach used}
```

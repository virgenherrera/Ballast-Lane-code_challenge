# Handoff: EP02-B5-03 — Integration Tests + Retrofit Existing Task Tests

## 1. Metadata

| Field         | Value                                                        |
| ------------- | ------------------------------------------------------------ |
| Task ID       | EP02-B5-03                                                   |
| Task Name     | Integration Tests + Retrofit Existing Task Tests             |
| Batch         | 5 of 6 (EP02 — Protected Access)                            |
| Epic          | EP02 — User Management                                       |
| User Stories  | US-003 (AC-003.2, AC-003.3, AC-003.4)                        |
| Persona       | Test Automation Engineer                                     |
| Model Tier    | sonnet                                                       |

## 2. Objective

This is the CRITICAL MIGRATION TASK. Add JWT token generation helpers to the test infrastructure, introduce an `AuthenticatedClient` property to `IntegrationTestBase`, retrofit ALL existing task integration tests to use authenticated requests (so they pass after `[Authorize]` was added in B5-01), write new auth-specific integration tests proving 401 rejection and user isolation, and add integration tests for `GET /api/auth/me`. After this task, the full integration test suite passes with JWT authentication enforced on all task endpoints.

## 3. Pre-Conditions

- [ ] EP02-B5-01 complete (JWT auth middleware active, `[Authorize]` on `TasksController`)
- [ ] EP02-B5-02 complete (`GET /api/auth/me` endpoint exists)
- [ ] `dotnet build` exits 0
- [ ] `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs` sets `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` env vars
- [ ] `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` exposes `SeedOwnerId` and `SeedOwnerId2` constants

If any pre-condition fails, report BLOCKED immediately.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                                        | Lines   | Why                                                  |
| --------------------------------------------------------------------------- | ------- | ---------------------------------------------------- |
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs`   | all     | Test factory — add token generation helper here       |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs`             | all     | Base class — add AuthenticatedClient property         |
| `tests/TaskFlow.IntegrationTests/Tasks/CreateTaskTests.cs`                  | all     | Representative test file — pattern for retrofit       |
| `tests/TaskFlow.IntegrationTests/Tasks/DeleteTaskEndpointTests.cs`          | all     | Retrofit: Client → AuthenticatedClient                |
| `tests/TaskFlow.IntegrationTests/Tasks/GetTaskByIdEndpointTests.cs`         | all     | Retrofit: Client → AuthenticatedClient                |
| `tests/TaskFlow.IntegrationTests/Tasks/ListTasksEndpointTests.cs`           | all     | Retrofit: Client → AuthenticatedClient                |
| `tests/TaskFlow.IntegrationTests/Tasks/UpdateTaskEndpointTests.cs`          | all     | Retrofit: Client → AuthenticatedClient                |
| `tests/TaskFlow.IntegrationTests/HarnessSmokeTests.cs`                      | all     | Verify if this needs auth (health endpoint = no)      |
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs`                      | all     | SeedOwnerId / SeedOwnerId2 constants for tokens       |
| `src/TaskFlow.API/Configuration/JwtOptions.cs`                              | all     | Secret/Issuer/Audience — must match test env vars     |
| `docs/epics/EP02-engineering-addenda.md`                                    | 21-34   | Decision #2 — JWT claims: sub, email, name            |

## 5. Deliverables

### Files to Create

| File Path                                                                   | Contents                                               |
| --------------------------------------------------------------------------- | ------------------------------------------------------ |
| `tests/TaskFlow.IntegrationTests/Auth/GetMeEndpointTests.cs`               | 4 tests: valid token 200, expired 401, no token 401, tampered 401 |
| `tests/TaskFlow.IntegrationTests/Auth/TaskAuthorizationTests.cs`            | 3 tests: no token 401, expired token 401, other-user-token returns empty |

### Files to Modify

| File Path                                                                   | Change                                                              |
| --------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs`   | Add `GenerateTestToken(Guid userId, string email, string name)` helper method |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs`             | Add `AuthenticatedClient` property with default Bearer token for `SeedOwnerId` |
| `tests/TaskFlow.IntegrationTests/Tasks/CreateTaskTests.cs`                  | Replace `Client` → `AuthenticatedClient` in all test methods         |
| `tests/TaskFlow.IntegrationTests/Tasks/DeleteTaskEndpointTests.cs`          | Replace `Client` → `AuthenticatedClient` in all test methods         |
| `tests/TaskFlow.IntegrationTests/Tasks/GetTaskByIdEndpointTests.cs`         | Replace `Client` → `AuthenticatedClient` in all test methods         |
| `tests/TaskFlow.IntegrationTests/Tasks/ListTasksEndpointTests.cs`           | Replace `Client` → `AuthenticatedClient` in all test methods         |
| `tests/TaskFlow.IntegrationTests/Tasks/UpdateTaskEndpointTests.cs`          | Replace `Client` → `AuthenticatedClient` in all test methods         |
| `tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj`          | Add `System.IdentityModel.Tokens.Jwt` package reference if not present |

### Expected Signatures

```csharp
// TaskFlowWebApplicationFactory.cs — add this method:
public string GenerateTestToken(
    Guid userId,
    string email = "seed@test.com",
    string name = "Seed User",
    TimeSpan? expiry = null)
{
    var secret = Environment.GetEnvironmentVariable("JWT_SECRET")!;
    var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")!;
    var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!;

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim("sub", userId.ToString()),
        new Claim("email", email),
        new Claim("name", name),
    };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(15)),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Overload for expired tokens:
public string GenerateExpiredTestToken(Guid userId)
{
    return GenerateTestToken(userId, expiry: TimeSpan.FromMinutes(-5));
}

// Overload for tampered tokens (signed with wrong key):
public string GenerateTamperedTestToken(Guid userId)
{
    var wrongKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("this-is-a-wrong-key-for-testing-tampered-tokens!"));
    var credentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);

    var claims = new[] { new Claim("sub", userId.ToString()) };
    var token = new JwtSecurityToken(
        issuer: Environment.GetEnvironmentVariable("JWT_ISSUER")!,
        audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE")!,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(15),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

```csharp
// IntegrationTestBase.cs — additions:
protected HttpClient AuthenticatedClient { get; private set; } = null!;

// In InitializeAsync(), after Client creation:
AuthenticatedClient = _factory.CreateClient();
var token = _factory.GenerateTestToken(SeedIdentity.SeedOwnerId);
AuthenticatedClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

**Required Test Names — GetMeEndpointTests.cs**:

1. `GetMe_WithValidToken_Returns200WithUserProfile`
2. `GetMe_WithExpiredToken_Returns401`
3. `GetMe_WithoutToken_Returns401`
4. `GetMe_WithTamperedToken_Returns401`

**Required Test Names — TaskAuthorizationTests.cs**:

1. `Tasks_WithoutToken_Returns401`
2. `Tasks_WithExpiredToken_Returns401`
3. `Tasks_WithOtherUserToken_ReturnsEmpty` — uses `SeedIdentity.SeedOwnerId2` to prove isolation (AC-003.4)

**Retrofit pattern** (for all 5 `Tasks/*EndpointTests.cs` files):

```csharp
// BEFORE:
var response = await Client.PostAsJsonAsync(Endpoint, payload);

// AFTER:
var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);
```

Keep `Client` (unauthenticated) for the new 401 tests in `TaskAuthorizationTests.cs`.

## 6. Quality Gates

| #  | Gate                    | Command                                                                  | Pass Criteria                      |
| -- | ----------------------- | ------------------------------------------------------------------------ | ---------------------------------- |
| G1 | Compilation             | `dotnet build`                                                           | exit 0                             |
| G2 | All integration tests   | `dotnet test tests/TaskFlow.IntegrationTests/`                           | exit 0, 0 failures                 |
| G3 | Existing task tests pass | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~Tasks."` | exit 0, all existing tests pass |
| G4 | New auth tests pass     | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~Auth."` | exit 0, 7 new tests pass          |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Modify `TasksController` or any API controller code — only test files change in this task
- Modify `Program.cs` or middleware — infrastructure was set up in B5-01
- Create E2E tests — those are Batch 6 (EP02-B6-01)
- Add new test categories or filtering attributes — use the existing test discovery pattern
- Change the test database setup or migration logic in `TaskFlowWebApplicationFactory`
- Remove the `Client` property from `IntegrationTestBase` — both `Client` (unauthenticated) and `AuthenticatedClient` are needed

### SCOPE BOUNDARY — Stop when:

- Token generation helpers exist in `TaskFlowWebApplicationFactory` (normal, expired, tampered)
- `AuthenticatedClient` is available in `IntegrationTestBase`
- All 5 existing `Tasks/*EndpointTests.cs` files use `AuthenticatedClient`
- 7 new auth tests exist and pass (4 GetMe + 3 TaskAuthorization)
- `dotnet test tests/TaskFlow.IntegrationTests/` exits 0 with 0 failures
- Do NOT proceed to E2E tests (EP02-B6-01)

## 8. Anti-Patterns

| Anti-Pattern                                         | Why It Fails                                              | Do Instead                                           |
| ---------------------------------------------------- | --------------------------------------------------------- | ---------------------------------------------------- |
| Using a different JWT secret in tests than the factory sets | Token validation fails — secret mismatch            | Read `JWT_SECRET` from env (already set by factory)  |
| Removing `Client` property                           | Breaks 401 tests that need unauthenticated requests       | Keep BOTH `Client` and `AuthenticatedClient`         |
| Generating tokens with `iss`/`aud` that don't match  | JWT validation rejects mismatched issuer/audience          | Use same `JWT_ISSUER`/`JWT_AUDIENCE` from factory env vars |
| Skipping `SeedOwnerId2` isolation test               | AC-003.4 (user isolation) remains untested                | Create token for `SeedOwnerId2`, verify empty task list |
| Using `Thread.Sleep` to test expiry                  | Flaky, slow                                               | Generate token with past expiry (`TimeSpan.FromMinutes(-5)`) |
| Asserting 403 instead of 401                         | Project uses 401 for all auth failures (no RBAC = no 403) | Assert `HttpStatusCode.Unauthorized` (401)           |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G4)
2. If G1 (compilation) fails: check missing `using` for `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`, `System.Security.Claims`; verify NuGet package added to test `.csproj`
3. If G2/G3 (existing tests fail with 401): `AuthenticatedClient` is not being used in the retrofitted tests — double-check ALL `Client.` usages in `Tasks/` files are replaced with `AuthenticatedClient.`
4. If G4 (new auth tests fail):
   - 401 expected but got 200: the `[Authorize]` attribute may be missing (check B5-01 completed)
   - 200 expected but got 401: token generation secret/issuer/audience mismatch
   - Isolation test returns non-empty: ownership filter not applied to queries (check `ICurrentUserContext` usage in repository/handler)
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each attempt.

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

### TASKFLOW-SECURITY
- JWT secret minimum 32 characters — validated at startup
- No plaintext passwords in logs, responses, or test output
- Standard error shape for all HTTP error responses (status, error, message, details)

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B5-03
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues or decisions made}
```

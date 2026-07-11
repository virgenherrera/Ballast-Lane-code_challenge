# Handoff: EP01-B2-05 — Integration Tests: UpdateTaskEndpointTests + Test Harness Bootstrap

## 1. Metadata

| Field        | Value                                                                        |
| ------------ | ---------------------------------------------------------------------------- |
| Task ID      | EP01-B2-05                                                                   |
| Task Name    | Integration Tests: HTTP-level test harness + UpdateTaskEndpointTests (all AC-007.1-8) |
| Batch        | 2 of N (EP01 Chunk-U)                                                        |
| Epic         | EP01 — Task Management                                                       |
| User Stories | US-007 (AC-007.1 through AC-007.8, full matrix)                              |
| Persona      | QA Automation — Integration Layer                                            |
| Model Tier   | sonnet                                                                       |

## 2. Objective

**Verified fact**: `tests/TaskFlow.IntegrationTests/` contains ONLY `Persistence/TaskRepositoryTests.cs` (a low-level EF Core test using Testcontainers). There is NO `WebApplicationFactory`, NO `IntegrationTestBase`, NO HTTP-level test helper, and NO second seed ownerId. This task CREATES the full HTTP-level integration test harness (using `WebApplicationFactory<Program>` + Testcontainers PostgreSQL) AND writes all 14 named integration tests covering AC-007.1 through AC-007.8.

Additionally, this task extends `SeedIdentity` with a second distinct owner GUID needed for AC-007.5 tests.

## 3. Pre-Conditions

- [ ] EP01-B2-04 STATUS: DONE — `TasksController` PATCH action compiles, DI is registered, middleware maps exceptions
- [ ] `dotnet build` exits 0
- [ ] `tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj` exists with references to `TaskFlow.API` and `Testcontainers.PostgreSql`
- [ ] `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` exists with one `SeedOwnerId`
- [ ] No `TaskFlowWebApplicationFactory.cs` exists under `tests/TaskFlow.IntegrationTests/`
- [ ] No `UpdateTaskEndpointTests.cs` exists under `tests/TaskFlow.IntegrationTests/`

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj` | 1-27 (full) | Verify existing packages (Testcontainers.PostgreSql already present) |
| `tests/TaskFlow.IntegrationTests/Persistence/TaskRepositoryTests.cs` | 1-70 (full) | Existing Testcontainers pattern — confirms postgres:17.5 usage |
| `src/TaskFlow.API/Program.cs` | full (post B2-04) | Understand DI/middleware registration to override in test factory |
| `src/TaskFlow.API/Controllers/TasksController.cs` | full (post B2-04) | Endpoint under test |
| `src/TaskFlow.API/Contracts/Tasks/UpdateTaskRequest.cs` | full (post B2-04) | Request shape |
| `src/TaskFlow.API/Middleware/ValidationExceptionHandler.cs` | full (post B2-04) | Maps validation errors to 400 |
| `src/TaskFlow.API/Middleware/TaskNotFoundExceptionHandler.cs` | full (post B2-04) | Maps not-found to 404 |
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` | 1-9 | Single ownerId to EXTEND with a second |
| `src/TaskFlow.Application/Tasks/Dtos/TaskDto.cs` | 1-11 | Response shape for assertions |
| `docs/architecture/api-contract.md` | 63-83 (section 2.3) | Standard error envelope shape to assert against |
| `docs/user-stories/US-007-update-task.md` | 100-122 | Test Plan — exact required test names |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs` | Custom `WebApplicationFactory<Program>` replacing DB with Testcontainers PostgreSQL, seeding test data |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs` | Shared base class: `IClassFixture<TaskFlowWebApplicationFactory>`, pre-configured `HttpClient`, helper methods for creating tasks via POST (or direct DB seed) |
| `tests/TaskFlow.IntegrationTests/Common/AssertErrorResponse.cs` | Shared assertion helper validating standard error envelope shape (status, error, message, details) |
| `tests/TaskFlow.IntegrationTests/Tasks/UpdateTaskEndpointTests.cs` | All 14 integration tests |

### Files to Modify

| File Path | Change |
|-----------|--------|
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` | ADD `SeedOwnerId2` — a second distinct GUID for AC-007.5 owner-mismatch tests |
| `tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj` | ADD `Microsoft.AspNetCore.Mvc.Testing` package reference if not present |

### Expected Signatures

```csharp
// SeedIdentity.cs — MODIFY: add second owner
namespace TaskFlow.Infrastructure.Identity;

public static class SeedIdentity
{
    public static readonly Guid SeedOwnerId = Guid.Parse("01961234-5678-7abc-def0-123456789abc");
    // Second owner for AC-007.5 integration tests (owner-mismatch -> 404)
    public static readonly Guid SeedOwnerId2 = Guid.Parse("02961234-5678-7abc-def0-123456789abc");
}
```

```csharp
// TaskFlowWebApplicationFactory.cs — key pattern
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests.Common;

public sealed class TaskFlowWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.5")
        .WithDatabase("taskflow_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration and replace with Testcontainers connection
            // Override ICurrentUserContext to return SeedOwnerId (primary test user)
            // Register EF Core with _postgres.GetConnectionString()
            // Apply migrations / EnsureCreated
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

```csharp
// AssertErrorResponse.cs — shared assertion helper
namespace TaskFlow.IntegrationTests.Common;

public static class AssertErrorResponse
{
    public static async Task AssertStandardError(
        HttpResponseMessage response,
        int expectedStatus,
        string expectedErrorCode)
    {
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.NotNull(body);
        Assert.Equal(expectedStatus, body.Status);
        Assert.Equal(expectedErrorCode, body.Error);
        Assert.NotNull(body.Message);
        Assert.NotNull(body.Details);
    }

    // Overload for field-level detail assertions
    public static async Task AssertValidationError(
        HttpResponseMessage response,
        string expectedField,
        string? expectedIssueContains = null) { /* ... */ }

    private sealed record ErrorEnvelope(int Status, string Error, string Message, object[] Details);
}
```

### Required Integration Test Names (14 — verbatim from story Test Plan)

1. `UpdateTask_WithNewTitle_Returns200WithTitleUpdated` — AC-007.1
2. `UpdateTask_WithValidStatusTransitionAnyDirection_Returns200WithNewStatus` — AC-007.2 (include reverse: Completed -> Pending)
3. `UpdateTask_WithPastDueDate_Returns200` — AC-007.3, CRITICAL asymmetry proof at integration level
4. `UpdateTask_WithInvalidStatusEnumValue_Returns400` — AC-007.4, assert `details[].field == "status"`
5. `UpdateTask_OwnedByAnotherUser_Returns404` — AC-007.5, uses SeedOwnerId2
6. `UpdateTask_NonExistentId_Returns404IdenticalToOwnerMismatch` — AC-007.5, **deserialize BOTH 404 response bodies (test #5's and this test's) and assert full structural equality**
7. `UpdateTask_WithEmptyTitleString_Returns400` — AC-007.6
8. `UpdateTask_WithWhitespaceOnlyTitle_Returns400` — AC-007.6 (separate from #7)
9. `UpdateTask_WithEmptyPayload_Returns400RequiresAtLeastOneField` — AC-007.7
10. `UpdateTask_WithPartialValidPayload_Returns200WithUpdatedFields` — AC-007.1, AC-007.3; assert unchanged fields retain pre-update values
11. `UpdateTask_WithMalformedGuidInRoute_Returns400` — AC-007.8, test at least TWO malformed shapes ("not-a-guid" and "12345")
12. `CreateTask_WithPastDueDate_StillReturns400` — **companion asymmetry proof**: same fixture, proves Create still rejects past dates alongside Update accepting them
13. `UpdateTask_ChangeStatus_UpdatedAtIsRefreshedToLaterTimestamp` — updatedAt monotonicity proof
14. `UpdateTask_WithValidPayload_Returns200WithExactExpectedFieldSet` — full response-shape assertion (all 8 fields of TaskDto present and correctly typed)

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0 |
| G2 | All integration tests | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~UpdateTaskEndpointTests"` | exit 0, 14 passed |
| G3 | 404-identity proof | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~NonExistentId_Returns404IdenticalToOwnerMismatch"` | exit 0, 1 passed with structural equality assertion |
| G4 | Asymmetry pair | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~PastDueDate"` | exit 0, both `UpdateTask_WithPastDueDate_Returns200` AND `CreateTask_WithPastDueDate_StillReturns400` pass |
| G5 | Full regression | `dotnet test` (entire solution) | exit 0, zero new failures in Domain.Tests, Application.Tests, or existing IntegrationTests |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Use InMemory or SQLite — PostgreSQL via Testcontainers only (per TASKFLOW-BUILD-PIPELINE)
- Hardcode a literal GUID for non-existent-id tests — use `Guid.NewGuid()` per test run
- Merge test #6's structural equality into two independent `.StatusCode.Should().Be(404)` assertions — the equality comparison IS the test
- Write E2E (Playwright) tests — that is EP01-B2-06
- Modify the controller, handler, or validator code — if tests fail, report which AC is violated, do not fix production code

### SCOPE BOUNDARY — Stop when:

- Test harness (`TaskFlowWebApplicationFactory`, `IntegrationTestBase`, `AssertErrorResponse`) is operational
- All 14 named tests pass including G3 and G4
- Full solution regression (G5) is green
- Do NOT proceed to Frontend/E2E work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| Using InMemory/SQLite provider | Masks PostgreSQL-specific behavior (e.g., UUID handling, case sensitivity) | Testcontainers with `postgres:17.5` |
| Two independent "returns 404" assertions for AC-007.5 | Doesn't prove response bodies are byte-identical | Deserialize both, assert structural equality |
| Testing `UpdateTask_WithPastDueDate_Returns200` in isolation from Create | Shared validator regression could make both wrongly accept past dates | Companion test in same file proving Create still rejects |
| Hardcoded literal GUID for non-existent-id | Risk of collision with seeded data as fixtures grow | `Guid.NewGuid()` fresh per test |
| Skipping the test harness and testing against a manually-started server | Non-reproducible, environment-dependent | `WebApplicationFactory<Program>` with full DI override |

## 9. Rollback Guidance

1. If G1 fails: likely missing `Microsoft.AspNetCore.Mvc.Testing` package — add to `.csproj`
2. If G2 fails with connection errors: verify Testcontainers is starting and `postgres:17.5` image is available (Docker must be running)
3. If G3 fails: check the equality assertion compares full deserialized objects, not just status codes
4. If G5 regression: identify which pre-existing test broke — likely the `TaskRepositoryTests` if DbContext registration changed
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Integration tests: API level (AAA pattern), PRIMARY confidence layer
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not modify production code to make tests pass
- Every decision must trace back to a requirement or AC
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B2-05
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G3 structural-equality assertion; confirm G4 asymmetry pair both pass; note if any production-code bug was discovered (report to orchestrator, do not fix)}
```

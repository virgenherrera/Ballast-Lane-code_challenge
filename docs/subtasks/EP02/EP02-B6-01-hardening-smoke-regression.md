# Handoff: EP02-B6-01 — Code Review + Auth Smoke Test + Regression

## 1. Metadata

| Field         | Value                                                   |
| ------------- | ------------------------------------------------------- |
| Task ID       | EP02-B6-01                                              |
| Task Name     | Code Review + Auth Smoke Test + Regression              |
| Batch         | 6 of 6 (EP02 — Hardening)                              |
| Epic          | EP02 — User Management                                  |
| User Stories  | US-003 (cross-cutting), Decision #7, Decision #9        |
| Persona       | QA Automation + Security Reviewer                       |
| Model Tier    | sonnet                                                  |

## 2. Objective

Final hardening pass for EP02: create a Playwright auth smoke test verifying backend-alive and authentication flow via HTTP assertions, execute the full code-review checklist (security, architecture, conventions), and run the complete regression suite (unit + integration + E2E) confirming zero failures. This is the quality gate that closes EP02 — no new features, only verification and one new E2E test file.

## 3. Pre-Conditions

- [ ] EP02-B5-03 complete (`dotnet test` exits 0 with all auth integration tests passing)
- [ ] Docker Compose environment is functional (`docker compose up -d` starts all services)
- [ ] `e2e/` directory exists with Playwright configured (`e2e/playwright.config.ts` present)
- [ ] `npx playwright install` has been run (browsers available)
- [ ] `src/TaskFlow.API/Controllers/AuthController.cs` has Register, Login, and GetMe actions
- [ ] Rate limiting is registered in `Program.cs` (from Batch 4)

If any pre-condition fails, report BLOCKED immediately.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                       | Lines   | Why                                               |
| ---------------------------------------------------------- | ------- | ------------------------------------------------- |
| `e2e/playwright.config.ts`                                 | all     | Test config — baseURL, testDir, projects           |
| `e2e/src/tests/health-semaphore.spec.ts`                   | all     | Pattern reference for writing smoke tests          |
| `e2e/src/tests/tasks/create-task.spec.ts`                  | 1-30    | Convention reference: imports, describe blocks      |
| `src/TaskFlow.API/Program.cs`                              | all     | Full pipeline — verify rate limiter, auth, etc.    |
| `src/TaskFlow.API/Controllers/AuthController.cs`           | all     | Verify no plaintext passwords, standard shapes     |
| `src/TaskFlow.Infrastructure/Identity/JwtTokenService.cs`  | all     | Verify signing key length validation               |
| `src/TaskFlow.Infrastructure/Identity/BcryptPasswordHasher.cs` | all | Verify work factor configuration (12 prod, 4 test) |
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs`     | all     | Seed user constants for E2E login                  |
| `docs/epics/EP02-engineering-addenda.md`                   | 79-87   | Decision #7 — E2E test strategy                   |
| `docs/epics/EP02-engineering-addenda.md`                   | 109-124 | Decision #9 — Rate limiting requirements           |
| `docker-compose.yml`                                       | all     | Verify services, env vars, health checks           |

## 5. Deliverables

### Files to Create

| File Path                                         | Contents                                               |
| ------------------------------------------------- | ------------------------------------------------------ |
| `e2e/src/tests/auth/auth-smoke.spec.ts`           | Playwright auth smoke test (4 test cases via HTTP API assertions) |

### Files to Modify

| File Path                          | Change                                                          |
| ---------------------------------- | --------------------------------------------------------------- |
| (none expected — review only)      | If code review finds issues, fix them and list here             |

### Expected Signatures

```typescript
// e2e/src/tests/auth/auth-smoke.spec.ts
import { test, expect } from '@playwright/test';

const API_BASE = `http://localhost:${process.env.API_PORT}`;

test.describe('Auth Smoke Tests', () => {

  test('backend is alive — GET /health returns 200', async ({ request }) => {
    const response = await request.get(`${API_BASE}/health`);
    expect(response.status()).toBe(200);
  });

  test('page loads at base URL', async ({ page }) => {
    // Verify the frontend is reachable (may redirect to login)
    const response = await page.goto('/');
    expect(response?.status()).toBeLessThan(400);
  });

  test('register + login flow via API', async ({ request }) => {
    const uniqueEmail = `smoke-${Date.now()}@test.com`;

    // Register
    const registerRes = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email: uniqueEmail, password: 'SmokeTe$t1234', name: 'Smoke User' },
    });
    expect(registerRes.status()).toBe(201);

    // Login
    const loginRes = await request.post(`${API_BASE}/api/auth/login`, {
      data: { email: uniqueEmail, password: 'SmokeTe$t1234' },
    });
    expect(loginRes.status()).toBe(200);
    const loginBody = await loginRes.json();
    expect(loginBody.accessToken).toBeTruthy();
  });

  test('authenticated /api/tasks succeeds, unauthenticated returns 401', async ({ request }) => {
    // Unauthenticated
    const noAuthRes = await request.get(`${API_BASE}/api/tasks`);
    expect(noAuthRes.status()).toBe(401);
    const errorBody = await noAuthRes.json();
    expect(errorBody.error).toBe('UNAUTHORIZED');

    // Login with seed user (or register fresh)
    const email = `smoke-auth-${Date.now()}@test.com`;
    await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, password: 'SmokeTe$t1234', name: 'Auth Test' },
    });
    const loginRes = await request.post(`${API_BASE}/api/auth/login`, {
      data: { email, password: 'SmokeTe$t1234' },
    });
    const { accessToken } = await loginRes.json();

    // Authenticated
    const authRes = await request.get(`${API_BASE}/api/tasks`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect(authRes.status()).toBe(200);
  });

});
```

### Code Review Checklist (execute inline, fix any failures)

Run each check and document pass/fail. If a check FAILS, fix the issue as part of this task.

| # | Check | How to Verify | Expected |
| - | ----- | ------------- | -------- |
| 1 | No plaintext passwords in logs/responses/test output | `rg -i "password" src/ --type cs -l` → verify none log or return password fields; check all response DTOs exclude password | No password field in any response DTO or log statement |
| 2 | No Infrastructure references in Domain or Application | `rg "using TaskFlow.Infrastructure" src/TaskFlow.Domain/ src/TaskFlow.Application/` | zero matches |
| 3 | All exception handlers return standard error shape | Review all `IExceptionHandler` implementations in `src/TaskFlow.API/Middleware/` | Each returns `{ status, error, message, details }` |
| 4 | Rate limiting active on login endpoint | Verify `[EnableRateLimiting("login")]` on Login action AND `AddRateLimiter(...)` in Program.cs | Both present |
| 5 | JWT secret length >= 32 chars validated at startup | Check Program.cs or a startup validator throws if `JWT_SECRET.Length < 32` | Validation present (either manual check or FluentValidation on options) |
| 6 | All async methods propagate CancellationToken | `rg "async Task" src/ --type cs -l` → spot-check that `CancellationToken ct` parameter exists and is passed to downstream calls | All handlers/controllers propagate ct |
| 7 | BCrypt work factor: 12 prod, 4 test | Check `BcryptPasswordHasher` reads work factor from config; verify `appsettings.json` = 12, test override = 4 | Configured correctly |
| 8 | No hardcoded secrets in source (excluding test harness) | `rg "secret|password" src/ --type cs -i` → verify no literal credentials outside test fixtures | Zero hardcoded secrets in src/ |

## 6. Quality Gates

| #  | Gate                    | Command                                                       | Pass Criteria                            |
| -- | ----------------------- | ------------------------------------------------------------- | ---------------------------------------- |
| G1 | Compilation             | `dotnet build`                                                | exit 0                                   |
| G2 | Full test suite         | `dotnet test`                                                 | exit 0, 0 failures (unit + integration)  |
| G3 | E2E tests              | `cd e2e && npx playwright test`                               | exit 0, 0 failures                       |
| G4 | No plaintext passwords  | `rg -i "password" src/TaskFlow.API/Contracts/ --type cs`      | zero matches in response DTOs            |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Add browser-driven form E2E tests (register/login FORMS) — those are EP04 scope per Decision #7
- Implement new API endpoints or features — this is verification only
- Refactor working code for style preferences — only fix actual defects found in the code review
- Add new NuGet packages unless a code review finding requires it
- Modify the Docker Compose service definitions (unless a missing health check blocks E2E)
- Change test database configuration or migrations
- Add authentication to the health endpoint (it must remain public)

### SCOPE BOUNDARY — Stop when:

- `e2e/src/tests/auth/auth-smoke.spec.ts` exists with 4 test cases passing
- All 8 code review checklist items are verified (pass or fixed)
- `dotnet test` exits 0 (full unit + integration suite)
- `npx playwright test` exits 0 (full E2E suite including new auth smoke test)
- Do NOT start EP03 or any other epic's work

## 8. Anti-Patterns

| Anti-Pattern                                          | Why It Fails                                              | Do Instead                                       |
| ----------------------------------------------------- | --------------------------------------------------------- | ------------------------------------------------ |
| Using browser navigation to test auth API             | Decision #7 says API-level HTTP assertions, not forms      | Use Playwright `request` API for direct HTTP calls |
| Sharing state between E2E tests                       | Tests become order-dependent and flaky                     | Each test registers its own unique user            |
| Skipping the code review and only running tests       | Security/architecture issues go undetected                 | Execute the full 8-item checklist BEFORE running regression |
| Fixing cosmetic issues found in review                | Scope creep — style is not a defect                        | Only fix actual security/architecture violations  |
| Hardcoding API_PORT in the smoke test                 | Breaks when docker-compose uses different ports             | Read from `process.env.API_PORT`                  |
| Running E2E without docker-compose up                 | Tests fail with connection refused — not a code bug        | Ensure Docker services are running before E2E     |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G4)
2. If G1 (compilation) fails: a code review fix likely introduced a syntax error — revert that specific fix and try alternative
3. If G2 (unit/integration tests) fails: a code review fix broke existing behavior — revert the fix; the review finding may need a different resolution approach
4. If G3 (E2E) fails:
   - Connection refused: Docker services not running — run `docker compose up -d` and retry
   - 401 on register/login: those endpoints should NOT have `[Authorize]` — check B5-01 didn't over-apply
   - Timeout: increase Playwright timeout or check if API is slow to start (health check dependency)
5. If G4 (plaintext passwords) fails: remove the offending field from the response DTO; do NOT add it back for "debugging convenience"
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each attempt.

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
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

### TASKFLOW-SECURITY
- JWT secret minimum 32 characters — validated at startup
- No plaintext passwords in logs, responses, or test output
- Standard error shape for all HTTP error responses (status, error, message, details)
- BCrypt work factor: 12 production, 4 test
- Rate limiting on login: 5 attempts/minute/IP

### TASKFLOW-E2E
- E2E tests use Playwright `request` API for backend-only verification
- Each test is independent — no shared state between tests
- Docker Compose must be running before E2E execution
- API_PORT and WEB_PORT read from environment variables

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B6-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues or decisions made}
```

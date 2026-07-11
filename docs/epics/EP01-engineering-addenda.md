> [📚 INDEX](../INDEX.md) / [EP01](EP01-user-management.md) / Engineering Addenda

# EP01 — Engineering Addenda

Refinement decisions made during EP01 grooming. These are binding technical specs that
complement the user stories' "what" with the "how." Produced by the full scrum team
(TL, QA, BE, DevOps, FE) and approved by the PO.

## 1. Password Policy

| Rule | Value |
| ---- | ----- |
| Minimum length | 8 characters |
| Uppercase required | At least 1 |
| Number required | At least 1 |
| Special character required | At least 1 |

Enforced by FluentValidation on `RegisterRequest` DTO. Domain layer's `PasswordHash`
value object receives the already-hashed result — it does not re-validate strength.

## 2. JWT Configuration

| Parameter | Value |
| --------- | ----- |
| Signing algorithm | HS256 |
| Expiry | 15 minutes |
| Refresh token | Out of scope — not in API contract |
| Secret minimum length | 32 characters |
| Claims | `sub` (user ID as UUID v7), `email`, `name` |
| Issuer | From `JWT_ISSUER` env var |
| Audience | From `JWT_AUDIENCE` env var |

No refresh token endpoint. When the token expires, the user must log in again. This is
acceptable for a demo/evaluation project with a 15-minute window.

## 3. Password Hashing

| Parameter | Value |
| --------- | ----- |
| Algorithm | BCrypt |
| Work factor | 12 |
| NuGet package | `BCrypt.Net-Next` (exact version in README Version Manifest) |
| Test work factor | 4 (fast, for integration test speed) |

BCrypt is the industry standard for password hashing in .NET. Work factor 12 balances
security with performance (~250ms per hash on modern hardware). Integration tests use
work factor 4 to avoid slow test suites.

## 4. Email Handling

| Aspect | Rule |
| ------ | ---- |
| API contract | Accepts lowercase only |
| Backend validation | If email contains uppercase → 400 Bad Request |
| Frontend | Lowercases input before sending to API |
| Storage | Stored as-is (always lowercase after validation) |
| Uniqueness | Case-insensitive unique index on `email` column (PostgreSQL `LOWER()`) |
| Lookup | `WHERE email = @input` (input is already lowercase) |

## 5. UUID Strategy

| Parameter | Value |
| --------- | ----- |
| Version | UUID v7 (time-ordered) |
| Generation | Client-side: `Guid.CreateVersion7()` (.NET 9+) |
| Benefits | Time-ordered for better B-tree index performance, native .NET support |
| Primary keys | All entities use UUID v7 as primary key |

## 6. US-003 — Protected Endpoint for Testing

US-003 (Protected Access) is cross-cutting and applies to all `/api/tasks/*` endpoints,
which belong to EP02. To test US-003 within EP01 scope:

- Add `GET /api/auth/me` — returns the authenticated user's profile
- This gives US-003 a concrete EP01-owned endpoint to test against
- Response: `{ id, email, name, createdAt }` (same shape as register response)
- 401 if no token / expired / tampered

## 7. E2E Test Strategy for EP01

EP01 is backend-only. Full E2E tests (browser-driven) require EP04's UI. Instead:

- Initialize the Playwright project structure (scaffold, config)
- Write one **smoke test**: load the frontend URL, verify the page loads, verify the
  backend is alive via `/health`
- Full auth E2E tests (register form, login form, protected routes) move to EP04's DOD
- Integration tests at the API level ARE the primary confidence layer for EP01

## 8. Angular Proxy (No CORS)

No CORS configuration needed. Angular dev server uses a proxy:

```json
// proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false
  },
  "/health": {
    "target": "http://localhost:5000",
    "secure": false
  }
}
```

In production (Docker), nginx proxies `/api/*` to `taskflow-api` — same pattern, no CORS.

## 9. Login Security — Timing Attack Prevention

**Problem**: if "user not found" skips password hashing but "wrong password" performs it,
response time leaks which case occurred — defeating the generic error message.

**Solution** (two layers):

1. **Constant-time comparison**: BCrypt.Verify is already constant-time. When user is not
   found, perform a dummy BCrypt.Verify against a throwaway hash so both code paths take
   the same time.
2. **Rate limiting**: ASP.NET Core's built-in rate limiting middleware. Exponential backoff
   on the `/api/auth/login` endpoint:
   - 5 attempts per minute per IP
   - After exceeding: `429 Too Many Requests` with `Retry-After` header
   - Backoff increases exponentially on repeated violations

## 10. Custom 401 Error Shape

ASP.NET's default JWT bearer challenge returns a plain `401` with `WWW-Authenticate`
header but no JSON body. This doesn't match the project's standard error shape.

Custom middleware maps JWT authentication failures to:

```json
{
  "status": 401,
  "error": "UNAUTHORIZED",
  "message": "Missing, invalid, or expired authentication token.",
  "details": []
}
```

## 11. Batch Plan

| Batch | Scope | Deliverables |
| ----- | ----- | ------------ |
| 0 | Infra bootstrap | .NET solution (8 projects), docker-compose db, .env.example, health endpoint, appsettings wiring, project references |
| 1 | Domain + Application | User entity, Email/PasswordHash VOs, IUserRepository, ITokenService, IPasswordHasher, RegisterUserUseCase, AuthenticateUserUseCase, FluentValidation validators, unit tests |
| 2 | Infrastructure | TaskFlowDbContext, User entity config, UserRepository, initial migration (Users table), JwtTokenService, BcryptPasswordHasher |
| 3 | API — US-001 | AuthController (register), exception middleware, env-var validation, integration tests for register |
| 4 | API — US-002 | AuthController (login), dummy-hash timing protection, rate limiting, integration tests for login |
| 5 | API — US-003 | JWT auth middleware, `GET /api/auth/me`, custom 401 shape, ownership claim extraction, integration tests |
| 6 | Hardening | Code review, Playwright smoke test scaffold, regression suite, cleanup |

## 12. Implementation Order

```mermaid
%% EP01 implementation — strictly sequential with shared foundation
flowchart LR
    B0["Batch 0\nInfra Bootstrap"] --> B1["Batch 1\nDomain + App"]
    B1 --> B2["Batch 2\nInfrastructure"]
    B2 --> B3["Batch 3\nUS-001 Register"]
    B3 --> B4["Batch 4\nUS-002 Login"]
    B4 --> B5["Batch 5\nUS-003 Protected"]
    B5 --> B6["Batch 6\nHardening"]

    style B0 fill:#94a3b8,color:#fff
    style B1 fill:#f59e0b,color:#fff
    style B2 fill:#22c55e,color:#fff
    style B3 fill:#3b82f6,color:#fff
    style B4 fill:#3b82f6,color:#fff
    style B5 fill:#3b82f6,color:#fff
    style B6 fill:#8b5cf6,color:#fff
```

## Related Documents

- [EP01 — User Management](EP01-user-management.md) — parent epic
- [API Contract](../architecture/api-contract.md) — endpoint specs
- [Testing Strategy](../architecture/testing-strategy.md) — test coverage mapping
- [Tech Stack](../architecture/tech-stack.md) — technology decisions
- [Process Protocols](../process.md) — DOR, DOD, refinement

# TaskFlow — E2E Tests

Browser-level tests using Playwright 1.61.1 against the Docker stack.

## Prerequisites

- Docker stack running (`docker compose up -d` from project root)
- Playwright browsers installed: `pnpm exec playwright install chromium`

## Environment Variables

| Variable | Required | Source | Description |
| -------- | -------- | ------ | ----------- |
| `WEB_PORT` | Yes | `.env` | Port where nginx serves the Angular SPA (e.g. `4200`) |
| `API_PORT` | Yes | `.env` | Port where the .NET API is exposed (e.g. `3000`) |
| `CI` | No | CLI | Set to `true` in CI pipelines (enables retries, single worker) |

The project root `.env` file is the source of truth for port configuration.
`playwright.config.ts` loads it automatically via `dotenv` — no manual prefix needed.
Explicit env vars on the CLI still take precedence. Zod validates all required variables
at config load time.

## Running

```bash
cd e2e

# Run all tests (headless — reads ports from ../.env automatically)
pnpm test:e2e

# Run with browser visible (headed mode)
pnpm test:e2e:headed

# Run in debug mode (step-by-step with Playwright Inspector)
pnpm test:e2e:debug

# Override a port for a single run
WEB_PORT=8080 pnpm test:e2e
```

## Structure

```text
e2e/
├── playwright.config.ts              # Zod-validated config
├── src/
│   ├── constants/
│   │   └── selectors.constants.ts    # Centralized DOM selectors
│   ├── fixtures/
│   │   ├── auth.fixture.ts           # Auth fixture (register + login per worker)
│   │   └── tasks.fixture.ts          # Task page object fixtures
│   ├── pages/
│   │   ├── create-task.page.ts       # Page Object: create task form
│   │   ├── task-detail.page.ts       # Page Object: task detail view
│   │   └── task-list.page.ts         # Page Object: task list / dashboard
│   └── tests/
│       └── tasks/
│           ├── create-task.spec.ts           # Create task (happy + validation)
│           ├── delete-task.spec.ts           # Delete task (confirm + cancel)
│           ├── filter-tasks-by-status.spec.ts# Filter by status tab
│           ├── list-tasks-pagination.spec.ts # Pagination (next/prev)
│           ├── update-task.spec.ts           # Update status from list
│           └── view-task-detail-from-list.spec.ts # Navigate to detail view
└── tsconfig.json
```

## Test Coverage

| Spec | Tests | Asserts |
| ---- | ----- | ------- |
| Create Task | 3 | Form submission, success feedback, empty-title validation, past-date validation |
| Delete Task | 2 | Delete with confirm (204, removed from DOM), cancel keeps task |
| Filter by Status | 2 | Filter shows only matching status, non-matching hidden |
| Pagination | 1 | Page 1 (20 items), next/prev navigation, URL params |
| Update Task | 1 | Status change via dropdown fires PATCH, DOM reflects change |
| View Detail | 1 | Navigate from list, detail fields visible (title, description, status, dates) |

## Known Limitations

E2E tests currently run against the same Docker stack used for development and demo.
There is no isolated environment — tests share the database, API, and rate limiter with
the live application.

Data conflicts are mitigated by user-level isolation: each Playwright worker registers a
unique throwaway user (`e2e-worker-*@test.local`), so test tasks never appear in the demo
account's session. However, test data accumulates without cleanup.

### What an isolated E2E environment would enable

- **Controlled seed data** — seed the database with a deterministic dataset before each
  suite (e.g. 10,000 tasks to stress-test pagination, edge cases with empty descriptions,
  tasks at every status) instead of relying on whatever the demo seeder provides.
- **Data assertions** — query the database directly after a test to verify side effects
  (e.g. confirm a deleted task is actually gone, not just hidden from the UI).
- **Rate limiter freedom** — no 429s during parallel test runs.
- **Teardown without risk** — truncate tables between suites without affecting the demo
  account.

### Recommended approach

A dedicated `docker-compose.e2e.yml` that spins up an isolated PostgreSQL instance and API
on separate ports, with a test-specific seed script. Playwright config would point to the
E2E ports. The main stack remains untouched.

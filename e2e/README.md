# TaskFlow — E2E Tests

Browser-level smoke tests using Playwright 1.53.1 against the Docker stack.

## Prerequisites

- Docker stack running (`docker compose up -d` from project root)
- Playwright browsers installed: `npx playwright install chromium`

## Environment Variables

| Variable | Required | Description |
| -------- | -------- | ----------- |
| `WEB_PORT` | Yes | Port where nginx serves the Angular SPA (e.g. `4200`) |
| `API_PORT` | Yes | Port where the .NET API is exposed (e.g. `3000`) |
| `CI` | No | Set to `true` in CI pipelines (enables retries, single worker) |

All required variables are validated with Zod at config load time — missing or
non-numeric values produce a clear error message instead of a silent failure.

## Running

```bash
cd e2e

# Run all tests
WEB_PORT=4200 API_PORT=3000 npx playwright test

# Run with UI mode
WEB_PORT=4200 API_PORT=3000 npx playwright test --ui

# Run a specific test file
WEB_PORT=4200 API_PORT=3000 npx playwright test src/tests/health-semaphore.spec.ts
```

## Structure

```text
e2e/
├── playwright.config.ts          # Zod-validated config
├── src/
│   ├── constants/
│   │   └── selectors.constants.ts  # Centralized DOM selectors
│   ├── fixtures/
│   │   └── health.fixture.ts       # Test fixtures (page object injection)
│   ├── pages/
│   │   └── health.page.ts          # Page Object for health semaphore
│   └── tests/
│       └── health-semaphore.spec.ts # Smoke tests
└── tsconfig.json
```

## Test Coverage

| Test | Asserts |
| ---- | ------- |
| Health heading | `<h1>TaskFlow</h1>` is visible |
| Status indicator | Semaphore has a valid `data-status` attribute |
| Status details | Status message text is visible |

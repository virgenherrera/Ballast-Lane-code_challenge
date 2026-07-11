# TaskFlow — Task Management System

A full-stack task management application built with ASP.NET Web API, Angular, and PostgreSQL,
following Clean Architecture principles and Test-Driven Development.

## Quick Start

```bash
# Clone and run — requires only Docker
git clone <repo-url>
cd TaskFlow
docker compose up
```

| Service | URL | Description |
| ------- | --- | ----------- |
| Frontend | `http://localhost:${WEB_PORT}` | Angular SPA (default: 4200) |
| API | `http://localhost:${API_PORT}` | ASP.NET Web API (default: 5000) |
| API Health | `http://localhost:${API_PORT}/health` | Liveness + DB connectivity check |
| PostgreSQL | — | Database (internal to Docker network, not exposed to host) |

> Ports are configured via `API_PORT` and `WEB_PORT` in the `.env` file. See `.env.example` for defaults.

## Demo Credentials

| Email | Password | Notes |
| ----- | -------- | ----- |
| `demo@taskflow.dev` | `Demo1234!` | Pre-seeded account with sample tasks |

## Version Manifest

Single source of truth for every pinned dependency. All other documents link here instead of
hardcoding versions. Updated as dependencies are added during implementation.

### Runtime

| Dependency | Version | Role |
| ---------- | ------- | ---- |
| .NET | 10.0 (LTS) | Backend SDK and runtime |
| ASP.NET Core | 10.0 | Web API framework |
| PostgreSQL | 17.5 | Database engine (Docker image: `postgres:17.5`) |
| Angular | 22.0.6 | Frontend SPA framework |
| Node.js | 22.x (LTS) | Frontend build toolchain |
| pnpm | 11.11.0 | Frontend package manager |
| nginx | 1.27 | Frontend static file server (Docker) |

### Backend Libraries (NuGet)

| Package | Version | Role |
| ------- | ------- | ---- |
| Microsoft.EntityFrameworkCore | 10.0.9 *(Batch 2)* | ORM — LINQ-only data access (no raw SQL) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 *(Batch 2)* | EF Core PostgreSQL provider |
| Npgsql | 10.0.3 | PostgreSQL driver (used by EF Core provider) |
| AspNetCore.HealthChecks.NpgSql | 9.0.0 | `/health` DB connectivity probe |
| FluentValidation | — | Request DTO validation |
| Microsoft.AspNetCore.OpenApi | 10.0.9 | OpenAPI spec generation |

### Backend Test Libraries (NuGet)

| Package | Version | Role |
| ------- | ------- | ---- |
| xUnit | — | Test framework |
| NSubstitute | — | Mocking library (unit tests) |
| Testcontainers.PostgreSql | — | PostgreSQL container for integration tests |
| Respawn | — | Database reset between integration tests |
| Microsoft.AspNetCore.Mvc.Testing | built-in | `WebApplicationFactory` for API tests |

### Frontend Libraries (npm)

| Package | Version | Role |
| ------- | ------- | ---- |
| TypeScript | 6.0.3 | Type-safe frontend development |
| Zod | 4.4.3 | API response validation (contract enforcement) |
| Tailwind CSS | — | Utility-first CSS framework |

### Frontend Test Libraries (npm)

| Package | Version | Role |
| ------- | ------- | ---- |
| Playwright | 1.53.1 | E2E browser tests |
| Vitest | 4.1.10 | Unit test runner (Angular component tests) |

### Infrastructure

| Tool | Version | Role |
| ---- | ------- | ---- |
| Docker Engine | 27.x+ | Container runtime |
| Docker Compose | v2 | Multi-container orchestration |
| ESLint | — | Linting (FE) |
| Prettier | 3.9.5 | Formatting (FE) |

> **`—`** = exact version will be pinned during implementation and updated here.
> No `^`, no `~`, no `latest`. See [Tech Stack — Decision 8](docs/architecture/tech-stack.md#decision-8-dependency-version-pinning).

## Tech Stack

For rationale behind each technology choice, see [Tech Stack decisions](docs/architecture/tech-stack.md).
All versions are pinned in the [Version Manifest](#version-manifest) above.

## Architecture

Clean Architecture with four layers — Domain at the center, Infrastructure at the edges.

```text
TaskFlow.sln
├── src/
│   ├── TaskFlow.Domain/           # Entities, interfaces, exceptions
│   ├── TaskFlow.Application/      # Use cases, DTOs, validation
│   ├── TaskFlow.Infrastructure/   # EF Core, JWT, repositories
│   └── TaskFlow.API/              # Controllers, middleware
├── tests/
│   ├── TaskFlow.Domain.Tests/     # Unit tests (domain logic)
│   ├── TaskFlow.Application.Tests/# Unit tests (use cases, mocked repos)
│   └── TaskFlow.IntegrationTests/ # API-level integration tests
└── e2e/                           # Playwright E2E tests
```

Full architecture documentation: [docs/architecture/](docs/architecture/)

## User Story

> As a user, I want to manage my personal tasks — create them, track their status, update
> details, and remove completed ones — through a simple web interface that requires me to
> log in so my tasks stay private.

Detailed user stories and acceptance criteria: [docs/INDEX.md](docs/INDEX.md)

## Thought Process

<!-- TODO: Fill during/after implementation -->

### Approach

1. **Discovery first** — decomposed the challenge into epics and user stories before writing
   any code. Every acceptance criterion traces back to a PDF requirement.
2. **Architecture before implementation** — documented Clean Architecture layers, API contract,
   and testing strategy as a blueprint for AI-assisted development.
3. **Tests as guardrails** — TDD at the integration level ensures the full pipeline works.
   Unit tests cover Domain invariants and Application logic in isolation.
4. **Docker for reproducibility** — same PostgreSQL, same pipeline, same artifacts in every
   environment. `docker compose up` is the only command the evaluator needs.

### Key Decisions

| Decision | Rationale |
| -------- | --------- |
| PostgreSQL over SQLite | Same engine in dev/test/demo — no fidelity mismatch |
| Integration tests as primary layer | For CRUD apps, full-pipeline tests catch more real bugs than isolated unit tests |
| Angular over React | Candidate expertise + existing production-ready base project |
| Exact version pinning | Reproducible builds regardless of when the repo is cloned |

### GenAI Usage

See [docs/epics/EP03-genai-documentation.md](docs/epics/EP03-genai-documentation.md) for
the full GenAI process documentation, including prompts, outputs, and validation.

## API Reference

| Method | Path | Auth | Description |
| ------ | ---- | ---- | ----------- |
| GET | `/health` | Public | Liveness + DB connectivity check |
| POST | `/api/auth/register` | Public | Register a new user |
| POST | `/api/auth/login` | Public | Log in, receive JWT |
| GET | `/api/auth/me` | Bearer | Current user profile |
| POST | `/api/tasks` | Bearer | Create a task |
| GET | `/api/tasks` | Bearer | List tasks (paginated, optional `?status=` filter) |
| GET | `/api/tasks/{id}` | Bearer | View task detail |
| PATCH | `/api/tasks/{id}` | Bearer | Update a task |
| DELETE | `/api/tasks/{id}` | Bearer | Delete a task |

Full contract: [docs/architecture/api-contract.md](docs/architecture/api-contract.md)

## License

This project was created as a technical interview exercise for Ballast Lane Applications.

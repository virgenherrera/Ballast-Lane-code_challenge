# AGENTS.md — TaskFlow Project

## Project Identity

**Name**: TaskFlow
**Type**: Full-stack web application (technical interview exercise)
**Domain**: Task Management System
**Constraint**: Single public GitHub repository, all deliverables unified
**Deadline**: 2026-07-13 at 11:00 CDT (Mexico time)

## Deliverables

- [ ] Backend API with Clean Architecture and TDD
- [ ] Frontend with CRUD operations
- [ ] GenAI process documentation (mandatory)
- [ ] README with setup instructions and thought process
- [ ] Seeded data and credentials for demo

## Repository Rules

- Single repo for all deliverables
- Public repository on GitHub
- No further work allowed after submission

## Project Phases

```mermaid
%% Project lifecycle — each phase gates the next
flowchart LR
    A([Discovery]) --> B([Architecture])
    B --> C([Implementation])
    C --> D([Verification])
    D --> E([Submission])

    style A fill:#22c55e,color:#fff
    style B fill:#94a3b8,color:#fff
    style C fill:#94a3b8,color:#fff
    style D fill:#94a3b8,color:#fff
    style E fill:#94a3b8,color:#fff
```

## Current Phase

**Architecture** — Technology decisions, layer design, and API contract documentation. Still no code.

## Conventions

### Documentation
- All planning documents are technology-agnostic until architecture phase
- User stories follow standard format: persona, action, value
- Acceptance criteria use Given/When/Then format
- Language: English for all artifacts in the repository

### Git Branching
- **No direct commits to `main`** — all work goes through branches
- Feature branches: `feature/<epic-or-scope>` (e.g., `feature/EP00-infrastructure`)
- Task branches (optional): `feature/<scope>/<task>` — branched from the feature branch
- Worktrees may be used for parallel task branches off the same feature branch
- When a feature branch is complete, **squash merge** into `main`
- Delete the feature branch after merge

### Git Commits
- Conventional commits: `type(scope): description`
- Types: `docs`, `feat`, `fix`, `refactor`, `test`, `chore`
- Atomic commits — one logical change per commit
- Commits on feature/task branches can be granular — the squash merge into `main` consolidates them

### Architecture (when phase begins)
- Clean Architecture: domain at the center, infrastructure at the edges
- Dependency rule: inner layers never depend on outer layers
- TDD: tests first, implementation second

## Agent Roles

### Orchestrator (Senku / main thread)
- **DOES NOT write code** — zero lines, no exceptions
- Orchestrates sub-agents for all implementation, testing, and code review
- Validates sub-agent outputs against specs and acceptance criteria
- Makes architecture and design decisions
- Writes and maintains documentation (epics, user stories, design docs)
- Manages project phases and transitions

### Sub-Agents (delegated workers)
- Receive specific, scoped tasks from the orchestrator
- Write code, run tests, perform builds
- Return results for orchestrator validation
- Follow compact rules injected by the orchestrator

### Delegation Contract (mandatory for every sub-agent launch)
Every sub-agent MUST receive before executing:
1. **Limits** — what is OUT of scope, what NOT to touch, where to stop
2. **Expected Result** — what the output looks like, format, acceptance criteria
3. **Compact Rules** — injected project standards from this file
4. **Status Protocol** — the mandatory status block format (see below)

No sub-agent launches without all four. No exceptions.

```mermaid
%% Orchestrator never touches code — only validates
flowchart TD
    O((Orchestrator)) --> D[Design & Decide]
    O --> V[Validate Outputs]
    O --> S1[Sub-Agent: Implement]
    O --> S2[Sub-Agent: Test]
    O --> S3[Sub-Agent: Review]
    S1 --> V
    S2 --> V
    S3 --> V
```

## Model Assignment

| Level | Model | Use When |
| ----- | ----- | -------- |
| Search | haiku | Grep, read docs, lint checks, exploratory reads |
| Implement | sonnet | Write code, tests, reviews, verify quality gates |
| Architect | opus | Design decisions, conflict resolution, multi-source synthesis |

With 6+ agents, model discipline multiplies savings. Never burn opus on a grep.

## Agent Naming and Personality

Every sub-agent receives a **name** and **expertise persona** based on the task:

| Task Type | Name Pattern | Expertise |
| --------- | ------------ | --------- |
| Schema/Types | Matt Pocock — TypeScript Educator | Zod schemas, type inference |
| Unit/Integration Tests | Kent C. Dodds — Testing Library Creator | Test architecture, AAA pattern |
| E2E Tests | Debbie O'Brien — Playwright Team PM | Playwright, POM patterns |
| API/Backend | Uncle Bob — Clean Architecture Author | SOLID, Clean Architecture, DI |
| Frontend | Sarah Drasner — VP of DX | Component design, state, a11y |
| DevOps/Docker | Kelsey Hightower — Cloud Native Pioneer | Containers, CI/CD, infra |
| Docs/Tech Writing | Daniele Procida — Diátaxis Creator | Documentation structure, clarity |

The persona sets the sub-agent's communication style and technical lens. It does NOT
override compact rules or project conventions.

## Sub-Agent Status Protocol

### Mandatory Status Block

Every sub-agent MUST include this block in its final response:

```text
Status: [IN_PROGRESS | BLOCKED | DONE | FAILED]
Progress: X/Y items
Blocker: (if applicable — describe exactly what blocks)
```

### Anti-Stall Rules

| Condition | Action |
| --------- | ------ |
| No status block returned | STALLED → kill + relaunch |
| BLOCKED > 1 iteration | Kill → reassign with blocker context |
| FAILED | Diagnose root cause before relaunch |
| Same gate fails 3 times | Reduce scope and relaunch |

```mermaid
%% Circuit breaker for sub-agent health
flowchart LR
    CLOSED([Closed — Normal]) -->|Gate fails| OPEN([Open — Anomaly])
    OPEN -->|Relaunch with reinforced prompt| HALF([Semi-Open — Recovery])
    HALF -->|Passes| CLOSED
    HALF -->|Fails again| OPEN
    
    style CLOSED fill:#22c55e,color:#fff
    style OPEN fill:#ef4444,color:#fff
    style HALF fill:#f59e0b,color:#fff
```

### Escalation by Rejection

1. Gate fails → specific feedback with evidence → agent corrects
2. Same gate fails again → kill + clean relaunch
3. Third failure → diagnose root cause, relaunch with reduced scope

## Post-Delegation Checkpoint (PDC)

After EVERY sub-agent returns, the orchestrator runs these 4 steps **sequentially**:

1. **ECHO** — Print the gates from the delegation: `GATES: [gate1] | [gate2] | [gate3]`
2. **VERIFY** — For each gate: `GATE [name]: PASS|FAIL — [evidence]`
3. **MARK** — Update progress tracker NOW (checkbox + inline evidence)
4. **DECIDE** — Any FAIL → no advance. All PASS → `CHECKPOINT CLEAR`, proceed.

If step 3 is not completed, the orchestrator **CANNOT** launch another sub-agent.

```mermaid
%% PDC — mandatory after every sub-agent return
flowchart TD
    RET([Sub-Agent Returns]) --> ECHO["1. ECHO gates"]
    ECHO --> VER["2. VERIFY each gate"]
    VER --> MARK["3. MARK progress tracker"]
    MARK --> DEC{4. DECIDE}
    DEC -->|All PASS| CLEAR([CHECKPOINT CLEAR — proceed])
    DEC -->|Any FAIL| BLOCK([BLOCKED — fix before next launch])
```

## Compact Rules for Sub-Agent Injection

### TASKFLOW-DOCS
- All planning docs are business-first, technology-agnostic
- User stories must have acceptance criteria in Given/When/Then
- No implementation details in user stories

### TASKFLOW-TEST-HARNESS
- Integration tests and E2E tests serve as the project's safety harness
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue — fix before proceeding
- Backend: integration tests at API level (AAA pattern: Arrange/Act/Assert)
- Frontend: Playwright E2E regression tests
- Unit tests cover Domain invariants and Application use cases in isolation (mocked repos). Integration tests at API level remain the PRIMARY confidence layer. Both must pass.
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not jump ahead
- Discovery phase: NO code, NO technology choices, NO architecture diagrams
- Every decision must trace back to a requirement or acceptance criterion
- Version pinning: ALL dependencies use exact versions, never floating (no ^, no ~, no latest)

### TASKFLOW-BUILD-PIPELINE
- Build pipeline has 5 sequential gated stages: setUp → build → test:static → test:dynamic → test:e2e
- Each stage gates the next — a failing stage STOPS the pipeline, no skipping allowed
- All build outputs go to `./artifacts/` (gitignored): dist/api, dist/web, testReports/api|e2e, openApi/
- E2E tests consume artifacts from BOTH api and web builds — they do NOT build anything themselves
- Same pipeline in every environment: local, CI, Docker — no environment-specific shortcuts
- PostgreSQL is the ONLY database engine — no EF Core InMemory, no SQLite, no in-memory substitutes
- Docker Compose topology: 3 containers (postgres:17.5, taskflow-api, taskflow-web) — all from pinned images
- Env vars come from `.env` file, validated at startup — fail-fast with named error on missing vars
- `.env.example` committed with placeholders, `.env` gitignored
- All dependency versions pinned in [README — Version Manifest](README.md#version-manifest) — single source of truth

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

**Discovery** — Business analysis and requirements documentation only. No code, no technology decisions.

## Conventions

### Documentation
- All planning documents are technology-agnostic until architecture phase
- User stories follow standard format: persona, action, value
- Acceptance criteria use Given/When/Then format
- Language: English for all artifacts in the repository

### Git
- Conventional commits: `type(scope): description`
- Types: `docs`, `feat`, `fix`, `refactor`, `test`, `chore`
- Atomic commits — one logical change per commit

### Architecture (when phase begins)
- Clean Architecture: domain at the center, infrastructure at the edges
- Dependency rule: inner layers never depend on outer layers
- TDD: tests first, implementation second

## Compact Rules for Sub-Agent Injection

### TASKFLOW-DOCS
- All planning docs are business-first, technology-agnostic
- User stories must have acceptance criteria in Given/When/Then
- No implementation details in user stories

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not jump ahead
- Discovery phase: NO code, NO technology choices, NO architecture diagrams
- Every decision must trace back to a requirement or acceptance criterion

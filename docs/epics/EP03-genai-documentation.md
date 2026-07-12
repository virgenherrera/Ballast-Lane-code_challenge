> [📚 INDEX](../INDEX.md) / [Epics](../INDEX.md#epics) / EP03

# EP03 — GenAI Process Documentation

## Summary

Document the full process of using a Generative AI coding tool to scaffold or implement the task management API. This is not a feature for end users — it is a deliverable for the interview panel demonstrating AI fluency.

**Tool**: Claude Code CLI (Claude Opus model). **Methodology**: vertical-slice development —
each user story was planned in a `docs(...)` commit and implemented in a paired `feat(...)`
commit, with TDD (integration tests written first, against real PostgreSQL via Testcontainers)
as the primary validation gate for every AI-generated change. See
[US-010](../user-stories/US-010-prompt-documentation.md) for prompt examples and
[US-011](../user-stories/US-011-ai-output-validation.md) for the validation report with
concrete before/after corrections.

## Business Value

Demonstrates the candidate's ability to leverage AI tools effectively: knowing what to ask, how to validate output, and when to correct or improve suggestions.

## GenAI Documentation Process

```mermaid
%% Process for documenting GenAI usage
flowchart TD
    A([Define Intent]) --> B[Write Prompt]
    B --> C[Run AI Tool]
    C --> D[Capture Output]
    D --> E{Valid?}
    E -->|Yes| F[Document What Worked]
    E -->|No| G[Identify Issues]
    G --> H[Apply Corrections]
    H --> I[Document Before/After]
    F --> J([Final Report])
    I --> J
```

## Deliverables

- [x] [**US-010** — Prompt Documentation](../user-stories/US-010-prompt-documentation.md) `Must Have`
- [x] [**US-011** — AI Output Validation Report](../user-stories/US-011-ai-output-validation.md) `Must Have`

## Acceptance Boundaries

- Show the exact prompt(s) used to generate code
- Show the AI's output (or representative sample)
- Describe how the output was validated against requirements
- Document what was corrected or improved and why
- Explain how edge cases, authentication, and validations were handled
- Demonstrate critical thinking, not blind acceptance

## Related Architecture

- [Project Brief — Deliverables](../project-brief.md#deliverables) — D3: GenAI Documentation deliverable
- [Testing Strategy](../architecture/testing-strategy.md) — validated output referenced by US-011

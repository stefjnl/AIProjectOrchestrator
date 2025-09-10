# Active Context

## Current Work Focus
Initializing the Memory Bank to establish foundational documentation for the AI Project Orchestrator. This sets the stage for ongoing development, ensuring continuity across sessions despite memory resets.

## Recent Changes
- Created projectbrief.md and productContext.md as core foundational documents.
- No code changes yet; focus on documentation setup.

## Next Steps
- Complete remaining Memory Bank files: systemPatterns.md, techContext.md, progress.md.
- Set up initial project structure if not already present (verify via tools).
- Implement basic API endpoints for requirements input.
- Integrate first AI provider (e.g., Claude) for analysis phase.

## Active Decisions and Considerations
- Adhere strictly to Clean Architecture: Domain first, then Application, Infrastructure, API.
- Use .NET 9 with nullable reference types enabled.
- Prioritize async patterns for all I/O operations.
- Decisions on AI routing: Start with rule-based selection, evolve to ML-based if needed.

## Important Patterns and Preferences
- Dependency Injection via constructor for all services.
- Structured logging with Serilog and correlation IDs.
- DTOs for all external communications.
- TDD approach: Write tests before implementation.

## Learnings and Project Insights
- Memory Bank is critical for session continuity; maintain meticulously.
- Docker setup essential for consistent environments.
- Multi-provider AI integration requires robust abstraction layers to handle varying APIs.

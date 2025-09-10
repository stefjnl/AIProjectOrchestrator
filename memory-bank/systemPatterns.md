# System Patterns

## System Architecture
The AI Project Orchestrator follows Clean Architecture principles with strict layer separation:
- **Domain Layer**: Contains entities, value objects, domain services, and interfaces. No external dependencies.
- **Application Layer**: Orchestrates use cases via services, mediators, and DTOs. Depends on Domain.
- **Infrastructure Layer**: Implements domain interfaces (repositories, external clients). Handles persistence, AI providers, logging.
- **API Layer**: Entry point with controllers, middleware. Depends on Application.

## Key Technical Decisions
- **Dependency Flow**: Inward-only; outer layers depend on inner (API -> Application -> Domain).
- **Async Everywhere**: All I/O-bound operations use async/await with CancellationToken.
- **Dependency Injection**: Built-in .NET DI container with scoped/transient/singleton lifetimes as appropriate.
- **Error Handling**: Custom exceptions in Domain, Result<T> pattern in Application for validation.

## Design Patterns in Use
- **Repository Pattern**: For data access abstraction (e.g., IRepository<T> implementations in Infrastructure).
- **Factory Pattern**: For service instantiation (e.g., ILLMServiceFactory for model selection).
- **CQRS**: Separated commands (e.g., CreateWorkflow) and queries (e.g., GetWorkflowStatus) where complexity warrants.
- **Unit of Work**: Managed via DbContext for transaction boundaries.
- **Decorator Pattern**: For cross-cutting concerns like logging, caching on services.
- **Strategy Pattern**: For AI provider routing based on task type.

## Component Relationships
- **Workflow Orchestrator**: Central service in Application layer coordinates phases, injecting phase-specific services (AnalysisService, PlanningService).
- **AI Clients**: Infrastructure abstractions (IModelClient) implemented per provider (ClaudeClient, LMStudioClient), routed via factory.
- **Persistence**: EF Core DbContext in Infrastructure, repositories implement domain interfaces.
- **External Integrations**: HTTP clients for AI APIs, configured with Polly for resilience (retries, circuit breakers).

## Critical Implementation Paths
- **Workflow Initiation**: API Controller -> Application Service -> Domain Validation -> Infrastructure Persistence -> AI Routing.
- **Phase Execution**: Sequential or parallel invocation of phase services, with state transitions tracked in database.
- **Human Approval**: Workflow pauses at gates, notifications via SignalR or email, resume via API.
- **Audit Trail**: All interactions logged with correlation IDs, stored in dedicated tables.

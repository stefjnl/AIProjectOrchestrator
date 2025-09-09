### AI Project Orchestrator:

## Architecture Overview
The system follows Clean Architecture principles with proper separation across Domain, Application, Infrastructure, and API layers. It implements a 6-stage AI-driven workflow pipeline: Project → Requirements → Planning → Stories → Prompts → Code, with human review checkpoints at each stage.

**## Project Overview**
**AI Project Orchestrator** - A .NET 9 Web API that orchestrates AI coding workflows, automatically routing requirements through analysis, planning, story generation, and implementation phases using multiple AI models (Claude, LM Studio, OpenRouter).

**## Build, Lint, and Test Commands**

**### Docker-First Development**
```bash
# Start development environment
docker-compose up -d
docker-compose logs -f api

# Run tests in containerized environment
docker-compose exec api dotnet test
```

**### Build**
- Restore dependencies: `dotnet restore`
- Build project: `dotnet build --no-restore`
- Build in Docker: `docker-compose build`

**### Lint/Format**
- Apply code formatting: `dotnet format`
- Check formatting: `dotnet format --verify-no-changes`

**### Test**
- Run all tests: `dotnet test --no-build --verbosity normal`
- Run single test: `dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName" --no-build --verbosity normal`
- Run tests in specific project: `dotnet test tests/AIProjectOrchestrator.UnitTests/AIProjectOrchestrator.UnitTests.csproj`

**### Database Operations**
```bash
# Add migrations with descriptive names
dotnet ef migrations add AddWorkflowStageTracking --project src/AIProjectOrchestrator.Infrastructure --startup-project src/AIProjectOrchestrator.API

# Apply migrations in Docker
docker-compose exec api dotnet ef database update
```

**## Architecture & Code Style Guidelines**

**### Clean Architecture Rules**
- **Domain layer**: No external dependencies, pure business logic
- **Application layer**: Domain interfaces, use cases, DTOs
- **Infrastructure layer**: External concerns (DB, APIs, files)
- **API layer**: Controllers, middleware, dependency injection setup

**### Project Structure**
```
AIProjectOrchestrator/
├── src/
│   ├── AIProjectOrchestrator.API/           # Web API layer
│   ├── AIProjectOrchestrator.Application/   # Business logic, services
│   ├── AIProjectOrchestrator.Domain/        # Domain entities, interfaces
│   └── AIProjectOrchestrator.Infrastructure/ # Data access, external APIs
├── tests/
├── docs/                                    # Vision, user stories, architecture
└── Instructions/                            # Sub-agent instruction files
```

**### Naming Conventions**
- **Services**: End with "Service" (RequirementsAnalysisService)
- **Interfaces**: Start with "I" and match implementation (IModelClient)
- **DTOs**: Descriptive names with "Request"/"Response" suffixes
- **Entities**: Clear domain names without technical suffixes
- **Classes**: Use file-scoped namespaces and primary constructors where possible
- **Methods/Properties**: PascalCase (e.g., `GetProjectsAsync()`)
- **Variables/Parameters**: camelCase (e.g., `projectId`)

**### Code Quality Guidelines**
- **Single Responsibility**: Classes should have one clear purpose (<15 methods)
- **Explicit Interface Segregation**: Create focused interfaces for each concern
- **Dependency Injection**: Constructor injection with proper lifetime management
- **Async/Await**: Use consistently for I/O operations with CancellationToken support

**### Imports and Using Statements**
- Place `using` statements at file top, sorted alphabetically
- Use file-scoped namespaces: `namespace AIProjectOrchestrator.Application;`
- Remove unused imports

**### Formatting**
- Indentation: 4 spaces
- Braces: On new lines for blocks
- Line length: Aim for < 120 characters
- Use `dotnet format` for auto-formatting

**### Types and Nullability**
- Enable nullable reference types (`<Nullable>enable</Nullable>` in csproj)
- Annotate nullable params/returns explicitly (e.g., `string?`)
- Prefer immutable types (e.g., records for DTOs)

**### Error Handling**
- **Structured Exceptions**: Custom exceptions in `Domain/Exceptions/`
- **Proper Logging**: Structured logging with Serilog and correlation IDs
- **API Layer**: Handle exceptions in controllers with ProblemDetails
- **Use Result<T> Pattern**: For business logic errors where appropriate

**### Async/Await**
- Mark I/O-bound methods as `async Task<T>`
- Use `ConfigureAwait(false)` in non-UI code
- Always include CancellationToken parameters for long-running operations
- Avoid async void except for event handlers

**## AI Integration Patterns**

**### Multi-Provider Model Client**
- Create abstraction layer for different AI providers
- Support dynamic model routing based on task type
- Implement circuit breaker and retry patterns
- Handle rate limiting and quota management

**### Sub-Agent Instruction System**
- Load instruction files from `/Instructions/` directory
- Support hot-reloading of instruction changes
- Template system for dynamic prompt generation
- Context window optimization (<40% utilization)

**### Configuration Management**
- Use strongly-typed configuration classes with IOptions<T>
- Separate configuration for each AI provider
- Environment-specific settings (Development, Production)
- Secret management for API keys (never in appsettings.json)

**## Development Workflow**

**### Creating New Services**
1. Define interface in Domain layer
2. Implement in Application layer with constructor DI
3. Register in Infrastructure DI container
4. Add unit tests with mocked dependencies
5. Create integration tests if external dependencies involved

**### AI Model Integration**
1. Create provider-specific client implementation
2. Implement common interface (IModelClient)
3. Add configuration section and validation
4. Implement circuit breaker and retry logic
5. Add comprehensive error handling and logging

**### Database Changes**
1. Update domain entities first
2. Modify DbContext and configurations
3. Create and test migration
4. Update repository implementations
5. Adjust DTOs and mapping logic

**## Testing Strategy**
- **Unit Tests**: Business logic in Application layer
- **Integration Tests**: API endpoints with test database
- **Contract Tests**: AI model client interfaces
- **TDD Approach**: Write tests first, especially for complex workflows
- **Use xUnit with Moq**: Follow AAA pattern (Arrange, Act, Assert)

**## Technology Stack**
- **.NET 9**: Latest framework with performance optimizations
- **Docker & Docker Compose**: Containerized development with PostgreSQL
- **Entity Framework Core**: Code-first approach with migrations
- **Clean Architecture**: Strict layer separation and dependency rules
- **xUnit**: Unit and integration testing
- **Serilog**: Structured logging with correlation IDs

**## Performance & Security**
- **AI Model Efficiency**: Batch requests, intelligent caching, optimize context window usage
- **Application Performance**: Use async/await consistently, implement connection pooling
- **API Security**: Proper authentication/authorization, input validation, rate limiting
- **AI-Specific Security**: Sanitize prompts, implement output validation, secure API key storage

**## Key Principles**
- Enterprise-grade system design for maintainability
- Prioritize clarity and testability over clever solutions
- Every component should be easily replaceable and well-documented
- Docker-first development environment
- Comprehensive logging and observability

## Key Strengths
- **Solid Architecture**: Clean separation of concerns, dependency injection, and proper abstractions
- **Multi-Provider AI Integration**: Flexible IAIClient interface supporting Claude, LM Studio, and OpenRouter with retry logic and fallback strategies
- **Comprehensive Workflow**: Well-structured pipeline with prerequisite validation and status tracking
- **Robust Data Layer**: Entity Framework Core with PostgreSQL, proper relationships, and performance indexes
- **Testing Foundation**: Integration tests with custom WebApplicationFactory and unit tests with mocking

## Technical Assessment
- **Domain Layer**: Well-structured entities with proper navigation properties and status enums
- **Application Layer**: Clear service interfaces with focused responsibilities and async patterns
- **Infrastructure Layer**: Repository pattern implementation with specialized repositories and EF Core integration
- **API Layer**: RESTful design with proper HTTP status codes and consistent error formatting
- **Frontend**: Static HTML/CSS/JS with modular architecture and comprehensive API client
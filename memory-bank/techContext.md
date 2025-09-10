# Tech Context

## Technologies Used
- **Framework**: .NET 9 for the core application, leveraging modern features like primary constructors and nullable reference types.
- **Database**: PostgreSQL with Entity Framework Core (EF Core) for ORM and migrations.
- **AI Integration**: HTTP clients for Claude API, LM Studio (local), OpenRouter; abstractions via IModelClient.
- **Logging**: Serilog for structured logging with sinks for console, file, and Seq.
- **Resilience**: Polly for retry, circuit breaker, and timeout policies on external calls.
- **Testing**: xUnit for unit/integration tests, FluentAssertions for assertions, Moq for mocking.
- **Containerization**: Docker and Docker Compose for development and deployment.
- **Other**: MediatR for CQRS mediation, AutoMapper for DTO mappings (if needed), Swagger for API documentation.

## Development Setup
- **IDE**: Visual Studio Code with C# extension pack.
- **Shell**: PowerShell 7 on Windows 11.
- **Working Directory**: c:/git/AIProjectOrchestrator.
- **Solution Structure**: Multi-project solution (Domain, Application, Infrastructure, API, Tests).
- **Build/Run**: `dotnet build`, `dotnet run --project src/AIProjectOrchestrator.API`; Docker: `docker-compose up`.
- **Database Migrations**: `dotnet ef migrations add` and `dotnet ef database update` via CLI or Docker exec.

## Technical Constraints
- **OS**: Windows 11; ensure cross-platform compatibility for Docker.
- **.NET Version**: Target .NET 9; no lower framework support.
- **Database**: PostgreSQL only; no SQL Server migration planned.
- **AI Providers**: Dependent on external APIs; handle rate limits and offline fallbacks.
- **Performance**: Async I/O required; no blocking calls in services.
- **Security**: No hardcoded secrets; use environment variables and Azure Key Vault in production.

## Dependencies
- **NuGet Packages** (managed via .csproj and Directory.Build.props):
  - Microsoft.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL
  - Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File
  - Polly, Polly.Extensions.Http
  - MediatR, MediatR.Extensions.Microsoft.DependencyInjection
  - xunit, FluentAssertions, Moq
  - Swashbuckle.AspNetCore for Swagger
- **External Services**: Claude API, LM Studio (local server), OpenRouter API.
- **Version Consistency**: Pin exact versions in Directory.Build.props for stability.

## Tool Usage Patterns
- **CLI Commands**: Prefer `dotnet` for build/test/migrate; `docker-compose` for env setup.
- **Git**: Branching for features, commits with conventional messages.
- **Testing**: Run `dotnet test` after changes; integration tests use TestServer or Docker.
- **Debugging**: Use VSCode debugger; log correlation IDs for tracing.
- **Documentation**: Update Memory Bank after significant changes; use Swagger for API exploration.

# GEMINI Project Overview: AIProjectOrchestrator

## Project Overview

This project is a .NET 9 Web API that orchestrates AI coding workflows. It follows the principles of Clean Architecture (Onion Architecture) and utilizes a vanilla JavaScript frontend. The backend is designed to interact with multiple AI providers, including Claude, LMStudio, and OpenRouter. The overall goal of the project is to automate software development tasks by breaking them down into user stories, generating prompts, and producing code.

The project is structured into the following layers:

*   **`AIProjectOrchestrator.API`**: The presentation layer, which exposes the Web API endpoints.
*   **`AIProjectOrchestrator.Application`**: The application layer, which contains the business logic and services.
*   **`AIProjectOrchestrator.Domain`**: The domain layer, which defines the domain entities and interfaces.
*   **`AIProjectOrchestrator.Infrastructure`**: The infrastructure layer, which handles data access and interactions with external services like AI providers.

The frontend is located in the `frontend` directory and consists of HTML, CSS, and JavaScript files.

## Building and Running

### With Docker

To build and run the application using Docker, run the following command from the project root:

```bash
docker-compose up --build
```

### Without Docker

To run the application locally without Docker, you need the .NET 9 SDK installed.

1.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```

2.  **Run the application:**
    ```bash
    dotnet run --project src/AIProjectOrchestrator.API
    ```

### Running Tests

To run the unit and integration tests, use the following command:

```bash
dotnet test
```

## Development Conventions

*   **Architecture**: The project follows the Clean Architecture (Onion Architecture) pattern, separating concerns into `Domain`, `Application`, `Infrastructure`, and `API` projects.
*   **Logging**: Serilog is used for logging and is configured to write to the console.
*   **Database**: The project uses Entity Framework Core with a PostgreSQL database. Migrations are applied automatically on startup in the development environment.
*   **API Clients**: `HttpClientFactory` is used to create clients for interacting with the different AI providers.
*   **Health Checks**: The application includes health checks for its dependencies, which are exposed at the `/api/health` endpoint.
*   **Frontend**: The frontend is built with vanilla JavaScript, HTML, and CSS. It communicates with the backend API to drive the AI orchestration workflow.


Always follow the following best practices when coding:

1.  **Prioritize Simplicity and Readability**: Reduce ambiguities, clarify processes, and use descriptive names for classes, methods, and variables to make code self-documenting.
2.  **Adhere to SOLID Principles**: Respect Single Responsibility, Open/Close, Liskov Substitution, Interface Segregation, and Dependency Inversion principles to organize functions and data structures effectively.
3.  **Ensure High Maintainability**: Refactor code to improve its maintainability index (a score from 0 to 100, where higher indicates easier maintenance), focusing on writing classes and methods with single responsibilities.
4.  **Limit Cyclomatic Complexity**: Keep the cyclomatic complexity of methods below 10. Higher numbers indicate code that is difficult to read and maintain, suggesting that such methods should be refactored into separate methods or classes.
5.  **Achieve High Cohesion and Low Coupling**: Design solutions where classes are not closely and directly connected (low coupling) and where each class has well-related methods and data (high cohesion). Prefer composition or aggregation over inheritance if appropriate to reduce dependencies and tight coupling.
6.  **Avoid Duplicate Code**: Ensure there is no reason for having duplicate code, and centralize reusable logic where possible.

8.  **Implement Robust Exception Handling**: Avoid empty `try-catch` blocks, as they hide unexpected behavior. Connect `try-catch` statements to logging solutions, and catch specific exceptions whenever possible rather than general ones to prevent hiding unexpected errors and application crashes.
9.  **Properly Manage Resources with `using` and `IDisposable`**: Always use the `using` statement (or `try-finally`) to correctly create and destroy I/O objects (like `StreamWriter`) and all other disposable objects to prevent memory leaks and ensure resources are freed gracefully. Implement the `IDisposable` pattern for classes that deal with and create objects requiring explicit disposal.
10. **Manage Object Allocation Carefully**: Avoid allocating large objects and be cautious with event handling and weak references to minimize the Garbage Collector's impact on performance and prevent memory issues.
11. **Utilize Dependency Injection (DI)**: Employ DI for cleaner code. With DI, you generally only need to worry about disposing of objects you explicitly create, not those injected. DI also facilitates injecting `ILogger` for debugging exceptions. Choose appropriate service lifetimes (Singleton, Scoped, Transient) based on business rules and object state.
12. **Perform Null Checking and Use Nullable Reference Types**: Always check objects for null before usage. C# 8's nullable reference types can help prevent related errors.
13. **Use Constants and Enumerators**: Replace "magic numbers" and literal text with constants and enumerators for better readability and maintainability.
14. **Comment Public Methods Thoroughly**: Provide comments for public methods to explain their correct external usage, especially since they are used outside your library.
15. **Include Default Treatment in `switch-case` Statements**: Include a `default` case in `switch-case` statements to handle unknown input variables and prevent code breaks.

17. **Don't Repeat Yourself (DRY Principle)**: Follow the DRY principle by analyzing your code and selecting parts that can be decoupled and reused, rather than copying and pasting code. Centralize common logic to ensure consistency and easier maintenance.
18. **Leverage Object-Oriented Analysis (OOA) Principles for Reuse**: Utilize C# capabilities like inheritance, abstraction, encapsulation, and polymorphism to build highly reusable code components.
19. **Utilize Generics for Flexible Code Reuse**: Implement generics in interfaces, classes, methods, or delegates to define placeholders for specific types, maximizing type safety, performance, and code reuse with different data types.
20. **Proactively Refactor Code**: Refactor code that is not well-tested, duplicated, too complex to understand, or tightly coupled. Refactoring improves software design, makes code easier to understand, helps find bugs, and cleans up the design, while ensuring the original behavior is preserved.

22. **Adopt Domain-Driven Design (DDD)**: Organize applications into **Bounded Contexts** to cope with complex software systems by defining distinct domain models and ubiquitous languages for different parts of an organization, which helps manage complexity and reduce discrepancies.
23. **Use Value Objects and Aggregates in DDD**: Employ Value Objects for data with no unique identities, and Aggregates to represent complex entities and their related objects as single units, with a unique aggregate root handling all operations on the aggregate.
24. **Implement Domain Events for Inter-Context Communication**: Use **Domain Events**, often via the Publisher/Subscriber pattern, for communication among Bounded Contexts. This approach maximizes independence between contexts by allowing publishers to broadcast information without needing to know specific subscribers.
25. **Apply Command Query Responsibility Segregation (CQRS)**: When data storage/update requirements differ significantly from query requirements, use the CQRS pattern. This involves using different structures for storing/updating and querying data, which can lead to more efficient and specialized solutions.

## Clean Architecture Layer Responsibilities

### 🏛️ **Domain Layer**
**What:** Core business rules and entities
- **Entities** - Business objects with identity
- **Value Objects** - Immutable business concepts  
- **Domain Services** - Business logic that doesn't belong to a single entity
- **Interfaces** - Contracts for external dependencies
- **Business Rules** - Validation, calculations, domain logic

**Example:** `ProviderConfiguration.IsValid()`, `User`, `OrderTotal.Calculate()`

### 🔧 **Application Layer** 
**What:** Use cases and orchestration
- **Services** - Coordinate between domain and infrastructure
- **Use Cases** - Application-specific business flows
- **DTOs** - Data transfer objects for API contracts
- **Orchestration** - Combine multiple domain services
- **Transaction Management** - Cross-service coordination

**Example:** `CreateUserService`, `ProcessOrderUseCase`, `ProviderManagementService`

### 🌐 **Infrastructure Layer**
**What:** External concerns and implementation details
- **Repositories** - Data access implementations
- **External APIs** - Third-party service clients
- **File System** - File operations, logging
- **Frameworks** - Entity Framework, HTTP clients
- **Configuration** - Settings, connection strings

**Example:** `SqlUserRepository`, `EmailService`, `ClaudeApiClient`

### 🚫 **Key Rule**
- **Domain** = No dependencies on anything
- **Application** = Depends only on Domain
- **Infrastructure** = Implements Domain interfaces, no business logic

## Development Philosophy

**Minimalist Approach**: 15-20 minute implementations, 2-4 files maximum
**Ship Fast**: Build functional solution, avoid architectural gold-plating
**Critical Evaluation**: Question every abstraction and interface
**Business Focus**: Solve real problems, not theoretical ones

### Recent Success Pattern
- **Problem**: Provider switching required config file edits
- **Solution**: Single override point in `ConfigurableAIProvider` with singleton service
- **Result**: Runtime switching in 15 minutes, 4 files touched
- **Key Learning**: Find the minimal intervention point, avoid cascading changes

## Technical Standards

**Clean Architecture**: Proper dependency flow without over-abstraction
**Single Responsibility**: Fat services acceptable if contained to single layer
**Interface Segregation**: Create interfaces only when multiple implementations exist
**Dependency Inversion**: Application services calling Infrastructure through Domain interfaces

## Next User Story Guidelines

**Scope**: 15-30 minute implementations maximum
**Files**: 2-4 files changed/created
**Testing**: Verify endpoints work, don't build comprehensive test suites
**Documentation**: Code comments only, no formal documentation

**Avoid**:
- Multi-week implementation plans
- Complex component hierarchies
- Multiple abstraction layers
- Comprehensive monitoring systems
- Enterprise-grade configuration management

## Available System Features

- Docker containerization with 3-service setup
- PostgreSQL (configured, using in-memory for development speed)
- Comprehensive REST API with health checks
- Vanilla JavaScript frontend with modular architecture
- Volume-mounted AI instruction files
- Review workflow with approval/rejection

## Critical Success Factors

1. **Validate assumptions** about existing system capabilities
2. **Find minimal intervention points** rather than rebuilding systems
3. **Question complexity** - if it takes more than 30 minutes, it's probably over-engineered
4. **Test practically** - does it work end-to-end?
5. **Ship incrementally** - working solution beats perfect architecture

Focus on building practical solutions that demonstrate senior-level engineering judgment: knowing when to stop optimizing and ship working code.

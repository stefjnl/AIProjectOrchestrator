# AIProjectOrchestrator

A .NET 9 Web API that orchestrates AI coding workflows following Clean Architecture principles.

## Project Structure

```
AIProjectOrchestrator/
├── src/
│   ├── AIProjectOrchestrator.API/           # Web API layer
│   ├── AIProjectOrchestrator.Application/   # Business logic, services
│   ├── AIProjectOrchestrator.Domain/        # Domain entities, interfaces
│   └── AIProjectOrchestrator.Infrastructure/ # Data access, external APIs
├── tests/
│   ├── AIProjectOrchestrator.UnitTests/
│   └── AIProjectOrchestrator.IntegrationTests/
├── docs/                                    # Documentation folder
│   ├── user-stories/                        # User stories and requirements
│   ├── architecture/                        # Architecture decisions, diagrams
│   └── setup/                               # Setup and deployment guides
├── Dockerfile                               # Multi-stage .NET 9 API build
├── docker-compose.yml
├── .github/workflows/
└── README.md
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- Docker Desktop
- Visual Studio Code or Visual Studio 2022

### Running the Application Locally

1. Clone the repository
2. Navigate to the project root directory
3. Run the application using Docker:
   ```bash
   docker-compose up --build
   ```

### Running the Application Without Docker

1. Clone the repository
2. Navigate to the project root directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Run the application:
   ```bash
   dotnet run --project src/AIProjectOrchestrator.API
   ```

### Running Tests

```bash
dotnet test
```

## API Endpoints

- `GET /health` - Health check endpoint
- `GET /api/projects` - Get all projects
- `GET /api/projects/{id}` - Get a specific project
- `POST /api/projects` - Create a new project

## Configuration

The application can be configured using `appsettings.json` files in the `AIProjectOrchestrator.API` project.

## Health Check

The application exposes a health check endpoint at `/health` which returns the status of the application and its dependencies.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
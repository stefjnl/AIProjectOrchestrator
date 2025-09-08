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

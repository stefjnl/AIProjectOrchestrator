# AI Project Orchestrator

AI Project Orchestrator is an advanced .NET 9 application that automates the entire software development lifecycle using AI-powered processes. The system transforms high-level project requirements into implementation-ready user stories and coding prompts, creating a streamlined workflow for AI-assisted development.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Workflow Pipeline](#workflow-pipeline)
- [AI Provider Integration](#ai-provider-integration)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Development](#development)
- [Contributing](#contributing)
- [License](#license)

## Overview

AI Project Orchestrator automates the software development process by breaking it down into a 5-stage workflow pipeline that leverages specialized AI models for different aspects of the development lifecycle. The system ensures quality and consistency through a review mechanism at each stage, providing an auditable trail of all AI interactions and decisions.

The application follows Clean Architecture principles with a Domain-Driven Design approach, ensuring separation of concerns and maintainability. It uses .NET Aspire for containerized orchestration and supports multiple AI providers through a unified interface.
![AIProjectOrchestrator](https://github.com/user-attachments/assets/41bce4af-42b4-4dee-be18-5112f002ed4a)

## Architecture

The application follows a Clean Architecture pattern with the following layers:

### Domain Layer
- Core business rules and entities
- Contains requirements, planning, stories, and prompt generation entities
- Defines interfaces for external dependencies
- Maintains business logic independence

### Application Layer
- Use cases and orchestration services
- Coordinates between domain and infrastructure layers
- Contains service interfaces and implementations
- Handles workflow orchestration and business rules application

### Infrastructure Layer
- Data access implementations (Entity Framework Core)
- AI provider integrations
- External service clients
- Configuration and dependency injection setup

### Presentation Layer
- ASP.NET Core Web API controllers
- Razor Pages frontend
- Static file serving
- API endpoints for all workflow stages

## Features

- **AI-Powered Development Pipeline**: Automated conversion of requirements to implementation-ready prompts
- **Multi-Stage Workflow**: 5-stage process with review gates at each stage
- **Multiple AI Provider Support**: Supports various AI models with provider switching capability
- **Review System**: Quality assurance with approval/rejection at each stage
- **Real-Time Progress Tracking**: Live updates on workflow progress
- **Structured Requirements Analysis**: Comprehensive requirements extraction and documentation
- **Project Planning**: Technical architecture and roadmap generation
- **User Story Generation**: Implementation-ready user stories with acceptance criteria
- **Prompt Engineering**: AI-ready prompts for coding assistance
- **Docker Orchestration**: Containerized deployment with .NET Aspire
- **Modular Frontend**: JavaScript-based UI with service bundle architecture
- **Health Checks**: Comprehensive monitoring and health verification

<img width="2042" height="1001" alt="image" src="https://github.com/user-attachments/assets/9b127981-5b1d-4434-b19f-ab6e14f04702" />

## Workflow Pipeline

The application implements a sophisticated 5-stage workflow:

### 1. Requirements Analysis
- **Purpose**: Converts raw project descriptions into structured requirements
- **AI Model**: Requirements Analyst specialized for understanding user needs
- **Output**: Comprehensive requirements document with functional and non-functional requirements
- **Review**: Automated submission for quality review

### 2. Project Planning
- **Purpose**: Creates detailed project plans including architecture, milestones, and roadmap
- **AI Model**: Project Planner for technical architecture decisions
- **Output**: Technology stack selection, architectural decisions, and project roadmap
- **Review**: Automated submission for quality review

### 3. User Stories Generation
- **Purpose**: Transforms requirements and plans into actionable user stories
- **AI Model**: Story Generator for creating implementation-ready stories
- **Output**: User stories organized into epics with acceptance criteria
- **Review**: Automated submission for quality review

### 4. Prompt Generation
- **Purpose**: Converts approved user stories into implementation-ready AI coding prompts
- **AI Model**: Prompt Generator for creating detailed coding instructions
- **Output**: Detailed prompts for AI coding assistants with implementation specifications
- **Review**: Automatically approved (bypasses review for efficiency)

### 5. Final Review
- **Purpose**: Overall project review and workflow completion
- **AI Model**: Not applicable (human review)
- **Output**: Complete project package ready for implementation
- **Review**: Final approval/rejection decision

## AI Provider Integration

The system supports multiple AI providers through a specialized provider architecture:

- **Requirements AI Provider**: Specialized for requirements analysis
- **Planning AI Provider**: Specialized for project planning
- **Story AI Provider**: Specialized for user story generation
- **Prompt Generation AI Provider**: Specialized for prompt engineering
- **Implementation Generation AI Provider**: Specialized for code generation
- **Test Generation AI Provider**: Specialized for test generation

### Supported Providers
- OpenRouter
- Claude (Anthropic)
- LM Studio
- NanoGPT (with proxy service)
- Custom providers can be added through the extensible architecture

### Provider Switching
The application supports runtime switching between AI providers without configuration changes, allowing users to experiment with different models and select the most appropriate one for each task.

## Technology Stack

### Backend
- **Framework**: .NET 9
- **Architecture**: Clean Architecture with Domain-Driven Design
- **Web Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core
- **Database**: PostgreSQL (with in-memory support for development)
- **Logging**: Serilog with structured logging
- **Health Checks**: Custom health check implementations
- **Orchestration**: .NET Aspire

### Frontend
- **Framework**: Vanilla JavaScript (no external frameworks)
- **Templating**: Razor Pages with Bootstrap-inspired CSS
- **API Communication**: Custom API service with error handling
- **State Management**: Modular service bundle architecture
- **UI Components**: Reusable components for workflow management

### Infrastructure
- **Containerization**: Docker with multi-container orchestration
- **Proxy Service**: Python Flask proxy for SSL/TLS compatibility
- **Caching**: Memory and distributed caching strategies
- **Security**: JWT authentication, role-based authorization
- **Monitoring**: Health checks and performance metrics

## Project Structure

```
AIProjectOrchestrator/
├── src/
│   ├── AIProjectOrchestrator.API/          # Presentation layer (Web API, Razor Pages)
│   ├── AIProjectOrchestrator.Application/  # Application layer (Services, Interfaces)
│   ├── AIProjectOrchestrator.Domain/       # Domain layer (Entities, Interfaces, Models)
│   ├── AIProjectOrchestrator.Infrastructure/ # Infrastructure layer (Data access, AI providers)
│   └── AIProjectOrchestrator.ServiceDefaults/ # Common services and configurations
├── Instructions/                          # AI instruction files for each stage
├── proxy/                                # Python proxy service for AI provider compatibility
├── tests/                                # Unit and integration tests
├── docs/                                 # Documentation files
└── memory-bank/                          # Context and progress tracking files
```

### Key Directories

**src/AIProjectOrchestrator.API**
- Controllers for all workflow stages
- Razor Pages for web interface
- Health checks and middleware
- Static file serving configuration

**src/AIProjectOrchestrator.Application**
- Business logic services for each workflow stage
- Service interfaces and implementations
- Orchestrators for complex workflows
- Validators and parsers

**src/AIProjectOrchestrator.Domain**
- Entity definitions for all workflow stages
- Domain services and interfaces
- Value objects and business rules
- Repository interfaces

**src/AIProjectOrchestrator.Infrastructure**
- Data access implementations
- AI provider integrations
- Configuration and dependency injection
- Repository implementations

**Instructions/**
- RequirementsAnalyst.md: AI instructions for requirements analysis
- ProjectPlanner.md: AI instructions for project planning
- StoryGenerator.md: AI instructions for user story generation
- PromptGenerator.md: AI instructions for prompt engineering
- CodeGenerator_Claude.md, CodeGenerator_DeepSeek.md, CodeGenerator_Qwen3Coder.md: AI instructions for code generation

## Installation

### Prerequisites

- .NET 9 SDK
- Docker Desktop (for containerization)
- PostgreSQL (for production) or available for Docker
- Python 3.11 (for proxy service)

### Clone the Repository

```bash
git clone https://github.com/your-username/AIProjectOrchestrator.git
cd AIProjectOrchestrator
```

### Setup Instructions

1. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

2. **Configure AI Provider API Keys**
   - Update `appsettings.json` or use User Secrets for API keys
   - Configure environment variables for AI provider credentials

3. **Run the Application**
   ```bash
   # Using .NET Aspire (recommended)
   dotnet run --project src/AIProjectOrchestrator.AppHost
   
   # Or run the API directly
   dotnet run --project src/AIProjectOrchestrator.API
   ```

4. **Access the Application**
   - Navigate to `http://localhost:5000` (or the port shown in the terminal)

### Docker Setup

1. **Build and Run with Docker Compose**
   ```bash
   docker compose up --build
   ```

2. **Access the Application**
   - The application will be available at the configured port

## Configuration

### Application Settings

The application uses standard .NET configuration with the following key sections:

- `AIProviderConfigurations`: API keys and endpoints for AI providers
- `ConnectionStrings`: Database connection strings
- `AIProviders`: Operation-specific AI provider configurations
- `ExceptionHandling`: Error handling and logging settings

### AI Provider Configuration

Configure AI providers in your `appsettings.json` or User Secrets:

```json
{
  "AIProviderConfigurations": {
    "Claude": {
      "ApiKey": "your-api-key",
      "BaseUrl": "https://api.anthropic.com",
      "TimeoutSeconds": 300
    },
    "OpenRouter": {
      "ApiKey": "your-api-key",
      "BaseUrl": "https://openrouter.ai/api/v1",
      "TimeoutSeconds": 300
    }
  },
  "AIProviders": {
    "Operations": {
      "Requirements": {
        "ProviderName": "Claude",
        "Model": "claude-3-sonnet-20240229",
        "MaxTokens": 4096,
        "Temperature": 0.7,
        "TimeoutSeconds": 300
      },
      "Planning": {
        "ProviderName": "OpenRouter",
        "Model": "openrouter/mixtral-8x7b-instruct",
        "MaxTokens": 4096,
        "Temperature": 0.7,
        "TimeoutSeconds": 300
      }
    }
  }
}
```

## Usage

### Starting a New Project

1. Navigate to the application in your browser
2. Click "Create New Project" 
3. Enter project description and requirements
4. Review and approve each stage of the workflow:
   - Requirements Analysis
   - Project Planning
   - User Stories Generation
   - Prompt Generation
   - Final Review

### Workflow Management

- **Progress Tracking**: Real-time updates on workflow progress
- **Provider Switching**: Switch between AI providers during workflow
- **Review System**: Approve or reject content at each stage
- **Export Functionality**: Export completed projects and artifacts

### API Endpoints

The application provides a comprehensive API for all workflow stages:

```
GET    /api/requirements/{id}           # Get requirements analysis
POST   /api/requirements                # Analyze new requirements
GET    /api/planning/{id}               # Get project planning
POST   /api/planning                    # Create project plan
GET    /api/stories/{id}                # Get user stories
POST   /api/stories                     # Generate stories
GET    /api/prompts/{id}                # Get generated prompts
POST   /api/prompts                     # Generate prompts
GET    /api/reviews/queue               # Review pending items
POST   /api/review                      # Submit review decision
GET    /api/projects                    # List projects
POST   /api/projects                    # Create new project
```

### Frontend Interface

The web interface provides:

- **Project Management**: Create and manage projects
- **Workflow Visualization**: Visual pipeline showing project progress
- **Real-time Updates**: Live updates on workflow status
- **Provider Management**: Switch between AI providers
- **Artifacts Panel**: Side panel showing generated artifacts
- **Review Queue**: Interface for reviewing generated content

## Development

### Adding New Features

1. **Follow Clean Architecture Principles**: Ensure dependencies flow from outer layers to inner layers
2. **Implement Domain-First**: Define domain entities and interfaces first
3. **Use Dependency Injection**: Register services in the DI container
4. **Follow SOLID Principles**: Maintain single responsibility and dependency inversion
5. **Write Tests**: Include unit and integration tests

### Adding New AI Provider

1. Create a new AI provider class implementing the appropriate interface
2. Register the provider in the DI container
3. Update configuration to include the new provider
4. Add health checks if needed

### Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests
dotnet test --filter "Category=Unit"

# Run integration tests
dotnet test --filter "Category=Integration"
```

### Code Quality

The project follows these coding standards:

- **Naming Conventions**: PascalCase for public members, camelCase for parameters
- **Documentation**: XML comments for public APIs
- **Error Handling**: Proper exception handling with logging
- **Performance**: Async/await patterns where appropriate
- **Security**: Input validation and output sanitization

## Contributing

We welcome contributions to the AI Project Orchestrator! Here's how you can help:

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes
4. Add tests if applicable
5. Ensure all tests pass
6. Commit your changes: `git commit -m 'Add amazing feature'`
7. Push to the branch: `git push origin feature/amazing-feature`
8. Open a Pull Request

### Development Guidelines

- Follow the existing code style and naming conventions
- Write clear, descriptive commit messages
- Include tests for new functionality
- Update documentation as needed
- Ensure the code builds and all tests pass
- Follow Clean Architecture and DDD principles

### Code Review Process

All pull requests will be reviewed by maintainers. We look for:

- Code quality and adherence to standards
- Proper test coverage
- Clean architecture compliance
- Performance considerations
- Security implications
- Documentation updates

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions:

1. Check the [existing issues](https://github.com/your-username/AIProjectOrchestrator/issues)
2. Create a new issue if needed
3. Provide detailed information about the problem
4. Include steps to reproduce the issue
5. Share your environment details (OS, .NET version, etc.)

## Acknowledgments

- .NET Team for the excellent development platform
- AI providers whose APIs power this application
- Open-source community for the tools and libraries used
- Contributors who help improve the project

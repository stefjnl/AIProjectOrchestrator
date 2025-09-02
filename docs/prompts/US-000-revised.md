# Implementation Prompt for US-000: Development Environment Setup

## Role
You are a senior .NET architect specializing in Clean Architecture, containerization, and CI/CD automation. You design robust, production-ready development environments that enable developers to go from `git clone` to running locally and deploying via CI/CD with minimal friction.

---

## Task
Create a complete, production-ready development environment for **AIProjectOrchestrator** — a .NET 9 Web API designed to orchestrate AI-powered coding workflows.

The solution must be **fully operational out of the box**, including local development, Docker support, and automated CI/CD via GitHub Actions. A developer should be able to:
- Clone the repo
- Run `docker-compose up` 
- Access the API and health check endpoint
- See passing CI/CD on push

---

## Project Structure Requirements:
AIProjectOrchestrator/
├── src/
│   ├── AIProjectOrchestrator.API/           # Web API layer (controllers, startup)
│   ├── AIProjectOrchestrator.Application/   # Business logic, services, DTOs
│   ├── AIProjectOrchestrator.Domain/        # Entities, domain services, interfaces
│   └── AIProjectOrchestrator.Infrastructure/ # EF Core, repositories, external integrations
├── tests/
│   ├── AIProjectOrchestrator.UnitTests/     # Unit tests (e.g., services)
│   └── AIProjectOrchestrator.IntegrationTests/ # Integration tests (e.g., API, DB)
├── docs/
│   ├── user-stories/                        # Placeholder for user stories (US-000.md, etc.)
│   ├── architecture/                        # ADRs, diagrams, decisions
│   └── setup/
│       ├── README.md                        # Local & Docker setup guide
│       └── ci-cd-setup.md                   # GitHub Actions explanation
├── .github/
│   └── workflows/
│       └── ci-cd.yml                        # GitHub Actions CI/CD pipeline
├── docker-compose.yml                       # PostgreSQL + API service
├── Dockerfile                               # Multi-stage .NET 9 build for API
├── AIProjectOrchestrator.sln                # Solution file at root
├── .gitignore                               # Properly excludes build artifacts, but NOT Dockerfile
└── README.md                                # Getting started guide


---

## Technical Requirements

| Component | Requirement |
|--------|-------------|
| **Framework** | .NET 9 Web API (minimal API or MVC) |
| **Architecture** | Clean Architecture (Domain, Application, Infrastructure, API) |
| **Database** | PostgreSQL with Entity Framework Core (empty DbContext for now) |
| **Containerization** | Docker & docker-compose.yml with:<br>- `ai-project-orchestrator-api` service<br>- `postgres` service (with env, volume, port 5432) |
| **CI/CD** | GitHub Actions workflow that:<br>- Runs on `push` and `pull_request` to `main`<br>- Restores, builds, and tests .NET projects<br>- Builds and pushes Docker image to GHCR on `main` push<br>- Uses correct permissions (`packages: write`) |
| **.gitignore** | Use standard [dotnet .gitignore](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore), but **must NOT ignore `Dockerfile` or `docker-compose.yml`** |
| **Health Checks** | `/health` endpoint returning 200 OK |
| **Logging** | Serilog configured with Console output |
| **Configuration** | `appsettings.json` with connection string placeholder |
| **Docker** | Multi-stage `Dockerfile`:<br>- Builder stage: `mcr.microsoft.com/dotnet/sdk:9.0`<br>- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:9.0`<br>- Exposes port 80 |
| **File Paths** | No nested root folders — `.sln` and `Dockerfile` must be in repo root |

---

## Deliverables

1. **Complete folder structure** as defined above.
2. **Working .NET solution**:
   - `dotnet new sln` at root
   - All projects added to solution
   - Projects reference each other appropriately
3. **Dockerfile**:
   - Multi-stage build
   - Copies source, restores, builds, publishes
   - Final image runs `AIProjectOrchestrator.API`
4. **docker-compose.yml**:
   - Defines `api` and `db` services
   - `api` builds from `Dockerfile` in context `.`
   - `db` uses `postgres:16`, with volume, env, and port mapping
5. **GitHub Actions CI/CD Workflow** (`ci-cd.yml`):
   - Two jobs: `build-and-test` and `build-and-push-docker` (latter only on `main`)
   - Uses `permissions:` block with `packages: write`
   - Logs into `ghcr.io` using `docker/login-action`
   - Tags image as `ghcr.io/${{ github.actor }}/ai-project-orchestrator:latest`
   - Ensures image name is lowercase
6. **README.md**:
   - Clear setup instructions
   - How to run locally (`dotnet run`)
   - How to run with Docker (`docker-compose up`)
   - CI/CD behavior summary
7. **Health Check Controller**:
   - Minimal `/health` endpoint returning `Ok()`
8. **DbContext**:
   - `AppDbContext` derived from `DbContext`
   - Configured in `Program.cs` with PostgreSQL connection string from config
9. **Documentation Structure**:
   - Placeholder files in `/docs/user-stories`, `/docs/architecture`, `/docs/setup`

---

## Constraints & Best Practices

✅ **File System Setup**
- You are running from `C:\git`
- Create project directly in `C:\git\AIProjectOrchestrator\`
- Run:  
  ```powershell
  dotnet new sln -n AIProjectOrchestrator

in the root folder — not inside a subfolder 

    Final structure:
    C:\git\AIProjectOrchestrator\src\AIProjectOrchestrator.API\
    NOT: C:\git\AIProjectOrchestrator\AIProjectOrchestrator\src\...
     

✅ Docker & CI/CD 

    Ensure Dockerfile is NOT ignored by .gitignore. Remove or comment out any lines like Dockerfile*.
    In docker-compose.yml, set build: . so it finds the root Dockerfile
    In GitHub Actions:
        Use docker/login-action@v3 and docker/build-push-action@v5
        Set permissions.packages: write to allow pushing to GHCR
        Use ghcr.io/${{ github.actor }}/ai-project-orchestrator (lowercase enforced)
        Do not use ${{ github.repository }} directly — it may contain uppercase letters
         
     

✅ General 

    Keep dependencies minimal (add only what’s needed)
    Avoid warnings (e.g., unused usings, nullable context)
    Use #nullable enable in all .csproj files
    Solution must build and run locally with dotnet run
     

 
Verification Checklist 

Before finalizing, verify: 

    .sln is in root
    Dockerfile exists and is NOT ignored
    docker-compose.yml references correct build context
    CI/CD workflow includes permissions: packages: write
    Image tag uses lowercase ${{ github.actor }}
    Health check endpoint works
    README.md explains how to run locally and with Docker
    All projects compile: dotnet build succeeds
    docker-compose up starts both API and PostgreSQL
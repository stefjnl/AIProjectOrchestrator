# AI Project Orchestrator - System Analysis (Updated)

## Overview
The AI Project Orchestrator automates software project planning and development using AI, following a multi-stage workflow that transforms high-level project descriptions into code artifacts. Human review checkpoints occur at each AI generation stage. The system uses ASP.NET Core backend with Clean Architecture and a static HTML/JS/CSS frontend, persisting data in PostgreSQL via Entity Framework Core.

## System Architecture

```mermaid
graph TD
    A[Frontend - HTML/CSS/JS] -->|REST API Calls| B[Backend - ASP.NET Core API]
    B -->|Entity Framework Core| C[Database - PostgreSQL]
    B -->|AI Services| D[OpenRouter (qwen/qwen3-coder)]
    B --> E[Review Service - In-Memory Queue with Metadata]
    
    subgraph "Frontend"
        A
    end
    
    subgraph "Backend"
        B
        E
    end
    
    subgraph "External Services"
        D
    end
    
    subgraph "Data Storage"
        C
    end
```

## Detailed Component Analysis

### 1. Frontend (Client-Side)
Static web application with HTML, CSS, JavaScript for user interaction.

**Key Components:**
- Static pages for workflow management and review queue
- APIClient in api.js for REST calls to backend
- WorkflowManager in workflow.js for state polling and UI updates (every 10s via getWorkflowStatus)
- Responsive design with status indicators and button state management

**Main Pages:**
- `projects/workflow.html` - Workflow stages with buttons to initiate generation (e.g., startRequirementsAnalysis calls POST /api/requirements/analyze, redirects to queue on pending)
- `reviews/queue.html` - Loads pending reviews via GET /api/review/pending, approve/reject via POST /api/review/{id}/approve/reject, pipeline-aware redirects (e.g., stories → stories-overview.html)
- `projects/create.html` - Project creation form (POST /api/projects)
- `projects/list.html` - Project listing (GET /api/projects)
- `projects/stories-overview.html` - Story and prompt management (Phase 4)

### 2. Backend (Server-Side)
ASP.NET Core Web API with Clean Architecture: Domain (entities/interfaces), Application (services), Infrastructure (repositories/AI clients), API (controllers).

**Key Components:**
- Controllers route HTTP requests to application services
- Services orchestrate AI calls, validation, review submission, and status updates
- Domain entities represent workflow artifacts with statuses (e.g., PendingReview, Approved)
- Infrastructure handles PostgreSQL via repositories and OpenRouter AI client
- Dependency injection with ILogger, IOptions, and lazy services (e.g., Lazy<IReviewService>)
- Health checks in HealthChecks/

**Main API Controllers:**
- `ProjectsController` - CRUD for projects (e.g., POST /api/projects)
- `RequirementsController` - Analyze requirements (POST /api/requirements/analyze), status/retrieval (GET /api/requirements/{id}), direct approval (POST /api/requirements/{id}/approve)
- `ProjectPlanningController` - Create plan (POST /api/projectplanning/create), status updates
- `StoriesController` - Generate stories (POST /api/stories/generate)
- `PromptGenerationController` - Generate prompts (POST /api/PromptGeneration/generate)
- `ReviewController` - Submit review (POST /api/review/submit), pending list (GET /api/review/pending), approve/reject (POST /api/review/{id}/approve/reject), workflow status (GET /api/review/workflow-status/{projectId})
- `CodeController` - Generate code (POST /api/code/generate)
- `AITestController` - Testing endpoints

### 3. Database (PostgreSQL)
Persistent storage via Entity Framework Core with AppDbContext.

**Key Entities:**
- `Project` - Core project with Id (int), Name, Description
- `RequirementsAnalysis` - AI-generated analysis with AnalysisId (string/GUID), ProjectId (int FK), Status (PendingReview/Approved), Content, ReviewId (string)
- `ProjectPlanning` - Plan with PlanningId, Status, ReviewId
- `StoryGeneration` - Stories collection with GenerationId, Status, ReviewId, StoryCount
- `PromptGeneration` - Per-story prompts with PromptId, StoryIndex, Status, ReviewId
- `Review` - Review submissions (likely metadata-linked, not fully persistent)

**Relationships:**
- Project (1) → RequirementsAnalysis/ProjectPlanning/StoryGeneration/PromptGeneration (Many)
- Each artifact (1) → ReviewId (for approval linkage via metadata)
- Migrations in Infrastructure/Migrations/

## User Workflow

Linear multi-stage process with prerequisites and review checkpoints. Frontend polls backend for status changes.

### Stage 1: Project Creation
1. User submits form on create.html → POST /api/projects via APIClient.createProject()
2. Backend creates Project entity in DB
3. Redirect to workflow.html?projectId={id}

### Stage 2: Requirements Analysis
1. On workflow.html, click "Start Analysis" → startRequirementsAnalysis() sets generating state, fetches project description, calls APIClient.analyzeRequirements({ProjectDescription, ProjectId})
2. RequirementsController.AnalyzeRequirements() → RequirementsAnalysisService.AnalyzeRequirementsAsync(): Validates input, loads RequirementsAnalyst.md instructions via IInstructionService, creates AIRequest (system message + prompt), calls OpenRouter IAIClient (qwen/qwen3-coder, temp=0.7, maxTokens=2000), stores RequirementsAnalysis entity (Status=PendingReview), submits to IReviewService.SubmitForReviewAsync() with metadata {AnalysisId, EntityId, ProjectId}, updates entity with ReviewId
3. Returns RequirementsAnalysisResponse with analysisId, reviewId, status=PendingReview
4. Frontend alerts and redirects to queue.html?projectId={id}
5. On queue.html, loadPendingReviews() → APIClient.getPendingReviews() → GET /api/review/pending → ReviewController.GetPendingReviews() → IReviewService.GetPendingReviewsAsync()
6. User clicks "Approve" → approveReview(reviewId) → APIClient.approveReview(reviewId) → POST /api/review/{id}/approve → ReviewController.ApproveReview() extracts AnalysisId from metadata, calls _requirementsAnalysisService.UpdateAnalysisStatusAsync(analysisId, Approved), returns ReviewResponse
7. Workflow polling (getWorkflowStatus) detects status change, enables "Start Planning" button

### Stage 3: Project Planning
Similar to Stage 2: Button calls APIClient.createProjectPlan({requirementsAnalysisId}) → checks prerequisites (canCreateProjectPlan), generates via IProjectPlanningService using ProjectPlanner.md, submits to review, approval updates status.

### Stage 4: User Story Generation
Button calls APIClient.generateStories({planningId}) → IStoryGenerationService using StoryGenerator.md, generates collection, submits for review. On approval, auto-redirects to stories-overview.html for Phase 4 prompt management (per-story prompts via PromptGenerationController).

### Stage 5: Prompt Generation (Phase 4)
Individual prompts per story: stories-overview.html generates via APIClient.generatePrompt({storyGenerationId, storyIndex}), submits to review. Completion tracked in workflow.html dashboard (progress bar, stats). All prompts approved enables code generation.

### Stage 6: Code Generation
Button calls APIClient.generateCode({storyGenerationId}) → ICodeGenerationService, final output without review (or optional).

## Component Interactions

### Frontend ↔ Backend
- JSON REST via fetch in api.js (baseUrl: http://localhost:8086/api)
- Error handling with circuit breaker (3 failures → maintenance mode)
- WorkflowManager polls /api/review/workflow-status/{projectId} for state sync
- No authentication (CORS-enabled)

### Backend ↔ Database
- EF Core repositories (e.g., IRequirementsAnalysisRepository.AddAsync/UpdateAsync/GetByAnalysisIdAsync)
- Status enums (RequirementsAnalysisStatus.NotStarted/PendingReview/Approved/Failed)
- Automatic migrations in development

### Backend ↔ AI Services
- IAIClientFactory creates OpenRouter client for requirements (qwen/qwen3-coder)
- IInstructionService loads .md files (Instructions/RequirementsAnalyst.md) as system messages
- AIRequest: SystemMessage (instructions), Prompt (project description + context), Temperature=0.7, MaxTokens=2000
- Responses parsed as Content, validated for success

### Review Process
- Services submit via IReviewService.SubmitForReviewAsync(SubmitReviewRequest {ServiceName="RequirementsAnalysis", Content=AI output, Metadata={AnalysisId, ProjectId}})
- Reviews stored in-memory, fetched via GetPendingReviewsAsync()
- Approval: ReviewController.ApproveReview() → IReviewService.ApproveReviewAsync() + service-specific UpdateStatusAsync(AnalysisId, Approved) via metadata extraction
- Rejection requires feedback, reloads queue
- Cleanup via ReviewCleanupService (background)

## Key Features

1. **Multi-Stage Workflow**: Prerequisite-checked stages with polling-based progression
2. **Human-in-the-Loop**: Central review queue with approve/reject, metadata-linked status updates
3. **AI Integration**: OpenRouter with instruction-based prompts (.md files), specific model for requirements
4. **Persistent Storage**: PostgreSQL for artifacts, in-memory for transient reviews
5. **State Management**: Frontend polling syncs with backend statuses, no localStorage
6. **Responsive UI**: Dynamic buttons/statuses, progress dashboards (Phase 4), temporary notifications
7. **Error Handling**: Try-catch in services/controllers, circuit breaker in frontend, validation (e.g., description length >=10)
8. **Health Monitoring**: HealthChecks/ endpoints, logging via ILogger

## Running the Application with Docker / docker-compose

The application can be deployed using Docker and docker-compose for easy setup of the full stack (backend API, frontend, PostgreSQL database).

### Prerequisites
- Docker and Docker Compose installed
- OpenRouter API key (set as environment variable: `OPENROUTER_API_KEY=your_api_key_here`)
- Optional: .NET SDK for local development

### Files
- `docker-compose.yml`: Orchestrates services (api, frontend, postgres)
- `Dockerfile` (root): Builds the ASP.NET Core backend API
- `frontend/Dockerfile`: Builds nginx container for static frontend files
- `frontend/nginx.conf`: Nginx configuration for serving frontend assets

### Commands
1. **Start the full stack** (development mode with hot reload):
   ```
   docker-compose up -d
   ```
   - Backend API: http://localhost:8086
   - Frontend: http://localhost:80 (served via nginx)
   - Database: PostgreSQL on internal network (auto-migrated by backend)

2. **View logs**:
   ```
   docker-compose logs -f api
   docker-compose logs -f frontend
   docker-compose logs -f postgres
   ```

3. **Stop services**:
   ```
   docker-compose down
   ```
   - Add `-v` to remove volumes (clears database): `docker-compose down -v`

4. **Rebuild after code changes**:
   ```
   docker-compose up --build -d
   ```

5. **Run database migrations manually** (if needed):
   ```
   docker-compose exec api dotnet ef database update
   ```

### Environment Configuration
- Set `OPENROUTER_API_KEY` in `.env` file or export before running
- Database connection string auto-configured in docker-compose.yml (points to postgres service)
- Frontend API base URL set to backend service (http://api:8086 in container network)
- Ports: Backend exposed on 8086, Frontend on 80

### Troubleshooting
- **Backend fails to start**: Check logs for EF migrations or missing API key
- **Frontend 404 errors**: Ensure nginx.conf serves /js/* and /css/* correctly
- **Database connection issues**: Verify postgres service healthy (`docker-compose ps`)
- **AI calls fail**: Validate OPENROUTER_API_KEY and network access

### Production Deployment
For production:
1. Use multi-stage Dockerfiles for optimized images
2. Set environment to Production in appsettings.json
3. Configure HTTPS and proper CORS
4. Use persistent volumes for postgres data
5. Scale services if needed (e.g., multiple API instances)

## Data Flow

```mermaid
graph LR
    A[User clicks Start Analysis] --> B[Frontend: POST /api/requirements/analyze]
    B --> C[RequirementsController → Service]
    C --> D[Load Instructions + AI Call (OpenRouter)]
    D --> E[Store Entity (PendingReview) + Submit Review]
    E --> F[User: GET /api/review/pending on queue.html]
    F --> G[Click Approve → POST /api/review/{id}/approve]
    G --> H[ReviewController → Update Entity Status to Approved]
    H --> I[Workflow polls GET /api/review/workflow-status/{projectId}]
    I --> J[UI enables next stage]
```

This updated analysis reflects the current codebase structure, with emphasis on the requirements generation/approval workflow and code relations.

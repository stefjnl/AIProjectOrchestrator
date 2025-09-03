  Application Overview

  The AI Project Orchestrator is a .NET 9 Web API application that automates the software development workflow by orchestrating AI models through a multi-stage pipeline. It follows Clean Architecture principles with
  a clear separation between Domain, Application, Infrastructure, and API layers.

  Key Features
   1. Multi-Provider AI Integration: Supports Claude API, LM Studio (local), and OpenRouter providers
   2. Instruction-based AI Services: Uses markdown instruction files to guide AI behavior for different tasks
   3. Human-in-the-Loop Workflow: Requires human approval at key pipeline stages
   4. Complete Development Pipeline: Transforms high-level ideas into working code through structured stages
   5. Context Management: Maintains context across the entire development lifecycle

  Architecture
   - Domain Layer: Contains core entities, interfaces, and models
   - Application Layer: Implements business logic and orchestrates services
   - Infrastructure Layer: Handles data access, AI model clients, and external integrations
   - API Layer: Provides RESTful endpoints and handles HTTP requests

  Functional Flow

  User Workflow
   1. Project Creation
      - User creates a project in the system
      - System initializes project tracking for the AI orchestration pipeline

   2. Requirements Analysis
      - User submits a high-level project description to the system
      - AI analyzes the description and generates structured requirements
      - System submits AI output for human review

   3. Human Review (Requirements Analysis)
      - AI generates structured requirements analysis based on user's project description
      - User reviews and approves the AI-generated requirements analysis through the web interface

   4. Project Planning
      - User initiates project planning based on approved requirements
      - AI generates project roadmap, architecture decisions, and milestones
      - System submits AI output for human review

   5. Human Review (Project Planning)
      - AI generates the project plan based on approved requirements
      - User reviews and approves the AI-generated project plan through the web interface

   6. User Story Generation
      - User initiates user story generation based on approved project plan
      - AI generates detailed user stories with acceptance criteria
      - System submits AI output for human review

   7. Human Review (User Stories)
      - AI generates the user stories based on approved project plan
      - User reviews and approves the AI-generated user stories through the web interface

   8. Code Generation
      - User initiates code generation based on approved user stories
      - AI generates code implementations and test files using optimal AI models
      - System submits AI output for human review

   9. Code Review/Download
      - AI generates the code implementation based on approved user stories
      - User reviews the generated code through the web interface or downloads it as a ZIP file

  Available Scenarios
   1. Requirements Analysis: Transform project ideas into structured requirements
   2. Project Planning: Create roadmaps, milestones, and architectural decisions
   3. Story Generation: Generate implementable user stories with acceptance criteria
   4. Code Generation: Produce working code implementations with tests

  Technical Flow

  Endpoints and Services

  1. Requirements Analysis
   - Endpoint: POST /api/requirements/analyze
   - Service: IRequirementsAnalysisService
   - Flow:
     1. Validate RequirementsAnalysisRequest
     2. Load RequirementsAnalyst.md instructions via IInstructionService
     3. Combine instructions with project description
     4. Call AI model via IAIClient
     5. Submit results to IReviewService for human approval
     6. Return RequirementsAnalysisResponse with review ID

  2. Project Planning
   - Endpoint: POST /api/planning/create
   - Service: IProjectPlanningService
   - Flow:
     1. Validate ProjectPlanningRequest and check if requirements are approved
     2. Load ProjectPlanner.md instructions via IInstructionService
     3. Retrieve approved requirements analysis results
     4. Combine context, instructions, and preferences
     5. Call AI model via IAIClient
     6. Submit results to IReviewService for human approval
     7. Return ProjectPlanningResponse with review ID

  3. Story Generation
   - Endpoint: POST /api/stories/generate
   - Service: IStoryGenerationService
   - Flow:
     1. Validate StoryGenerationRequest and check if planning is approved
     2. Load StoryGenerator.md instructions via IInstructionService
     3. Retrieve approved project planning results
     4. Combine context with instructions
     5. Call AI model via IAIClient
     6. Submit results to IReviewService for human approval
     7. Return StoryGenerationResponse with review ID

  4. Code Generation
   - Endpoint: POST /api/code/generate
   - Service: ICodeGenerationService
   - Flow:
     1. Validate CodeGenerationRequest and check if stories are approved
     2. Load code generation instructions via IInstructionService
     3. Retrieve approved user stories
     4. Combine context with technical preferences
     5. Call AI model via IAIClient
     6. Submit results to IReviewService for human approval
     7. Return CodeGenerationResponse with review ID

  5. Review Management
   - Endpoints:
     - POST /api/review/submit - Submit content for review
     - GET /api/review/{id} - Get review details
     - POST /api/review/{id}/approve - Approve a review
     - POST /api/review/{id}/reject - Reject a review
     - GET /api/review/pending - Get pending reviews
   - Service: IReviewService
   - Flow: Manages human approval workflow for all AI-generated content

  Data Models
   - Project: Basic project entity with ID, name, description
   - RequirementsAnalysis: Analysis of project requirements with AI-generated content
   - ProjectPlanning: Project roadmap, architecture decisions, and milestones
   - UserStory: Individual user stories with acceptance criteria
   - CodeArtifact: Generated code files with metadata

  Core Services
   1. IInstructionService: Loads and manages AI instruction files
   2. IAIClient: Abstract interface for calling different AI providers
   3. IReviewService: Manages human approval workflow
   4. IProjectService: Manages basic project entities

  This system creates a complete pipeline for transforming ideas into code, with quality gates at each stage to ensure human oversight of AI-generated content.
# Frontend Architecture Diagram

## System Architecture Overview

```mermaid
graph TB
    subgraph "Client Browser"
        A[User Request] --> B[Razor Pages Engine]
        B --> C{Page Request}
        C -->|Home| D[Pages/Index.cshtml]
        C -->|Projects| E[Projects/*.cshtml]
        C -->|Reviews| F[Reviews/Queue.cshtml]
        C -->|Error| G[Error.cshtml]
    end
    
    subgraph "ASP.NET Core Pipeline"
        B --> H[Static Files Middleware]
        B --> I[Razor Pages Middleware]
        B --> J[API Controllers Middleware]
        H --> K[wwwroot/]
        I --> L[Pages/]
        J --> M[Controllers/]
    end
    
    subgraph "Frontend Assets"
        K --> N[css/styles.css]
        K --> O[js/api.js]
        K --> P[js/app.js]
        K --> Q[js/workflow.js]
        L --> R[_Layout.cshtml]
        L --> S[Individual Page Views]
    end
    
    subgraph "API Integration"
        O --> T[/api]
        P --> T
        Q --> T
        T --> U[ProjectsController]
        T --> V[RequirementsController]
        T --> W[StoriesController]
        T --> X[ReviewController]
    end
    
    subgraph "Backend Services"
        U --> Y[ProjectService]
        V --> Z[RequirementsAnalysisService]
        W --> AA[StoryGenerationService]
        X --> AB[ReviewService]
    end
    
    subgraph "Data Layer"
        Y --> AC[ProjectRepository]
        Z --> AD[RequirementsAnalysisRepository]
        AA --> AE[StoryGenerationRepository]
        AB --> AF[ReviewRepository]
        AC --> AG[(PostgreSQL)]
        AD --> AG
        AE --> AG
        AF --> AG
    end
```

## Razor Pages Architecture

```mermaid
graph TB
    subgraph "Pages Structure"
        A[Pages/] --> B[_ViewStart.cshtml]
        A --> C[_Layout.cshtml]
        A --> D[Index.cshtml]
        A --> E[Error.cshtml]
        A --> F[Projects/]
        F --> G[Index.cshtml]
        F --> H[Create.cshtml]
        F --> I[List.cshtml]
        F --> J[Workflow.cshtml]
        A --> K[Reviews/]
        K --> L[Queue.cshtml]
    end
    
    subgraph "Layout Components"
        C --> M[Header with Navigation]
        C --> N[Main Content Area]
        C --> O[Footer]
        M --> P[Active State Detection]
        M --> Q[Responsive Menu]
    end
    
    subgraph "Page Models"
        D --> R[HomeModel]
        G --> S[ProjectsListModel]
        H --> T[CreateProjectModel]
        J --> U[WorkflowModel]
        L --> V[ReviewQueueModel]
    end
    
    subgraph "Services & Dependencies"
        R --> W[IProjectService]
        S --> W
        T --> W
        U --> W
        V --> IReviewService
        W --> X[IAIClient]
        V --> X
    end
```

## CSS Architecture System

```mermaid
graph TB
    subgraph "CSS Architecture"
        A[styles.css] --> B[:root Variables]
        A --> C[Base Styles]
        A --> D[Component Styles]
        A --> E[Layout Styles]
        A --> F[Utility Classes]
    end
    
    subgraph "CSS Variables"
        B --> G[Color System]
        B --> H[Typography System]
        B --> I[Spacing System]
        B --> J[Animation System]
        G --> K[Primary Colors]
        G --> L[Semantic Colors]
        G --> M[Neutral Colors]
        G --> N[Gradients]
        H --> O[Font Families]
        H --> P[Font Sizes]
        H --> Q[Font Weights]
    end
    
    subgraph "Component Library"
        D --> R[Cards]
        D --> S[Buttons]
        D --> T[Forms]
        D --> U[Navigation]
        D --> V[Modals]
        D --> W[Badges]
        D --> X[Tables]
        R --> Y[Modern Card Design]
        S --> Z[Premium Button System]
        T --> AA[Enhanced Form Styling]
    end
    
    subgraph "Responsive Design"
        E --> BB[Breakpoints]
        E --> CC[Grid System]
        E --> DD[Flexbox Layout]
        BB --> EE[Mobile First]
        BB --> FF[Tablet]
        BB --> GG[Desktop]
    end
```

## JavaScript Architecture

```mermaid
graph TB
    subgraph "JavaScript Modules"
        A[wwwroot/js/] --> B[api.js]
        A --> C[app.js]
        A --> D[workflow.js]
    end
    
    subgraph "API Client (api.js)"
        B --> E[window.APIClient]
        E --> F[makeRequest Method]
        E --> G[Error Handling]
        E --> H[Response Processing]
        F --> I[GET/POST/PUT/DELETE]
        F --> J[Authentication]
        F --> K[Retry Logic]
    end
    
    subgraph "Application Logic (app.js)"
        C --> L[Page Initialization]
        C --> M[Event Handlers]
        C --> N[State Management]
        C --> O[Utility Functions]
        L --> P[DOM Ready Handlers]
        M --> Q[Form Submissions]
        M --> R[Button Clicks]
        N --> S[Local Storage]
        N --> T[Session State]
    end
    
    subgraph "Workflow Manager (workflow.js)"
        D --> U[WorkflowManager Class]
        U --> V[State Management]
        U --> W[API Integration]
        U --> X[UI Updates]
        V --> Y[Current Stage]
        V --> Z[Project Data]
        W --> AA[Stage Progression]
        W --> BB[Status Polling]
        X --> CC[DOM Updates]
        X --> DD[Animations]
    end
```

## Data Flow Architecture

```mermaid
sequenceDiagram
    participant U as User
    participant B as Browser
    participant R as Razor Pages
    participant A as API
    participant S as Services
    participant D as Database
    
    U->>B: Navigate to Page
    B->>R: Request Page
    R->>R: Process Request
    R->>B: Return HTML
    
    U->>B: User Action
    B->>A: API Call (/api/*)
    A->>S: Process Request
    S->>D: Query/Update
    D->>S: Return Data
    S->>A: Return Response
    A->>B: Return JSON
    B->>U: Update UI
    
    Note over B,D: Continuous Polling for Workflow Updates
    loop Every 5 seconds
        B->>A: GET /api/projects/{id}/status
        A->>S: Check Status
        S->>D: Query Database
        D->>S: Return Status
        S->>A: Return Status
        A->>B: Return Status
        B->>B: Update UI if Changed
    end
```

## Security Architecture

```mermaid
graph TB
    subgraph "Security Layers"
        A[Browser] --> B[CSP Headers]
        A --> C[XSS Protection]
        A --> D[Input Validation]
        B --> E[Content Security Policy]
        C --> F[Output Encoding]
        D --> G[Client Validation]
        E --> H[Restrict Scripts]
        F --> I[HTML Escaping]
        G --> J[API Parameter Validation]
    end
    
    subgraph "API Security"
        K[API Gateway] --> L[Authentication]
        K --> M[Authorization]
        K --> N[Rate Limiting]
        L --> O[API Key Validation]
        M --> P[Role-Based Access]
        N --> Q[Request Throttling]
    end
    
    subgraph "Data Protection"
        R[Data in Transit] --> S[HTTPS]
        R --> T[API Encryption]
        S --> SSL/TLS
        T --> AES-256
    end
```

## Performance Architecture

```mermaid
graph TB
    subgraph "Performance Optimizations"
        A[Frontend] --> B[Asset Minification]
        A --> C[Compression]
        A --> D[Caching]
        A --> E[Lazy Loading]
        B --> F[CSS/JS Minification]
        C --> G[Gzip/Brotli]
        D --> H[Browser Caching]
        E --> I[Image Lazy Loading]
    end
    
    subgraph "API Performance"
        J[API Layer] --> K[Response Compression]
        J --> L[Caching]
        J --> M[Connection Pooling]
        K --> N[Gzip Compression]
        L --> O[Redis Cache]
        M --> P[HTTP Client Pool]
    end
    
    subgraph "Database Performance"
        Q[Database] --> R[Indexing]
        Q --> S[Connection Pooling]
        Q --> T[Query Optimization]
        R --> U[Proper Indexes]
        S --> V[Connection Limits]
        T --> W[Query Plans]
    end
```

This architecture provides a comprehensive view of how the new frontend will be structured and interact with the existing backend systems.
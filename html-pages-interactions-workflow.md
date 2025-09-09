# HTML Pages, Interactions & User Workflow Analysis

## Complete HTML Page Inventory

### 1. Landing & System Pages
- **[`index.html`](src/AIProjectOrchestrator.API/wwwroot/index.html:1)** - Main dashboard with system status and recent projects
- **[`faq.html`](src/AIProjectOrchestrator.API/wwwroot/faq.html:1)** - System analysis documentation with architecture diagrams
- **[`system-analysis.html`](src/AIProjectOrchestrator.API/wwwroot/system-analysis.html:1)** - Live system health monitoring dashboard

### 2. Project Management Pages
- **[`projects/create.html`](src/AIProjectOrchestrator.API/wwwroot/projects/create.html:1)** - Project creation form with Markdown preview
- **[`projects/list.html`](src/AIProjectOrchestrator.API/wwwroot/projects/list.html:1)** - All projects grid view with status badges
- **[`projects/workflow.html`](src/AIProjectOrchestrator.API/wwwroot/projects/workflow.html:1)** - 5-stage workflow management interface
- **[`projects/stories-overview.html`](src/AIProjectOrchestrator.API/wwwroot/projects/stories-overview.html:1)** - Individual story management and prompt generation

### 3. Review & Quality Pages
- **[`reviews/queue.html`](src/AIProjectOrchestrator.API/wwwroot/reviews/queue.html:1)** - Central approval queue with pipeline-aware redirects

### 4. Development & Testing Pages
- **[`prompt-playground.html`](src/AIProjectOrchestrator.API/wwwroot/prompt-playground.html:1)** - Interactive prompt template editor
- **[`test-scenarios.html`](src/AIProjectOrchestrator.API/wwwroot/test-scenarios.html:1)** - Predefined project scenario testing
- **[`test-truncation-simple.html`](src/AIProjectOrchestrator.API/wwwroot/test-truncation-simple.html:1)** - Text truncation functionality testing

## Page Interactions & Navigation Flow

### Global Navigation Pattern
All pages include a consistent navigation header with these buttons:
```html
<div class="main-nav-buttons">
    <a href="/projects/create.html"><button>Create New Project</button></a>
    <a href="/projects/list.html"><button>View All Projects</button></a>
    <a href="/reviews/queue.html"><button>Go To Review Queue</button></a>
    <a href="/prompt-playground.html"><button>Prompt Playground</button></a>
</div>
```

### Detailed Page Interactions

#### 1. index.html (Dashboard)
**Interactions:**
- Displays system statistics (total projects, pending reviews)
- Shows 3 most recent projects with truncated descriptions
- Each project card has "Continue Workflow" button → workflow.html?projectId={id}
- "Create New Project" → create.html
- "View All Projects" → list.html
- "Go To Review Queue" → queue.html
- "Prompt Playground" → prompt-playground.html

**JavaScript Integration:**
- [`window.APIClient.getProjects()`](src/AIProjectOrchestrator.API/wwwroot/js/api.js:104) for project data
- [`renderMarkdownToHTML()`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:9) for description rendering
- [`initTruncatedDescription()`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:93) for text truncation

#### 2. projects/create.html (Project Creation)
**Interactions:**
- Real-time Markdown preview as user types
- Form validation (name and description required)
- Submit → API call → Redirect to workflow.html?projectId={id}&newProject=true
- Back link to index.html

**JavaScript Integration:**
- [`window.APIClient.createProject()`](src/AIProjectOrchestrator.API/wwwroot/js/api.js:114) for project creation
- Real-time preview using [`renderMarkdownToHTML()`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:9)

#### 3. projects/workflow.html (5-Stage Workflow)
**Interactions:**
- **Stage 1**: "Start Analysis" → API call → Redirect to queue.html
- **Stage 2**: "Start Planning" → Prerequisite check → API call → Redirect to queue.html  
- **Stage 3**: "Generate Stories" → Prerequisite check → API call → Redirect to queue.html
- **Stage 4**: "View All Prompts" → Redirect to stories-overview.html (when approved)
- **Stage 5**: "Generate Code" → Prerequisite check → API call → Completion
- Progress indicator with milestone dots and percentage
- "Reset Workflow" button for state reset
- Auto-polling every 2-30 seconds for status updates

**JavaScript Integration:**
- [`WorkflowManager`](src/AIProjectOrchestrator.API/wwwroot/js/workflow.js:1) class for state management
- [`startRequirementsAnalysis()`](src/AIProjectOrchestrator.API/wwwroot/projects/workflow.html:325) for Stage 1
- [`startProjectPlanning()`](src/AIProjectOrchestrator.API/wwwroot/projects/workflow.html:397) for Stage 2
- [`startStoryGeneration()`](src/AIProjectOrchestrator.API/wwwroot/projects/workflow.html:473) for Stage 3
- [`startCodeGeneration()`](src/AIProjectOrchestrator.API/wwwroot/projects/workflow.html:548) for Stage 5

#### 4. projects/stories-overview.html (Story Management)
**Interactions:**
- Individual story approval/rejection with feedback
- Story editing with inline forms
- Prompt generation per story (when approved)
- Prompt viewing with modal popup
- Bulk download of approved prompts
- "Back to Project Workflow" → workflow.html

**JavaScript Integration:**
- [`loadAndRenderStories()`](src/AIProjectOrchestrator.API/wwwroot/projects/stories-overview.html:111) for story loading
- [`approveStory()`](src/AIProjectOrchestrator.API/wwwroot/projects/stories-overview.html:536) for individual approval
- [`generatePrompt()`](src/AIProjectOrchestrator.API/wwwroot/projects/stories-overview.html:782) for prompt generation
- [`viewPrompt()`](src/AIProjectOrchestrator.API/wwwroot/projects/stories-overview.html:875) for prompt viewing

#### 5. reviews/queue.html (Review Queue)
**Interactions:**
- Load pending reviews on page load
- Approve/Reject/Delete actions for each review
- Pipeline-aware redirects:
  - Stories stage → stories-overview.html
  - Other stages → workflow.html
- Real-time status updates

**JavaScript Integration:**
- [`loadPendingReviews()`](src/AIProjectOrchestrator.API/wwwroot/reviews/queue.html:29) for queue population
- [`approveReview()`](src/AIProjectOrchestrator.API/wwwroot/reviews/queue.html:68) with pipeline detection
- [`rejectReview()`](src/AIProjectOrchestrator.API/wwwroot/reviews/queue.html:125) with feedback prompt

#### 6. projects/list.html (All Projects)
**Interactions:**
- Grid layout of project cards
- Real-time stage status calculation via API
- "Continue Workflow" → workflow.html?projectId={id}
- Delete project with confirmation
- Toast notifications for actions

**JavaScript Integration:**
- [`populateProjectList()`](src/AIProjectOrchestrator.API/wwwroot/projects/list.html:57) for card rendering
- [`getProjectStage()`](src/AIProjectOrchestrator.API/wwwroot/projects/list.html:99) for status calculation
- [`deleteProject()`](src/AIProjectOrchestrator.API/wwwroot/projects/list.html:153) with confirmation

## Complete User Workflow

### Primary Workflow: Project Creation to Completion

```mermaid
graph TD
    A[index.html] -->|Create Project| B[projects/create.html]
    B -->|Submit Form| C[projects/workflow.html?projectId={id}&newProject=true]
    
    C -->|Stage 1| D[Start Requirements Analysis]
    D -->|API Call| E[reviews/queue.html?projectId={id}]
    E -->|Approve| F[Back to workflow.html]
    F -->|Stage 2| G[Start Project Planning]
    G -->|API Call| H[reviews/queue.html]
    H -->|Approve| I[Back to workflow.html]
    I -->|Stage 3| J[Generate User Stories]
    J -->|API Call| K[reviews/queue.html]
    K -->|Approve| L[projects/stories-overview.html?projectId={id}]
    
    L -->|Individual Stories| M[Approve/Reject Each Story]
    M -->|Generate Prompts| N[reviews/queue.html per story]
    N -->|Approve Prompts| O[Back to stories-overview.html]
    O -->|All Approved| P[Back to workflow.html]
    P -->|Stage 5| Q[Generate Code]
    Q -->|Complete| R[workflow.html - Project Complete]
```

### Alternative Entry Points

1. **From Project List**: projects/list.html → workflow.html (existing project)
2. **From Dashboard**: index.html → any project card → workflow.html
3. **Direct Review Access**: Any page → reviews/queue.html → workflow/stories-overview

### Key Workflow Features

1. **Prerequisite Validation**: Each stage checks if previous stage is approved
2. **Pipeline-Aware Navigation**: Review approvals redirect to appropriate next stage
3. **State Persistence**: All state managed through API, minimal local storage
4. **Real-time Updates**: Polling keeps workflow status current across all pages
5. **Error Recovery**: Circuit breaker pattern with maintenance mode fallback
6. **User Feedback**: Toast notifications and status messages throughout

### Data Flow Patterns

1. **API-First Architecture**: All data operations go through [`window.APIClient`](src/AIProjectOrchestrator.API/wwwroot/js/api.js:1)
2. **State Synchronization**: [`WorkflowManager`](src/AIProjectOrchestrator.API/wwwroot/js/workflow.js:1) handles cross-page state consistency
3. **Error Handling**: Structured error responses with user-friendly messages
4. **Security**: Input sanitization via [`DOMPurify`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:33) and HTML escaping

This workflow represents a complete human-in-the-loop AI project orchestration system with review checkpoints at each major stage, ensuring quality control while maintaining an intuitive user experience.
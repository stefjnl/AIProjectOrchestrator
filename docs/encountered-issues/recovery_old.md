# AI Project Orchestrator - Frontend Recovery Prompt

## Context & Objective

You are helping a senior .NET developer (15+ years experience) rebuild the frontend for an AI Project Orchestrator after losing work due to git issues. The **backend API is fully functional** - we only need to recreate the frontend that was working perfectly before.

## System Overview

### Backend Status ✅ (Already Working)
- **.NET 9 Web API** with Clean Architecture
- **PostgreSQL database** with Docker containers
- **AI Services**: Requirements Analysis, Project Planning, Story Generation, Code Generation
- **Review System**: Human approval workflow with state synchronization
- **API Endpoints**: All REST endpoints functional and tested

### Frontend Requirements 🔨 (To Rebuild)
A **4-stage workflow application** that orchestrates AI development pipeline with sophisticated state management.

## Application Architecture to Rebuild

### Directory Structure
```
frontend/
├── index.html                    # Landing/dashboard page
├── projects/
│   ├── create.html              # Project creation form
│   ├── list.html                # Project listing page
│   └── workflow.html            # MAIN: 4-stage workflow interface
├── reviews/
│   └── queue.html               # Review approval interface
├── js/
│   ├── api.js                   # Complete API wrapper functions
│   ├── app.js                   # Global application functions
│   └── workflow.js              # WorkflowManager class + state logic
└── css/
    └── styles.css               # Application styling
```

## Core Components to Implement

### 1. Main Workflow Interface (`projects/workflow.html`)

**UI Layout Requirements:**
- **4 distinct workflow sections** displayed vertically or in cards:
  1. **Requirements Analysis** section
  2. **Project Planning** section  
  3. **Story Generation** section
  4. **Code Generation** section

**Button State Management:**
- Each section has a primary action button with dynamic states:
  - `"Start Analysis"` → `"Processing..."` → `"Pending Review"` → `"Approved ✓"`
  - `"Start Planning"` → `"Processing..."` → `"Pending Review"` → `"Approved ✓"`
  - `"Generate Stories"` → `"Processing..."` → `"Pending Review"` → `"Approved ✓"`
  - `"Generate Code"` → `"Processing..."` → `"Pending Review"` → `"Approved ✓"`

**Progressive Enablement:**
- Only Requirements Analysis starts enabled
- Each subsequent stage only enables after previous stage is approved
- Visual indicators for stage dependencies and current progress

**Real-time State Updates:**
- Page automatically detects when reviews are approved
- UI updates without manual refresh
- Workflow state persists across browser sessions

### 2. WorkflowManager Class (`js/workflow.js`)

**State Management Interface:**
```javascript
class WorkflowManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.loadState();
    }

    // Stage status management
    setRequirementsApproved(analysisId)
    setPlanningApproved(planningId)  
    setStoriesApproved(generationId)
    setCodeApproved(codeId)
    
    // Status checking
    isRequirementsApproved()
    isPlanningApproved()
    isStoriesApproved()
    isCodeApproved()
    
    // UI synchronization
    updateWorkflowUI()
    enableNextStage(stageName)
    
    // Persistence
    saveState()
    loadState()
}
```

**localStorage Schema:**
```javascript
{
    projectId: "123",
    requirements: { approved: true, analysisId: "req-456" },
    planning: { approved: true, planningId: "plan-789" },
    stories: { approved: false, generationId: "story-101" },
    code: { approved: false, codeId: null }
}
```

### 3. API Integration Layer (`js/api.js`)

**Required API Functions:**
```javascript
// Project Management
async function createProject(projectData)
async function getProject(projectId)
async function listProjects()

// Workflow Stages
async function analyzeRequirements(requirementsData)
async function createProjectPlan(planningData)
async function generateStories(storyData)
async function generateCode(codeData)

// Status Checking (returns boolean)
async function canCreateProjectPlan(requirementsAnalysisId)
async function canGenerateStories(projectPlanningId)
async function canGenerateCode(storyGenerationId)

// Review Management
async function getReview(reviewId)
async function approveReview(reviewId)
async function rejectReview(reviewId)
async function getPendingReviews()
```

**API Configuration:**
- Base URL: `http://localhost:8080/api`
- Error handling with user-friendly messages
- Loading states and timeout handling
- Response validation for both boolean and object returns

### 4. Review Queue Interface (`reviews/queue.html`)

**Review Display Requirements:**
- **Pending reviews list** with expandable content sections
- **AI-generated content preview** with formatted display
- **Original request context** for review reference
- **Approve/Reject buttons** with immediate visual feedback

**User Experience Flow:**
1. Display all pending reviews with service type indicators
2. Show generated content in readable format (preserve markdown formatting)
3. Provide approve/reject actions with loading states
4. Update UI immediately after approval (disable buttons, show success)
5. Auto-refresh pending list after actions
6. Provide navigation back to workflow

## User Workflow to Support

### Complete Journey Flow:
1. **Project Creation**: User creates project → gets project ID → redirected to workflow
2. **Requirements Stage**: Click "Start Analysis" → AI processes → redirect to review queue
3. **Review Process**: User reviews AI output → approves → manually returns to workflow
4. **State Update**: Workflow page detects approval → enables "Start Planning" button
5. **Repeat Pattern**: Same flow for Planning → Stories → Code generation stages

### State Synchronization Requirements:
- **Cross-page persistence**: Workflow state survives navigation and browser refresh
- **Automatic detection**: Page load checks for newly approved reviews
- **UI responsiveness**: Immediate feedback for all user actions
- **Error resilience**: Workflow continues even if status checks fail

## Backend API Reference

### Available Endpoints:
```
POST /api/projects                    # Create project
GET  /api/projects                    # List projects
GET  /api/projects/{id}               # Get project

POST /api/requirements/analyze        # Submit requirements analysis
GET  /api/requirements/{id}/status    # Check analysis status

POST /api/planning/create             # Create project plan
GET  /api/planning/{id}/status        # Check planning status
GET  /api/planning/can-create/{reqId} # Check if planning can be created

POST /api/stories/generate            # Generate user stories
GET  /api/stories/{id}/status         # Check generation status

POST /api/code/generate               # Generate code
GET  /api/code/{id}/status            # Check generation status

POST /api/review/submit               # Submit for review
GET  /api/review/{id}                 # Get review details
POST /api/review/{id}/approve         # Approve review
POST /api/review/{id}/reject          # Reject review
GET  /api/review/pending              # Get pending reviews
```

### Request/Response Formats:
- **Project Creation**: `{name: string, description: string}`
- **Requirements Analysis**: `{projectId: string, description: string, context?: string}`
- **Review Approval**: Returns `{success: boolean, message: string}`

## Technical Implementation Notes

### JavaScript Architecture:
- **Global scope**: Functions available via `window` object to avoid module issues
- **Async/await**: All API calls use proper async patterns with error handling
- **Event-driven**: UI updates triggered by state changes
- **Responsive**: Loading states and user feedback for all actions

### Error Handling Strategy:
- **API failures**: Graceful degradation with user notifications
- **Network issues**: Retry logic and timeout handling
- **State inconsistencies**: Validation and recovery mechanisms
- **User guidance**: Clear messaging for required actions

### Performance Considerations:
- **Lazy loading**: Only load necessary data when accessed
- **Caching**: Store API responses to minimize backend calls
- **Debouncing**: Prevent rapid API calls from UI interactions
- **Memory management**: Clean up event listeners and timers

## Success Criteria

### Functional Requirements:
✅ **Complete 4-stage workflow** that progresses logically through pipeline  
✅ **State persistence** across browser sessions and page navigation  
✅ **Review approval workflow** with content display and action buttons  
✅ **Real-time UI updates** reflecting current workflow state  
✅ **Error resilience** with graceful handling of API failures  

### User Experience Goals:
✅ **Intuitive progression** with clear visual indicators of current stage  
✅ **Immediate feedback** for all user actions and state changes  
✅ **Seamless navigation** between workflow and review interfaces  
✅ **Content visibility** showing what AI generated at each stage  

## Implementation Priority

1. **Phase 1**: Basic workflow.html with 4 sections and static UI
2. **Phase 2**: WorkflowManager class with state management and localStorage
3. **Phase 3**: API integration layer with all backend endpoints
4. **Phase 4**: Review queue interface with approval workflow
5. **Phase 5**: State synchronization and cross-page navigation
6. **Phase 6**: UI polish, error handling, and user experience refinements

## Developer Learning Objectives

This rebuild serves as **interview preparation** for senior .NET developer roles. Focus areas:
- **Code evaluation skills**: Systematically assess AI-generated frontend code quality
- **State management patterns**: Evaluate localStorage vs in-memory vs server-side state
- **API integration architectures**: Review error handling, async patterns, response processing
- **User experience design**: Assess workflow progression and feedback mechanisms
- **Enterprise frontend patterns**: Examine separation of concerns and maintainability

Key Sections:

Complete application architecture with file structure
Detailed component specifications for each major piece
WorkflowManager class requirements with state management
API integration layer with all required endpoints
User workflow documentation showing the complete journey
Backend API reference for integration
Implementation priority for systematic rebuilding

Critical Elements Captured:

4-stage workflow interface with progressive enablement
State persistence across browser sessions
Review approval workflow with content display
Cross-page navigation and state synchronization
Real-time UI updates without manual refresh

Build this as **production-quality code** that demonstrates enterprise frontend development skills.
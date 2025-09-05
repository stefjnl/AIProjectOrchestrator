# AI Project Orchestrator Frontend Implementation

## What Was Working Before

I had a complete 4-stage workflow frontend with:
- Project creation and management
- Requirements Analysis → Project Planning → User Stories → Code Generation pipeline
- Manual review/approval system between each stage
- Complete state management with localStorage persistence
- Full API integration with error handling
- Working review queue interface (partially preserved)

## Current Infrastructure (Keep As-Is)

**Docker Setup:** Backend on port 8086, frontend on 8087
**Nginx Config:** Working CORS headers for API communication
**File Structure:** 
```
frontend/
├── css/
├── js/
├── projects/
├── reviews/
│   └── queue.html (partially preserved)
└── index.html
```

**Preserved File:** `/reviews/queue.html` exists but needs API namespace fixes

## Implementation Requirements

### 1. Core API Client (`/js/api.js`)

Create a global APIClient with these exact endpoints:

```javascript
window.APIClient = {
    baseUrl: 'http://localhost:8086/api',
    
    // Projects
    async createProject(projectData) // POST /projects
    async getProjects() // GET /projects  
    async getProject(id) // GET /projects/{id}
    
    // Requirements Analysis
    async analyzeRequirements(request) // POST /requirements/analyze
    // Request: { projectId, projectDescription, context, constraints }
    
    // Project Planning  
    async canCreateProjectPlan(analysisId) // GET /projectplanning/can-create/{id}
    async createProjectPlan(request) // POST /projectplanning/create
    // Request: { requirementsAnalysisId, preferences }
    
    // Story Generation
    async canGenerateStories(planningId) // GET /stories/can-generate/{id} 
    async generateStories(request) // POST /stories/generate
    // Request: { planningId }
    
    // Code Generation
    async canGenerateCode(storyGenId) // GET /code/can-generate/{id}
    async generateCode(request) // POST /code/generate
    // Request: { storyGenerationId }
    
    // Reviews
    async getPendingReviews() // GET /review/pending
    async approveReview(reviewId) // POST /review/{id}/approve
    async rejectReview(reviewId, feedback) // POST /review/{id}/reject
    async getReview(reviewId) // GET /review/{id}
}
```

### 2. Workflow State Manager (`/js/workflow.js`)

```javascript
class WorkflowManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.storageKey = `workflow_${projectId}`;
        this.state = {
            requirementsAnalysisId: null,
            projectPlanningId: null, 
            storyGenerationId: null,
            codeGenerationId: null,
            requirementsApproved: false,
            planningApproved: false,
            storiesApproved: false,
            requirementsPending: false,
            planningPending: false,
            storiesPending: false
        };
    }
    
    // State persistence
    saveState() // localStorage
    loadState() // localStorage
    
    // ID tracking
    setRequirementsAnalysisId(id)
    setProjectPlanningId(id) 
    setStoryGenerationId(id)
    setCodeGenerationId(id)
    
    // Approval tracking
    setRequirementsApproved(approved)
    setPlanningApproved(approved)
    setStoriesApproved(approved)
    
    // Pending flags
    setRequirementsPending(pending)
    setPlanningPending(pending) 
    setStoriesPending(pending)
    
    // UI updates
    updateWorkflowUI() // Update button states and status text
    
    // Status checking
    async checkApprovedReviews() // Check if pending reviews are now approved
}
```

### 3. Core Pages

**`/index.html`** - Simple landing page with navigation to create project or view projects

**`/projects/create.html`** - Form with project name and description, redirects to workflow on creation

**`/projects/workflow.html`** - Main workflow interface with:
- 4 stage sections (Requirements, Planning, Stories, Code)
- Each stage shows status: "Not Started", "Pending Review", "Approved"
- Each stage has action button, disabled until previous stage approved
- Automatic state checking on page load
- Redirect to review queue after stage submission

### 4. Critical Implementation Details

**State Flow:**
1. User clicks stage button → API call → Store ID → Set pending flag → Redirect to review queue
2. User approves in review queue → Returns to workflow page 
3. Page load → Check localStorage flags → Verify approval status → Update UI → Enable next stage

**Error Handling:**
- API call failures with user-friendly messages
- Prerequisite checking before stage progression
- Graceful fallback for network issues

**UI Updates:**
- Button enable/disable based on stage dependencies
- Status text updates ("Not Started" → "Pending Review" → "Approved")
- Loading states during API calls

## Fix Required for Existing File

Update `/reviews/queue.html` to use `window.APIClient` namespace:
- Change `getPendingReviews()` to `window.APIClient.getPendingReviews()`
- Change `approveReview(reviewId)` to `window.APIClient.approveReview(reviewId)`  
- Change `rejectReview(reviewId, { reason: 'User rejection', feedback: feedback })` to `window.APIClient.rejectReview(reviewId, feedback)`

## Success Criteria

The complete workflow should allow:
1. Create project → Automatic redirect to workflow page
2. Start Requirements Analysis → Redirect to review queue
3. Approve review → Return to workflow → Next stage enabled
4. Complete all 4 stages with proper state persistence
5. Handle page refreshes and browser sessions correctly

Build this as a production-ready implementation matching the detailed functionality described in my progress summaries.
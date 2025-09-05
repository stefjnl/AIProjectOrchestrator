# Phase 4: Workflow State Management and Functionality

## Objective
Add complete workflow state management, API integration for all stages, and functional buttons to create the fully working end-to-end pipeline from your original implementation.

## Context
- Phase 1-3: Working project creation and basic workflow page structure
- Need to implement the complete 4-stage pipeline with state persistence
- Must recreate the exact functionality from your progress summaries
- This is the final phase to restore full functionality

## Requirements

### Files to Create/Modify

**1. `/js/workflow.js` - WorkflowManager Class**
- Complete state management with localStorage persistence
- Methods for tracking all 4 workflow stages
- UI update functionality
- Approval status checking

**2. Modify `/projects/workflow.html`**
- Add workflow.js script
- Make all buttons functional
- Add proper event handlers
- Implement the complete workflow logic

**3. Expand `/js/api.js`**
- Add all remaining API endpoints for the complete workflow

### Technical Specifications

**API Client Expansion (`/js/api.js`):**
Add these methods to existing `window.APIClient`:
```javascript
// Requirements Analysis
async analyzeRequirements(request),
// Input: { projectId, projectDescription, context, constraints }

// Project Planning  
async canCreateProjectPlan(analysisId),
async createProjectPlan(request),
// Input: { requirementsAnalysisId, preferences }

// Story Generation
async canGenerateStories(planningId),
async generateStories(request),
// Input: { planningId }

// Code Generation
async canGenerateCode(storyGenId),
async generateCode(request),
// Input: { storyGenerationId }

// Reviews
async getPendingReviews(),
async approveReview(reviewId),
async rejectReview(reviewId, feedback),
async getReview(reviewId)
```

**WorkflowManager Class (`/js/workflow.js`):**
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
    saveState()
    loadState()
    
    // ID storage methods
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
    updateWorkflowUI()
    
    // Review status checking
    async checkApprovedReviews()
}
```

### Workflow Stage Functions

**Complete workflow.html functionality:**
```javascript
// Global workflow manager
let workflowManager;

// Page initialization
window.addEventListener('DOMContentLoaded', async function() {
    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('projectId');
    
    workflowManager = new WorkflowManager(projectId);
    workflowManager.loadState();
    await workflowManager.checkApprovedReviews();
    workflowManager.updateWorkflowUI();
    
    // Load and display project
    const project = await window.APIClient.getProject(projectId);
    displayProjectInfo(project);
});

// Stage 1: Requirements Analysis
async function startRequirementsAnalysis() {
    try {
        const project = await window.APIClient.getProject(workflowManager.projectId);
        const request = {
            projectId: workflowManager.projectId,
            projectDescription: project.description,
            context: "",
            constraints: []
        };
        
        const response = await window.APIClient.analyzeRequirements(request);
        workflowManager.setRequirementsAnalysisId(response.analysisId);
        workflowManager.setRequirementsPending(true);
        workflowManager.saveState();
        workflowManager.updateWorkflowUI();
        
        alert('Requirements analysis submitted for review.');
        window.location.href = '/reviews/queue.html';
    } catch (error) {
        alert('Error: ' + error.message);
    }
}

// Stage 2: Project Planning
async function startProjectPlanning() {
    try {
        const canCreate = await window.APIClient.canCreateProjectPlan(workflowManager.state.requirementsAnalysisId);
        if (!canCreate) {
            alert('Requirements must be approved first.');
            return;
        }
        
        const request = {
            requirementsAnalysisId: workflowManager.state.requirementsAnalysisId,
            preferences: {}
        };
        
        const response = await window.APIClient.createProjectPlan(request);
        workflowManager.setProjectPlanningId(response.planningId);
        workflowManager.setPlanningPending(true);
        workflowManager.saveState();
        workflowManager.updateWorkflowUI();
        
        alert('Project planning submitted for review.');
        window.location.href = '/reviews/queue.html';
    } catch (error) {
        alert('Error: ' + error.message);
    }
}

// Stage 3: Story Generation  
async function startStoryGeneration() {
    try {
        const canGenerate = await window.APIClient.canGenerateStories(workflowManager.state.projectPlanningId);
        if (!canGenerate) {
            alert('Project planning must be approved first.');
            return;
        }
        
        const request = {
            planningId: workflowManager.state.projectPlanningId
        };
        
        const response = await window.APIClient.generateStories(request);
        workflowManager.setStoryGenerationId(response.generationId);
        workflowManager.setStoriesPending(true);
        workflowManager.saveState();
        workflowManager.updateWorkflowUI();
        
        alert('User stories submitted for review.');
        window.location.href = '/reviews/queue.html';
    } catch (error) {
        alert('Error: ' + error.message);
    }
}

// Stage 4: Code Generation
async function startCodeGeneration() {
    try {
        const canGenerate = await window.APIClient.canGenerateCode(workflowManager.state.storyGenerationId);
        if (!canGenerate) {
            alert('User stories must be approved first.');
            return;
        }
        
        const request = {
            storyGenerationId: workflowManager.state.storyGenerationId
        };
        
        const response = await window.APIClient.generateCode(request);
        workflowManager.setCodeGenerationId(response.generationId);
        workflowManager.saveState();
        workflowManager.updateWorkflowUI();
        
        alert('Code generation submitted for review.');
        window.location.href = '/reviews/queue.html';
    } catch (error) {
        alert('Error: ' + error.message);
    }
}
```

### UI State Management Logic

**updateWorkflowUI() Requirements:**
- Update status text: "Not Started" → "Pending Review" → "Approved"
- Enable/disable buttons based on dependencies
- Show loading states during API calls
- Visual feedback for current stage

**checkApprovedReviews() Logic:**
- Check localStorage pending flags
- If stage was pending, verify if now approved
- Update approval status and enable next stage
- Clear pending flags when approved

### Success Criteria

**Complete User Flow:**
1. Create project → Workflow page loads
2. Start Requirements Analysis → Redirect to review queue
3. Approve review → Return to workflow → Planning enabled
4. Start Project Planning → Redirect to review queue  
5. Approve review → Return to workflow → Stories enabled
6. Start Story Generation → Redirect to review queue
7. Approve review → Return to workflow → Code enabled
8. Start Code Generation → Complete workflow

**State Persistence:**
- Workflow state survives page refreshes
- Correct stage progression after approvals
- Proper button enable/disable logic
- Accurate status displays

## Constraints

- **Build on existing files** - Extend Phase 1-3 work, don't replace
- **Match original functionality** - Recreate exact workflow from summaries
- **Complete integration** - Must work with existing review queue
- **Error handling** - Robust error handling throughout
- **State management** - Proper localStorage persistence

The application should be able to start and run via the existing docker-compose.yml file (do not edit)
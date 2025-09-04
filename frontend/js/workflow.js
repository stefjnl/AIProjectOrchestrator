// Workflow state management for AI Project Orchestrator

class WorkflowManager {
    constructor() {
        this.projectId = null;
        this.workflowState = {
            requirements: {
                analysisId: null,
                status: 'not_started',
                reviewId: null
            },
            planning: {
                planningId: null,
                status: 'not_started',
                reviewId: null
            },
            stories: {
                generationId: null,
                status: 'not_started',
                reviewId: null
            },
            code: {
                generationId: null,
                status: 'not_started',
                reviewId: null
            }
        };
    }
    
    setProjectId(projectId) {
        this.projectId = projectId;
        this.loadState();
    }
    
    loadState() {
        if (!this.projectId) return;
        
        try {
            const savedState = localStorage.getItem(`workflow_${this.projectId}`);
            if (savedState) {
                this.workflowState = JSON.parse(savedState);
            }
        } catch (error) {
            console.error('Error loading workflow state:', error);
        }
    }
    
    saveState() {
        if (!this.projectId) return;
        
        try {
            localStorage.setItem(`workflow_${this.projectId}`, JSON.stringify(this.workflowState));
        } catch (error) {
            console.error('Error saving workflow state:', error);
        }
    }
    
    // Requirements methods
    setRequirementsAnalysis(analysisId, reviewId) {
        this.workflowState.requirements.analysisId = analysisId;
        this.workflowState.requirements.reviewId = reviewId;
        this.workflowState.requirements.status = 'pending_review';
        this.saveState();
    }
    
    getRequirementsAnalysisId() {
        return this.workflowState.requirements.analysisId;
    }
    
    setRequirementsApproved() {
        this.workflowState.requirements.status = 'approved';
        this.saveState();
    }
    
    areRequirementsApproved() {
        return this.workflowState.requirements.status === 'approved';
    }
    
    // Planning methods
    setProjectPlanning(planningId, reviewId) {
        this.workflowState.planning.planningId = planningId;
        this.workflowState.planning.reviewId = reviewId;
        this.workflowState.planning.status = 'pending_review';
        this.saveState();
    }
    
    getProjectPlanningId() {
        return this.workflowState.planning.planningId;
    }
    
    setPlanningApproved() {
        this.workflowState.planning.status = 'approved';
        this.saveState();
    }
    
    canStartPlanning() {
        return this.workflowState.requirements.status === 'pending_review' || 
               this.workflowState.requirements.status === 'approved';
    }
    
    // Stories methods
    setStoryGeneration(generationId, reviewId) {
        this.workflowState.stories.generationId = generationId;
        this.workflowState.stories.reviewId = reviewId;
        this.workflowState.stories.status = 'pending_review';
        this.saveState();
    }
    
    getStoryGenerationId() {
        return this.workflowState.stories.generationId;
    }
    
    setStoriesApproved() {
        this.workflowState.stories.status = 'approved';
        this.saveState();
    }
    
    canGenerateStories() {
        return this.workflowState.planning.status === 'pending_review' || 
               this.workflowState.planning.status === 'approved';
    }
    
    // Code methods
    setCodeGeneration(generationId, reviewId) {
        this.workflowState.code.generationId = generationId;
        this.workflowState.code.reviewId = reviewId;
        this.workflowState.code.status = 'pending_review';
        this.saveState();
    }
    
    getCodeGenerationId() {
        return this.workflowState.code.generationId;
    }
    
    setCodeApproved() {
        this.workflowState.code.status = 'approved';
        this.saveState();
    }
    
    canGenerateCode() {
        return this.workflowState.stories.status === 'pending_review' || 
               this.workflowState.stories.status === 'approved';
    }
    
    // Update UI based on current state
    updateWorkflowUI() {
        // Update requirements status
        const requirementsStatus = document.getElementById('requirements-status');
        if (requirementsStatus) {
            if (this.workflowState.requirements.status === 'pending_review') {
                requirementsStatus.textContent = 'Pending Review';
                requirementsStatus.className = 'stage-status status-pending';
            } else if (this.workflowState.requirements.status === 'approved') {
                requirementsStatus.textContent = 'Approved';
                requirementsStatus.className = 'stage-status status-approved';
            }
        }
        
        // Update planning status and button
        const planningStatus = document.getElementById('planning-status');
        const planningButton = document.getElementById('planning-stage')?.querySelector('button');
        if (planningStatus) {
            if (this.workflowState.planning.status === 'pending_review') {
                planningStatus.textContent = 'Pending Review';
                planningStatus.className = 'stage-status status-pending';
                if (planningButton) planningButton.disabled = false;
            } else if (this.workflowState.planning.status === 'approved') {
                planningStatus.textContent = 'Approved';
                planningStatus.className = 'stage-status status-approved';
                if (planningButton) planningButton.disabled = false;
            } else if (this.canStartPlanning()) {
                planningStatus.textContent = 'Not Started';
                planningStatus.className = 'stage-status';
                if (planningButton) planningButton.disabled = false;
            } else {
                planningStatus.textContent = 'Not Started';
                planningStatus.className = 'stage-status';
                if (planningButton) planningButton.disabled = true;
            }
        }
        
        // Update stories status and button
        const storiesStatus = document.getElementById('stories-status');
        const storiesButton = document.getElementById('stories-stage')?.querySelector('button');
        if (storiesStatus) {
            if (this.workflowState.stories.status === 'pending_review') {
                storiesStatus.textContent = 'Pending Review';
                storiesStatus.className = 'stage-status status-pending';
                if (storiesButton) storiesButton.disabled = false;
            } else if (this.workflowState.stories.status === 'approved') {
                storiesStatus.textContent = 'Approved';
                storiesStatus.className = 'stage-status status-approved';
                if (storiesButton) storiesButton.disabled = false;
            } else if (this.canGenerateStories()) {
                storiesStatus.textContent = 'Not Started';
                storiesStatus.className = 'stage-status';
                if (storiesButton) storiesButton.disabled = false;
            } else {
                storiesStatus.textContent = 'Not Started';
                storiesStatus.className = 'stage-status';
                if (storiesButton) storiesButton.disabled = true;
            }
        }
        
        // Update code status and button
        const codeStatus = document.getElementById('code-status');
        const codeButton = document.getElementById('code-stage')?.querySelector('button');
        if (codeStatus) {
            if (this.workflowState.code.status === 'pending_review') {
                codeStatus.textContent = 'Pending Review';
                codeStatus.className = 'stage-status status-pending';
                if (codeButton) codeButton.disabled = false;
            } else if (this.workflowState.code.status === 'approved') {
                codeStatus.textContent = 'Approved';
                codeStatus.className = 'stage-status status-approved';
                if (codeButton) codeButton.disabled = false;
            } else if (this.canGenerateCode()) {
                codeStatus.textContent = 'Not Started';
                codeStatus.className = 'stage-status';
                if (codeButton) codeButton.disabled = false;
            } else {
                codeStatus.textContent = 'Not Started';
                codeStatus.className = 'stage-status';
                if (codeButton) codeButton.disabled = true;
            }
        }
    }
}

// Create a global instance
const workflowManager = new WorkflowManager();
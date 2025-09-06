class WorkflowManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.storageKey = `workflow_${projectId}`;
        this.state = {
            version: 1, // State version for validation
            apiFailures: 0, // Circuit breaker counter
            enableDynamicStage4: true, // Feature flag for Task 1
            enableStoriesMVP: true, // Feature flag for Task 2
            // Existing state fields
            requirementsAnalysisId: null,
            projectPlanningId: null,
            storyGenerationId: null,
            codeGenerationId: null,
            requirementsApproved: false,
            planningApproved: false,
            storiesApproved: false,
            requirementsPending: false,
            planningPending: false,
            storiesPending: false,
            // Track when stages are being generated
            requirementsGenerating: false,
            planningGenerating: false,
            storiesGenerating: false,
            codeGenerating: false,
            // Phase 4: Story-level prompt tracking
            storyPrompts: {}, // { storyIndex: { promptId, status, pending, approved } }
            approvedStories: [] // Cache of approved story data
        };
        this.isUpdating = false; // Lock for async state updates

        // Periodically check for approved reviews to update the workflow state
        setInterval(() => this.checkApprovedStatus(), 5000); // 5 seconds

        // Phase 3: Enhanced state synchronization
        this.startStatusPolling();
    }

    // Phase 3: Enhanced state synchronization
    async syncWithStoriesPage() {
        // Sync data when returning from stories page
        const urlParams = new URLSearchParams(window.location.search);
        const fromStories = urlParams.get('fromStories');
        
        if (fromStories === 'true') {
            // User returned from stories page - refresh all status
            await this.checkStoryPromptApprovals();
            this.updateWorkflowUI();
            
            // Clean URL without reloading
            const cleanUrl = window.location.pathname + '?projectId=' + this.projectId;
            window.history.replaceState({}, document.title, cleanUrl);
            
            // Show success message if appropriate
            this.showReturnMessage();
        }
    }
    
    showReturnMessage() {
        const approvedPrompts = Object.values(this.state.storyPrompts || {})
            .filter(prompt => prompt.approved).length;
        const totalStories = this.state.approvedStories?.length || 0;
        
        if (approvedPrompts > 0) {
            const message = totalStories === approvedPrompts 
                ? `All ${totalStories} prompts are approved! You can now continue to code generation.`
                : `${approvedPrompts} of ${totalStories} prompts approved. Continue managing prompts or generate more.`;
            
            this.showTemporaryMessage(message, 'success');
        }
    }
    
    showTemporaryMessage(message, type = 'info') {
        // Create temporary notification
        const notification = document.createElement('div');
        notification.className = `temp-notification ${type}`;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 5000);
    }
    
    // Enhanced progress tracking
    getPromptCompletionProgress() {
        const totalStories = this.state.approvedStories?.length || 0;
        const storyPrompts = this.state.storyPrompts || {};
        
        let generated = 0, pending = 0, approved = 0, rejected = 0;
        
        Object.values(storyPrompts).forEach(prompt => {
            if (prompt.promptId) generated++;
            if (prompt.pending) pending++;
            if (prompt.approved) approved++;
            if (!prompt.approved && !prompt.pending && prompt.promptId) rejected++;
        });
        
        return {
            total: totalStories,
            generated,
            pending,
            approved,
            rejected,
            notStarted: totalStories - generated,
            percentComplete: totalStories > 0 ? Math.round((approved / totalStories) * 100) : 0
        };
    }
    
    // Real-time status polling for active sessions
    startStatusPolling() {
        if (this.statusPollingInterval) {
            clearInterval(this.statusPollingInterval);
        }
        
        // Poll every 30 seconds when on stories management or workflow page
        this.statusPollingInterval = setInterval(async () => {
            const progress = this.getPromptCompletionProgress();
            if (progress.pending > 0) {
                await this.checkStoryPromptApprovals();
                this.updateWorkflowUI();
                
                // Notify if any prompts were just approved
                this.checkForNewApprovals();
            }
        }, 30000);
    }
    
    stopStatusPolling() {
        if (this.statusPollingInterval) {
            clearInterval(this.statusPollingInterval);
            this.statusPollingInterval = null;
        }
    }
    
    checkForNewApprovals() {
        const currentProgress = this.getPromptCompletionProgress();
        const lastProgress = this.state.lastKnownProgress || { approved: 0 };
        
        if (currentProgress.approved > lastProgress.approved) {
            const newApprovals = currentProgress.approved - lastProgress.approved;
            this.showTemporaryMessage(
                `${newApprovals} new prompt${newApprovals > 1 ? 's' : ''} approved!`, 
                'success'
            );
        }
        
        this.state.lastKnownProgress = currentProgress;
        this.saveState();
    }

    // Async state update with lock to prevent races
    async updateState(updater) {
        if (this.isUpdating) {
            console.log('State update locked - skipping');
            return false;
        }
        this.isUpdating = true;
        try {
            await updater();
            this.saveState();
            return true;
        } finally {
            this.isUpdating = false;
        }
    }

    saveState() {
        localStorage.setItem(this.storageKey, JSON.stringify(this.state));
    }

    loadState() {
        const savedState = localStorage.getItem(this.storageKey);
        if (savedState) {
            try {
                const parsed = JSON.parse(savedState);
                // Validate version and structure
                if (!parsed.version || parsed.version < 1) {
                    throw new Error('Invalid state version');
                }
                // Merge with defaults, preserving new fields
                this.state = { ...this.state, ...parsed };
            } catch (error) {
                console.warn('Corrupted state detected:', error.message);
                if (confirm('Workflow state appears corrupted. Reset to defaults?')) {
                    this.resetState();
                } else {
                    // Fallback to in-memory defaults
                    this.state = { ...this.state };
                }
            }
        }

        // Fix for stale state: if pending but no ID, reset to initial
        if (!this.state.requirementsAnalysisId && this.state.requirementsPending) {
            this.setRequirementsPending(false);
            // Clear other progress if no ID
            this.state.requirementsAnalysisId = null;
            this.saveState();
        }
        // Extend to other stages if needed, but focus on requirements for now

        // Circuit breaker auto-reset
        if (this.state.apiFailures > 0) {
            setTimeout(() => {
                this.state.apiFailures = 0;
                this.saveState();
                console.log('Circuit breaker reset');
            }, 60000); // 1 minute
        }
    }

    setRequirementsAnalysisId(id) {
        this.state.requirementsAnalysisId = id;
    }

    setProjectPlanningId(id) {
        this.state.projectPlanningId = id;
    }

    setStoryGenerationId(id) {
        this.state.storyGenerationId = id;
    }

    setCodeGenerationId(id) {
        this.state.codeGenerationId = id;
    }

    setRequirementsApproved(approved) {
        this.state.requirementsApproved = approved;
        if (approved) this.state.requirementsPending = false;
    }

    setPlanningApproved(approved) {
        this.state.planningApproved = approved;
        if (approved) this.state.planningPending = false;
    }

    setStoriesApproved(approved) {
        this.state.storiesApproved = approved;
        if (approved) this.state.storiesPending = false;
    }

    setRequirementsPending(pending) {
        this.state.requirementsPending = pending;
    }

    setPlanningPending(pending) {
        this.state.planningPending = pending;
    }

    setStoriesPending(pending) {
        this.state.storiesPending = pending;
    }

    // Setter methods for generating states
    setRequirementsGenerating(generating) {
        this.state.requirementsGenerating = generating;
    }

    setPlanningGenerating(generating) {
        this.state.planningGenerating = generating;
    }

    setStoriesGenerating(generating) {
        this.state.storiesGenerating = generating;
    }

    setCodeGenerating(generating) {
        this.state.codeGenerating = generating;
    }

    // Phase 4: Story-level prompt management
    setStoryPromptId(storyIndex, promptId) {
        if (!this.state.storyPrompts[storyIndex]) {
            this.state.storyPrompts[storyIndex] = {};
        }
        this.state.storyPrompts[storyIndex].promptId = promptId;
        this.state.storyPrompts[storyIndex].pending = true;
        this.state.storyPrompts[storyIndex].approved = false;
        this.saveState();
    }

    // Removed: No longer needed for simplified navigation
    // setPendingRedirectToStories(pending) {
    //     this.state.pendingRedirectToStories = pending;
    //     this.saveState();
    // }

    setStoryPromptApproved(storyIndex, approved) {
        if (this.state.storyPrompts[storyIndex]) {
            this.state.storyPrompts[storyIndex].approved = approved;
            this.state.storyPrompts[storyIndex].pending = false;
            this.saveState();
        }
    }

    getStoryPromptStatus(storyIndex) {
        const prompt = this.state.storyPrompts[storyIndex];
        if (!prompt) return 'Not Started';
        if (prompt.pending) return 'Pending Review';
        if (prompt.approved) return 'Approved';
        return 'Rejected';
    }

    // Check approval status for all story prompts
    async checkStoryPromptApprovals() {
        for (const storyIndex in this.state.storyPrompts) {
            const prompt = this.state.storyPrompts[storyIndex];
            if (prompt.pending && prompt.promptId) {
                try {
                    const review = await window.APIClient.getReview(prompt.promptId);
                    if (review.status === 'Approved') {
                        this.setStoryPromptApproved(storyIndex, true);
                    } else if (review.status === 'Rejected') {
                        this.setStoryPromptApproved(storyIndex, false);
                    }
                } catch (error) {
                    console.log(`No review found for prompt ${prompt.promptId}`);
                }
            }
        }
    }

    // Enhanced UI update to include Phase 4
    updateStoryPromptUI() {
        const storiesContainer = document.getElementById('storiesContainer');
        if (!storiesContainer) return;

        this.state.approvedStories.forEach((story, index) => {
            const promptBtn = document.getElementById(`generatePrompt-${index}`);
            const statusDiv = document.getElementById(`promptStatus-${index}`);

            if (promptBtn && statusDiv) {
                const status = this.getStoryPromptStatus(index);
                statusDiv.textContent = status;
                statusDiv.className = `prompt-status ${status.toLowerCase().replace(' ', '-')}`;

                // Update button state based on status
                if (status === 'Not Started') {
                    promptBtn.disabled = false;
                    promptBtn.textContent = 'Generate Prompt';
                    promptBtn.onclick = () => generateStoryPrompt(index);
                } else if (status === 'Pending Review') {
                    promptBtn.disabled = true;
                    promptBtn.textContent = 'Generating...';
                } else if (status === 'Approved') {
                    promptBtn.disabled = false;
                    promptBtn.textContent = 'View Prompt';
                    promptBtn.onclick = () => viewPrompt(index);
                } else if (status === 'Rejected') {
                    promptBtn.disabled = false;
                    promptBtn.textContent = 'Retry';
                    promptBtn.onclick = () => generateStoryPrompt(index);
                }
            }
        });
    }

    // Reset workflow state for new/fresh start
    resetState() {
        localStorage.removeItem(this.storageKey);
        this.state = {
            version: 1,
            apiFailures: 0,
            enableDynamicStage4: true,
            enableStoriesMVP: true,
            requirementsAnalysisId: null,
            projectPlanningId: null,
            storyGenerationId: null,
            codeGenerationId: null,
            requirementsApproved: false,
            planningApproved: false,
            storiesApproved: false,
            requirementsPending: false,
            planningPending: false,
            storiesPending: false,
            requirementsGenerating: false,
            planningGenerating: false,
            storiesGenerating: false,
            codeGenerating: false,
            storyPrompts: {},
            approvedStories: []
        };
        this.isUpdating = false;
        this.saveState();
        this.updateWorkflowUI();
    }

    // Circuit breaker check
    isApiDisabled() {
        return this.state.apiFailures >= 3;
    }

    // Increment failure and check breaker
    recordApiFailure() {
        this.state.apiFailures++;
        if (this.isApiDisabled()) {
            console.warn('API circuit breaker tripped');
            // Show maintenance banner (page-specific implementation needed)
            if (typeof showMaintenanceBanner === 'function') {
                showMaintenanceBanner();
            }
        }
        this.saveState();
    }

    // Reset failure counter on success
    recordApiSuccess() {
        if (this.state.apiFailures > 0) {
            this.state.apiFailures = 0;
            this.saveState();
        }
    }

    // State cleanup for storage limits (basic)
    cleanupOldStates() {
        const keys = Object.keys(localStorage).filter(k => k.startsWith('workflow_'));
        if (keys.length > 10) {
            // Keep newest 10, remove oldest
            keys.slice(0, -10).forEach(k => localStorage.removeItem(k));
            console.log('Cleaned up old workflow states');
        }
    }

    // Add these methods to existing WorkflowManager class

    // Check if user has visited stories page
    getHasVisitedStoriesPage() {
        return this.state.hasVisitedStoriesPage || false;
    }

    setHasVisitedStoriesPage(visited) {
        this.state.hasVisitedStoriesPage = visited;
        this.saveState();
    }

    // Get overall completion status for Stage 4
    getStage4CompletionStatus() {
        if (!this.state.storyGenerationId || !this.state.storiesApproved) {
            return 'Not Available';
        }
        
        const storyPrompts = this.state.storyPrompts || {};
        const promptCount = Object.keys(storyPrompts).length;
        
        if (promptCount === 0) {
            return 'Ready'; // Can start generating prompts
        }
        
        const approvedCount = Object.values(storyPrompts)
            .filter(prompt => prompt.status === 'Approved').length;
        const pendingCount = Object.values(storyPrompts)
            .filter(prompt => prompt.status === 'PendingReview').length;
        
        if (pendingCount > 0) {
            return `Pending Review (${pendingCount})`;
        }
        
        if (approvedCount === promptCount) {
            return `Complete (${approvedCount} prompts)`;
        }
        
        return `In Progress (${approvedCount}/${promptCount} approved)`;
    }

    // Update UI to show proper Stage 4 status
    updateStage4UI() {
        const stage4Status = document.getElementById('promptStageStatus');
        const viewPromptsBtn = document.getElementById('viewAllPromptsBtn');
        const downloadBtn = document.getElementById('downloadResultsBtn');
        
        if (!stage4Status) return;
        
        const status = this.getStage4CompletionStatus();
        stage4Status.textContent = status;
        
        // Enable/disable buttons based on status
        const canViewPrompts = this.state.storiesApproved && this.state.storyGenerationId;
        const hasApprovedPrompts = status.includes('Complete') || status.includes('In Progress');
        
        if (viewPromptsBtn) {
            viewPromptsBtn.disabled = !canViewPrompts;
        }
        
        if (downloadBtn) {
            downloadBtn.disabled = !hasApprovedPrompts;
        }
    }

    // Get story prompt ID by index
    getStoryPromptId(storyIndex) {
        const prompt = this.state.storyPrompts[storyIndex];
        return prompt ? prompt.promptId : null;
    }

    // Get story prompt status by index
    getStoryPromptStatus(storyIndex) {
        const prompt = this.state.storyPrompts[storyIndex];
        if (!prompt) return 'Not Started';
        if (prompt.pending) return 'Pending Review';
        if (prompt.approved) return 'Approved';
        return 'Rejected';
    }

    updateWorkflowUI() {
        const updateStageUI = (stageName, idKey, approvedKey, pendingKey, buttonId, statusId, prevStageApprovedKey = null) => {
            const statusElement = document.getElementById(statusId);
            const buttonElement = document.getElementById(buttonId);

            // Check if this stage is currently being generated
            const generatingKey = `${stageName}Generating`;
            if (this.state[generatingKey]) {
                // Preserve the generating state
                statusElement.textContent = 'Generating...';
                statusElement.className = 'stage-status generating';
                buttonElement.disabled = true;
                return;
            }

            // Reset button state
            buttonElement.disabled = true;
            buttonElement.classList.remove('btn-primary', 'btn-success', 'btn-warning', 'btn-danger');

            if (this.state[approvedKey]) {
                statusElement.textContent = 'Approved';
                statusElement.className = 'stage-status approved';
                buttonElement.disabled = true;
            } else if (this.state[pendingKey]) {
                statusElement.textContent = 'Pending Review';
                statusElement.className = 'stage-status pending-review';
                buttonElement.disabled = true;
            } else if (this.state[idKey]) {
                statusElement.textContent = 'Generated, Awaiting Approval';
                statusElement.className = 'stage-status generated';
                buttonElement.disabled = true;
            } else {
                statusElement.textContent = 'Not Started';
                statusElement.className = 'stage-status not-started';

                // Enable button if prerequisites are met and not already approved/pending/generated
                if (prevStageApprovedKey === null || this.state[prevStageApprovedKey]) {
                    buttonElement.disabled = false;
                    buttonElement.classList.add('btn-primary');
                }
            }
        };

        updateStageUI('requirements', 'requirementsAnalysisId', 'requirementsApproved', 'requirementsPending', 'startRequirementsBtn', 'requirementsStatus');
        updateStageUI('planning', 'projectPlanningId', 'planningApproved', 'planningPending', 'startPlanningBtn', 'planningStatus', 'requirementsApproved');
        updateStageUI('stories', 'storyGenerationId', 'storiesApproved', 'storiesPending', 'startStoriesBtn', 'storiesStatus', 'planningApproved');
        updateStageUI('code', 'codeGenerationId', null, null, 'startCodeBtn', 'codeStatus', 'storiesApproved');

        // Phase 4: Update story prompt interface
        this.updateStoryPromptUI();

        // Stage 4: Simplified prompt management
        this.updateStage4UI();
    }

    updateStage4UI() {
        // Call the updateStage4UI function if it exists
        if (typeof updateStage4UI === 'function') {
            updateStage4UI();
        }
    }

        async checkApprovedStatus() {
            if (this.state.requirementsAnalysisId && !this.state.requirementsApproved) {
                try {
                    const canCreate = await window.APIClient.canCreateProjectPlan(this.state.requirementsAnalysisId);
                    if (canCreate) {
                        this.setRequirementsApproved(true);
                    }
                } catch (e) {
                    console.error(`Error checking if project plan can be created:`, e);
                }
            }

            if (this.state.projectPlanningId && !this.state.planningApproved) {
                try {
                    const canCreate = await window.APIClient.canGenerateStories(this.state.projectPlanningId);
                    if (canCreate) {
                        this.setPlanningApproved(true);
                    }
                } catch (e) {
                    console.error(`Error checking if stories can be generated:`, e);
                }
            }

            if (this.state.storyGenerationId && !this.state.storiesApproved) {
                try {
                    const canCreate = await window.APIClient.canGenerateCode(this.state.storyGenerationId);
                    if (canCreate) {
                        this.setStoriesApproved(true);
                    }
                } catch (e) {
                    console.error(`Error checking if code can be generated:`, e);
                }
            }

            // Also check for story prompt approvals if in stories management phase
            if (this.state.storiesApproved) {
                await this.checkStoryPromptApprovals();
                this.checkForNewApprovals();
            }

            this.saveState();
            this.updateWorkflowUI();
        }

        // Show notification and redirect to stories management
        async showStoriesRedirectNotification() {
            // Show notification
            const notification = document.createElement('div');
            notification.className = 'notification-toast';
            notification.textContent = 'Stories approved! Redirecting to prompt management...';
            document.body.appendChild(notification);
            
            // Auto-remove after 2 seconds
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 2000);
            
            // Redirect with source parameter
            setTimeout(() => {
                window.location.href = `stories-overview.html?projectId=${this.projectId}&source=workflow`;
            }, 2000);
        }
    }

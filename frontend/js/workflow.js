class WorkflowManager {
    constructor(projectId) {
        // Validate projectId
        if (!projectId || projectId === 'unknown' || projectId === 'null' || projectId.trim() === '') {
            console.error('Invalid projectId provided to WorkflowManager:', projectId);
            // Use a default valid ID for demo/testing
            this.projectId = 'demo';
        } else {
            this.projectId = projectId;
        }

        this.isWorkflowPage = window.location.pathname.includes('workflow.html');

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
            storyPrompts: [], // Array of { storyIndex, promptId, status, pending, approved }
            approvedStories: [] // Cache of approved story data
        };
        this.isUpdating = false; // Lock for async state updates
        this.pollingInterval = null;

        // Load initial state from API
        this.loadStateFromAPI();
    }

    // Phase 3: Enhanced state synchronization
    async syncWithStoriesPage() {
        // Sync data when returning from stories page
        const urlParams = new URLSearchParams(window.location.search);
        const fromStories = urlParams.get('fromStories');
        
        if (fromStories === 'true') {
            // User returned from stories page - refresh all status
            await this.refreshState();
            this.updateWorkflowUI();
            
            // Clean URL without reloading
            const cleanUrl = window.location.pathname + '?projectId=' + this.projectId;
            window.history.replaceState({}, document.title, cleanUrl);
            
            // Show success message if appropriate
            this.showReturnMessage();
        }
    }
    
    showReturnMessage() {
        const approvedPrompts = this.state.storyPrompts?.filter(sp => sp.approved).length || 0;
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
        const storyPrompts = this.state.storyPrompts || [];
        
        const approved = storyPrompts.filter(sp => sp.approved).length;
        const pending = storyPrompts.filter(sp => sp.pending).length;
        const total = storyPrompts.length;
        const generated = storyPrompts.filter(sp => sp.promptId).length;
        const percentage = total > 0 ? (approved / total) * 100 : 0;
        
        return { approved, pending, total, generated, percentage };
    }
    
    // API-driven state loading
    async loadStateFromAPI() {
        try {
            const response = await window.APIClient.getWorkflowStatus(this.projectId);
            this.state = {
                version: 1,
                apiFailures: 0,
                enableDynamicStage4: true,
                enableStoriesMVP: true,
                
                // Requirements Analysis
                requirementsAnalysisId: response.requirementsAnalysis?.analysisId || null,
                requirementsApproved: response.requirementsAnalysis?.isApproved || false,
                requirementsPending: response.requirementsAnalysis?.isPending || false,
                
                // Project Planning
                projectPlanningId: response.projectPlanning?.planningId || null,
                planningApproved: response.projectPlanning?.isApproved || false,
                planningPending: response.projectPlanning?.isPending || false,
                
                // Story Generation
                storyGenerationId: response.storyGeneration?.generationId || null,
                storiesApproved: response.storyGeneration?.isApproved || false,
                storiesPending: response.storyGeneration?.isPending || false,
                storyCount: response.storyGeneration?.storyCount || 0,
                
                // Prompt Generation
                storyPrompts: response.promptGeneration?.storyPrompts || [],
                promptCompletionPercentage: response.promptGeneration?.completionPercentage || 0,
                
                // Cache for UI
                approvedStories: [] // Will be loaded separately if needed
            };
            
            this.recordApiSuccess();
            return this.state;
        } catch (error) {
            console.error('Failed to load workflow state from API:', error);
            this.recordApiFailure();
            throw error;
        }
    }
    
    async refreshState() {
        await this.loadStateFromAPI();
        this.updateWorkflowUI();
    }
    
    // Poll every 10 seconds for state updates
    startPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
        }
        this.pollingInterval = setInterval(() => {
            this.refreshState().catch(console.error);
        }, 10000);
    }
    
    stopPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = null;
        }
    }
    
    checkForNewApprovals() {
        const currentProgress = this.getPromptCompletionProgress();
        const lastProgress = this.state.lastKnownProgress || { completed: 0 };
        
        if (currentProgress.completed > lastProgress.completed) {
            const newApprovals = currentProgress.completed - lastProgress.completed;
            this.showTemporaryMessage(
                `${newApprovals} new prompt${newApprovals > 1 ? 's' : ''} approved!`, 
                'success'
            );
        }
        
        this.state.lastKnownProgress = currentProgress;
        // No saveState - state updated on next refresh
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
            // No saveState - will be updated on next API refresh
            return true;
        } finally {
            this.isUpdating = false;
        }
    }

    setRequirementsAnalysisId(id) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.requirementsAnalysisId = id;
    }

    setProjectPlanningId(id) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.projectPlanningId = id;
    }

    setStoryGenerationId(id) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.storyGenerationId = id;
    }

    setCodeGenerationId(id) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.codeGenerationId = id;
    }

    setRequirementsApproved(approved) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.requirementsApproved = approved;
        if (approved) this.state.requirementsPending = false;
    }

    setPlanningApproved(approved) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.planningApproved = approved;
        if (approved) this.state.planningPending = false;
    }

    setStoriesApproved(approved) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.storiesApproved = approved;
        if (approved) this.state.storiesPending = false;
    }

    setRequirementsPending(pending) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.requirementsPending = pending;
    }

    setPlanningPending(pending) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        this.state.planningPending = pending;
    }

    setStoriesPending(pending) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
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
    setStoryPromptId(storyIndex, promptId, reviewId = null) {
        // Remove localStorage.setItem calls
        // State will be updated on next API refresh
        let storyPrompt = this.state.storyPrompts.find(sp => sp.storyIndex === storyIndex);
        if (!storyPrompt) {
            storyPrompt = { storyIndex, promptId, reviewId, pending: true, approved: false };
            this.state.storyPrompts.push(storyPrompt);
        } else {
            storyPrompt.promptId = promptId;
            storyPrompt.reviewId = reviewId;
            storyPrompt.pending = true;
            storyPrompt.approved = false;
        }
    }

    // Removed: No longer needed for simplified navigation
    // setPendingRedirectToStories(pending) {
    //     this.state.pendingRedirectToStories = pending;
    //     this.saveState();
    // }

    setStoryPromptApproved(storyIndex, approved) {
        const storyPrompt = this.state.storyPrompts.find(sp => sp.storyIndex === storyIndex);
        if (storyPrompt) {
            storyPrompt.approved = approved;
            storyPrompt.pending = false;
            // No saveState - updated on next refresh
        }
    }

    getStoryPromptStatus(storyIndex) {
        const storyPrompt = this.state.storyPrompts.find(sp => sp.storyIndex === storyIndex);
        if (!storyPrompt) return 'Not Started';
        if (storyPrompt.pending) return 'Pending Review';
        if (storyPrompt.approved) return 'Approved';
        return 'Rejected';
    }

    // Check approval status for all story prompts
    async checkStoryPromptApprovals() {
        // Now handled via API refresh - no individual checks needed
        await this.refreshState();
    }

    // Enhanced UI update to include Phase 4
    updateStoryPromptUI() {
        const storiesContainer = document.getElementById('storiesContainer');
        if (!storiesContainer) return;

        this.state.approvedStories.forEach((story, index) => {
            const promptBtn = document.getElementById(`generatePrompt-${index}`);
            const statusDiv = document.getElementById(`promptStatus-${index}`);

            if (promptBtn && statusDiv) {
                const storyPrompt = this.state.storyPrompts.find(sp => sp.storyIndex === index);
                const status = storyPrompt ? (storyPrompt.approved ? 'Approved' : storyPrompt.pending ? 'Pending Review' : 'Not Started') : 'Not Started';
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
                }
            }
        });
    }

    // Reset workflow state for new/fresh start
    async resetState() {
        // Clear any local state, but actual reset via API would require backend call
        // For now, reload from API to get fresh state
        await this.loadStateFromAPI();
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
        // No saveState - updated on next refresh
    }

    // Reset failure counter on success
    recordApiSuccess() {
        if (this.state.apiFailures > 0) {
            this.state.apiFailures = 0;
            // No saveState - updated on next refresh
        }
    }

    // Add these methods to existing WorkflowManager class

    // Check if user has visited stories page
    getHasVisitedStoriesPage() {
        return this.state.hasVisitedStoriesPage || false;
    }

    setHasVisitedStoriesPage(visited) {
        this.state.hasVisitedStoriesPage = visited;
        // No saveState - updated on next refresh
    }

    // Get overall completion status for Stage 4
    getStage4CompletionStatus() {
        if (!this.state.storyGenerationId || !this.state.storiesApproved) {
            return 'Not Started';
        }
        
        const storyPrompts = this.state.storyPrompts || [];
        const promptCount = storyPrompts.length;
        
        if (promptCount === 0) {
            return 'Ready'; // Can start generating prompts
        }
        
        const approvedCount = storyPrompts.filter(sp => sp.approved).length;
        const pendingCount = storyPrompts.filter(sp => sp.pending).length;
        
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
        
        // Update status logic: "Not Started" when stories not individually approved
        if (!this.state.storiesApproved) {
            stage4Status.textContent = "Not Started";
            stage4Status.className = "stage-status not-started";
            
            if (viewPromptsBtn) {
                viewPromptsBtn.disabled = true;
            }
            
            if (downloadBtn) {
                downloadBtn.disabled = true;
            }
            return;
        }
        
        // Check if individual stories are approved
        const storyPrompts = this.state.storyPrompts || [];
        const individualStoriesApproved = storyPrompts.filter(sp => sp.approved).length;
        
        if (individualStoriesApproved > 0) {
            stage4Status.textContent = "Ready";
            stage4Status.className = "stage-status ready";
            
            if (viewPromptsBtn) {
                viewPromptsBtn.disabled = false;
            }
            
            if (downloadBtn) {
                downloadBtn.disabled = false;
            }
        } else {
            stage4Status.textContent = "Waiting";
            stage4Status.className = "stage-status waiting";
            
            if (viewPromptsBtn) {
                viewPromptsBtn.disabled = false;
            }
            
            if (downloadBtn) {
                downloadBtn.disabled = true;
            }
        }
    }

    // Get story prompt ID by index
    getStoryPromptId(storyIndex) {
        const storyPrompt = this.state.storyPrompts.find(sp => sp.storyIndex === storyIndex);
        return storyPrompt ? storyPrompt.promptId : null;
    }

    updateWorkflowUI() {
        if (this.isWorkflowPage) {
            const updateStageUI = (stageName, idKey, approvedKey, pendingKey, buttonId, statusId, prevStageApprovedKey = null) => {
                const statusElement = document.getElementById(statusId);
                const buttonElement = document.getElementById(buttonId);

                if (!statusElement || !buttonElement) {
                    console.warn(`DOM elements for ${stageName} stage not found. This may be running on a non-workflow page.`);
                    return;
                }

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
        } else {
            console.log('updateWorkflowUI called on non-workflow page. Skipping UI updates.');
        }
    }

        async checkApprovedStatus() {
        // Now handled via polling and API refresh - no individual checks needed
        await this.refreshState();
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

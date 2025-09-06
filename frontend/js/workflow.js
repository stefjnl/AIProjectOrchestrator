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

        // Periodically check for approved reviews to update the workflow state
        setInterval(() => this.checkApprovedStatus(), 5000); // 5 seconds
    }

    saveState() {
        localStorage.setItem(this.storageKey, JSON.stringify(this.state));
    }

    loadState() {
        const savedState = localStorage.getItem(this.storageKey);
        if (savedState) {
            this.state = { ...this.state, ...JSON.parse(savedState) };
        }

        // Fix for stale state: if pending but no ID, reset to initial
        if (!this.state.requirementsAnalysisId && this.state.requirementsPending) {
            this.setRequirementsPending(false);
            // Clear other progress if no ID
            this.state.requirementsAnalysisId = null;
            this.saveState();
        }
        // Extend to other stages if needed, but focus on requirements for now
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
        this.saveState();
        this.updateWorkflowUI();
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

        await this.checkStoryPromptApprovals();

        this.saveState();
        this.updateWorkflowUI();
    }
}

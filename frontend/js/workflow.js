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
            codeGenerating: false
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

        this.saveState();
        this.updateWorkflowUI();
    }
}

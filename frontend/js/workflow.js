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

    updateWorkflowUI() {
        const updateStageUI = (stageName, idKey, approvedKey, pendingKey, buttonId, statusId, prevStageApprovedKey = null) => {
            const statusElement = document.getElementById(statusId);
            const buttonElement = document.getElementById(buttonId);

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

    async checkApprovedReviews() {
        const checkStatus = async (id, pendingFlag, approveFunc) => {
            if (this.state[pendingFlag]) {
                try {
                    // We no longer need to check the review content, just its existence.
                    await window.APIClient.getReview(this.state[id]);
                } catch (e) {
                    if (e.message && e.message.includes('404')) {
                        // If the review is not found, it means it was approved and deleted.
                        console.log(`Review for ${id} not found (404), assuming approved.`);
                        this[approveFunc](true);
                    } else {
                        console.error(`Error checking review status for ${id}:`, e);
                    }
                }
            }
        };

        await checkStatus('requirementsAnalysisId', 'requirementsPending', 'setRequirementsApproved');
        await checkStatus('projectPlanningId', 'planningPending', 'setPlanningApproved');
        await checkStatus('storyGenerationId', 'storiesPending', 'setStoriesApproved');

        this.saveState();
        this.updateWorkflowUI();
    }
}

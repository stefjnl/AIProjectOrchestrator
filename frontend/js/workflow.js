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
        const updateStageUI = (stageName, idKey, approvedKey, pendingKey, buttonId, statusId, nextStageApprovedKey = null) => {
            const statusElement = document.getElementById(statusId);
            const buttonElement = document.getElementById(buttonId);

            if (this.state[approvedKey]) {
                statusElement.textContent = 'Approved';
                statusElement.style.color = 'green';
                buttonElement.disabled = true;
            } else if (this.state[pendingKey]) {
                statusElement.textContent = 'Pending Review';
                statusElement.style.color = 'orange';
                buttonElement.disabled = true;
            } else if (this.state[idKey]) {
                statusElement.textContent = 'Generated, Awaiting Approval';
                statusElement.style.color = 'orange';
                buttonElement.disabled = true;
            } else {
                statusElement.textContent = 'Not Started';
                statusElement.style.color = '#6c757d';
                buttonElement.disabled = true; // Default to disabled
            }

            // Enable button if prerequisites are met and not already approved/pending
            if (!this.state[approvedKey] && !this.state[pendingKey] && !this.state[idKey]) {
                if (stageName === 'requirements') {
                    buttonElement.disabled = false;
                } else if (nextStageApprovedKey !== null && this.state[nextStageApprovedKey]) {
                    buttonElement.disabled = false;
                }
            }
        };

        updateStageUI('requirements', 'requirementsAnalysisId', 'requirementsApproved', 'requirementsPending', 'startRequirementsBtn', 'requirementsStatus');
        updateStageUI('planning', 'projectPlanningId', 'planningApproved', 'planningPending', 'startPlanningBtn', 'planningStatus', 'requirementsApproved');
        updateStageUI('stories', 'storyGenerationId', 'storiesApproved', 'storiesPending', 'startStoriesBtn', 'storiesStatus', 'planningApproved');
        updateStageUI('code', 'codeGenerationId', null, null, 'startCodeBtn', 'codeStatus', 'storiesApproved');
    }

    async checkApprovedReviews() {
        // This function will be called on page load to update the state based on backend reviews
        // For now, we assume the backend will eventually update the status.
        // In a real app, you might poll the backend or use websockets.
        // For this implementation, we'll rely on the user manually checking the review queue
        // and the state being updated when they return to this page.

        // If a stage was pending, check if it's now approved by the backend
        // This is a simplified check. A more robust solution would involve polling the backend
        // or having a notification system.
        if (this.state.requirementsPending) {
            try {
                const review = await window.APIClient.getReview(this.state.requirementsAnalysisId);
                if (review && review.status === 'Approved') {
                    this.setRequirementsApproved(true);
                }
            } catch (e) {
                console.error("Error checking requirements review status:", e);
            }
        }

        if (this.state.planningPending) {
            try {
                const review = await window.APIClient.getReview(this.state.projectPlanningId);
                if (review && review.status === 'Approved') {
                    this.setPlanningApproved(true);
                }
            } catch (e) {
                console.error("Error checking planning review status:", e);
            }
        }

        if (this.state.storiesPending) {
            try {
                const review = await window.APIClient.getReview(this.state.storyGenerationId);
                if (review && review.status === 'Approved') {
                    this.setStoriesApproved(true);
                }
            } catch (e) {
                console.error("Error checking stories review status:", e);
            }
        }
        this.saveState();
    }
}

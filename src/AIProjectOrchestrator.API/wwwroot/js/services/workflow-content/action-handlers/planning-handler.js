/**
 * PlanningHandler - Handles project planning actions
 */
class PlanningHandler {
    /**
     * Initialize the planning handler
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        if (!workflowManager) {
            throw new Error('WorkflowManager is required');
        }
        if (!apiClient) {
            throw new Error('APIClient is required');
        }

        this.workflowManager = workflowManager;
        this.apiClient = apiClient;
    }

    /**
     * Generate project plan
     * @returns {Promise<void>}
     */
    async generatePlan() {
        try {
            console.log('=== PlanningHandler.generatePlan called ===');

            // Check if requirements are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating a project plan.', 'warning');
                return;
            }

            // Check if planning already exists and is approved
            if (this.workflowManager.workflowState?.projectPlanning?.isApproved === true) {
                if (!confirm('Project planning is already completed. Do you want to regenerate it? This will require re-approval.')) {
                    return;
                }
            }

            const loadingOverlay = this.showLoading('Generating project plan...');
            try {
                // Get project details for planning generation
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Create the project planning request
                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: null
                };

                const result = await this.apiClient.createProjectPlan(request);

                window.App.showNotification('Project plan submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(2);

            } finally {
                this.hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to generate project plan:', error);
            window.App.showNotification(`Failed to generate plan: ${error.message || error}`, 'error');
        }
    }

    /**
     * Regenerate project plan
     * @returns {Promise<void>}
     */
    async regeneratePlan() {
        try {
            console.log('=== PlanningHandler.regeneratePlan called ===');

            // Check if planning is already approved
            if (this.workflowManager.workflowState?.projectPlanning?.isApproved === true) {
                if (!confirm('Project planning is already completed. Do you want to regenerate it? This will require re-approval.')) {
                    return;
                }
            }

            // Check if requirements are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating a project plan.', 'warning');
                return;
            }

            const loadingOverlay = this.showLoading('Generating project plan...');
            try {
                // Get project details for regeneration
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Create the project planning request for regeneration
                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: 'Regenerated plan'
                };

                const result = await this.apiClient.createProjectPlan(request);

                window.App.showNotification('Project plan submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(2);

            } finally {
                this.hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to regenerate project plan:', error);
            window.App.showNotification(`Failed to regenerate plan: ${error.message || error}`, 'error');
        }
    }

    /**
     * Show loading overlay
     * @param {string} message - Loading message
     * @returns {*} Loading overlay reference
     */
    showLoading(message) {
        if (typeof showLoading === 'function') {
            return showLoading(message);
        }
        console.log('Loading:', message);
        return null;
    }

    /**
     * Hide loading overlay
     * @param {*} overlay - Loading overlay reference
     */
    hideLoading(overlay) {
        if (typeof hideLoading === 'function' && overlay) {
            hideLoading(overlay);
        }
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PlanningHandler;
} else if (typeof window !== 'undefined') {
    window.PlanningHandler = PlanningHandler;
}
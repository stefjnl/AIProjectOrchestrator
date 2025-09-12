/**
 * RequirementsHandler - Handles requirements analysis actions
 */
class RequirementsHandler {
    /**
     * Initialize the requirements handler
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
     * Analyze requirements for the project
     * @returns {Promise<void>}
     */
    async analyzeRequirements() {
        try {
            console.log('=== RequirementsHandler.analyzeRequirements called ===');

            // Check if requirements already exist and are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved === true) {
                window.App.showNotification('Requirements analysis is already completed and approved.', 'info');
                return;
            }

            // Check if there's already a pending analysis
            if (this.workflowManager.workflowState?.requirementsAnalysis?.status === 'PendingReview') {
                window.App.showNotification('Requirements analysis is already pending review. Check the Review Queue.', 'info');
                return;
            }

            const loadingOverlay = this.showLoading('Preparing requirements analysis...');
            try {
                // Get project details to pre-populate requirements
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                let requirementsInput = '';

                // If this is a new project, suggest using the project description
                if (this.workflowManager.isNewProject && project.description) {
                    const useProjectDescription = confirm(
                        'We found your project description. Would you like to use it as a starting point for requirements analysis?\n\n' +
                        'Project Description: ' + project.description.substring(0, 200) + '...'
                    );

                    if (useProjectDescription) {
                        requirementsInput = project.description;
                    }
                }

                // If no pre-populated input, prompt user for manual input
                if (!requirementsInput) {
                    this.hideLoading(loadingOverlay);

                    // Show a prompt for manual requirements input
                    requirementsInput = prompt('Please describe your project requirements:\n\n' +
                        'What problem are you trying to solve? What features do you need? ' +
                        'What technology constraints do you have?');

                    // If user cancels the prompt, don't proceed
                    if (!requirementsInput) {
                        window.App.showNotification('Requirements analysis cancelled. You can try again later.', 'info');
                        return;
                    }

                    // Re-show loading overlay since we're proceeding
                    loadingOverlay = this.showLoading('Preparing requirements analysis...');
                }

                // Create the requirements analysis request
                const request = {
                    ProjectDescription: requirementsInput,
                    ProjectId: this.workflowManager.projectId,
                    AdditionalContext: project.techStack ? `Tech Stack: ${project.techStack}` : null,
                    Constraints: project.timeline ? `Timeline: ${project.timeline}` : null
                };

                const result = await this.apiClient.analyzeRequirements(request);

                window.App.showNotification('Requirements submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(1);

            } finally {
                this.hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to analyze requirements:', error);
            window.App.showNotification(`Failed to analyze requirements: ${error.message || error}`, 'error');
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
    module.exports = RequirementsHandler;
} else if (typeof window !== 'undefined') {
    window.RequirementsHandler = RequirementsHandler;
}
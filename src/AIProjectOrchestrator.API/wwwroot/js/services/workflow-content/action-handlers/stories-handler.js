/**
 * StoriesHandler - Handles user stories actions
 */
class StoriesHandler {
    /**
     * Initialize the stories handler
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
     * Generate user stories
     * @returns {Promise<void>}
     */
    async generateStories() {
        try {
            console.log('=== StoriesHandler.generateStories called ===');

            // Check if stories are already approved
            if (this.workflowManager.workflowState?.storyGeneration?.isApproved === true) {
                if (!confirm('User stories are already completed. Do you want to regenerate them? This will require re-approval.')) {
                    return;
                }
            }

            // Check if requirements and planning are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating user stories.', 'warning');
                return;
            }

            if (this.workflowManager.workflowState?.projectPlanning?.isApproved !== true) {
                window.App.showNotification('You must complete Project Planning before generating user stories.', 'warning');
                return;
            }

            const loadingOverlay = this.showLoading('Generating user stories...');
            try {
                // Get project details for story generation
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Validate that we have required IDs before proceeding
                if (!this.workflowManager.workflowState?.projectPlanning?.planningId) {
                    console.error('Cannot generate stories: Project Planning ID is missing');
                    window.App.showNotification('Failed to generate stories: Project Planning not completed.', 'error');
                    return;
                }

                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    PlanningId: this.workflowManager.workflowState?.projectPlanning?.planningId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: null
                };

                const result = await this.apiClient.generateStories(request);

                window.App.showNotification('User stories submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(3);

            } finally {
                this.hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to generate user stories:', error);
            window.App.showNotification(`Failed to generate stories: ${error.message || error}`, 'error');
        }
    }

    /**
     * Regenerate user stories
     * @returns {Promise<void>}
     */
    async regenerateStories() {
        try {
            console.log('=== StoriesHandler.regenerateStories called ===');

            // Check if stories are already approved
            if (this.workflowManager.workflowState?.storyGeneration?.isApproved === true) {
                if (!confirm('User stories are already completed. Do you want to regenerate them? This will require re-approval.')) {
                    return;
                }
            }

            // Check if requirements and planning are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating user stories.', 'warning');
                return;
            }

            if (this.workflowManager.workflowState?.projectPlanning?.isApproved !== true) {
                window.App.showNotification('You must complete Project Planning before generating user stories.', 'warning');
                return;
            }

            const loadingOverlay = this.showLoading('Generating user stories...');
            try {
                // Get project details for story regeneration
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Validate that we have required IDs before proceeding
                if (!this.workflowManager.workflowState?.projectPlanning?.planningId) {
                    console.error('Cannot regenerate stories: Project Planning ID is missing');
                    window.App.showNotification('Failed to regenerate stories: Project Planning not completed.', 'error');
                    return;
                }

                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    PlanningId: this.workflowManager.workflowState?.projectPlanning?.planningId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: 'Regenerated stories'
                };

                const result = await this.apiClient.generateStories(request);

                window.App.showNotification('User stories submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(3);

            } finally {
                this.hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to regenerate user stories:', error);
            window.App.showNotification(`Failed to regenerate stories: ${error.message || error}`, 'error');
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
    module.exports = StoriesHandler;
} else if (typeof window !== 'undefined') {
    window.StoriesHandler = StoriesHandler;
}
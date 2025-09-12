/**
 * PromptsHandler - Handles prompt generation actions
 */
class PromptsHandler {
    /**
     * Initialize the prompts handler
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
     * Generate all prompts for approved stories
     * @returns {Promise<void>}
     */
    async generateAllPrompts() {
        try {
            console.log('=== PromptsHandler.generateAllPrompts called ===');

            // Check if stories are approved
            if (this.workflowManager.workflowState?.storyGeneration?.isApproved !== true) {
                window.App.showNotification('You must complete User Stories before generating prompts.', 'warning');
                return;
            }

            // Check if prompts are already generated
            if (this.workflowManager.workflowState?.promptGeneration?.completionPercentage >= 100) {
                if (!confirm('Prompts are already generated. Do you want to regenerate them?')) {
                    return;
                }
            }

            const loadingOverlay = this.showLoading('Generating all prompts...');
            try {
                // Get project details for prompt generation
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Validate that we have required IDs before proceeding
                if (!this.workflowManager.workflowState?.storyGeneration?.generationId) {
                    console.error('Cannot generate prompts: Story Generation ID is missing');
                    window.App.showNotification('Failed to generate prompts: User Stories not completed.', 'error');
                    return;
                }

                // Get approved stories to generate prompts for
                console.log('Getting approved stories...');
                const approvedStories = await this.apiClient.getApprovedStories(this.workflowManager.workflowState.storyGeneration.generationId);
                console.log('Approved stories:', approvedStories);

                if (!approvedStories || approvedStories.length === 0) {
                    window.App.showNotification('No approved stories found. Please approve some stories first.', 'warning');
                    return;
                }

                // Create the prompt generation request
                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    PlanningId: this.workflowManager.workflowState?.projectPlanning?.planningId,
                    StoryGenerationId: this.workflowManager.workflowState?.storyGeneration?.generationId,
                    Stories: approvedStories,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: null
                };

                console.log('Generating prompts with request:', request);

                const result = await this.apiClient.generatePrompt(request);

                window.App.showNotification('Prompts submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(4);

            } finally {
                this.hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to generate prompts:', error);
            window.App.showNotification(`Failed to generate prompts: ${error.message || error}`, 'error');
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
    module.exports = PromptsHandler;
} else if (typeof window !== 'undefined') {
    window.PromptsHandler = PromptsHandler;
}
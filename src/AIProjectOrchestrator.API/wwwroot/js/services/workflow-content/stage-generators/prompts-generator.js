/**
 * PromptsGenerator - Handles prompts stage content generation
 * Extends BaseContentGenerator for common functionality
 */
class PromptsGenerator extends BaseContentGenerator {
    /**
     * Initialize the prompts generator
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        super(workflowManager, apiClient);
    }

    /**
     * Generate prompts stage content
     * @returns {Promise<string>} HTML content for prompts stage
     */
    async generateContent() {
        try {
            // Check if we have prompts from the workflow state or API
            let prompts = [];
            let hasPrompts = false;
            const workflowState = this.workflowState;

            // First try to get prompts from workflow state
            if (workflowState?.promptGeneration?.storyPrompts && workflowState.promptGeneration.storyPrompts.length > 0) {
                prompts = workflowState.promptGeneration.storyPrompts;
                hasPrompts = true;
            } else {
                // Try to load prompts from API
                try {
                    prompts = await this.apiClient.getPrompts(this.projectId);
                    hasPrompts = prompts && prompts.length > 0;
                } catch (apiError) {
                    console.warn('Could not load prompts from API:', apiError);
                }
            }

            if (hasPrompts) {
                // Show prompt review interface
                return this.generateReviewState(prompts);
            } else {
                // Check if we can get prompts from story generation
                const generationId = workflowState?.storyGeneration?.generationId;
                if (generationId) {
                    try {
                        const approvedStories = await this.apiClient.getApprovedStories(generationId);
                        if (approvedStories && approvedStories.length > 0) {
                            return this.generateReadyState(approvedStories);
                        }
                    } catch (apiError) {
                        console.warn('Could not load approved stories:', apiError);
                    }
                }
                return this.generateEmptyState();
            }
        } catch (error) {
            console.error('Error in PromptsGenerator.generateContent:', error);
            return this.generateEmptyState();
        }
    }

    /**
     * Generate review state content for prompts
     * @param {Array} prompts - Array of prompt objects
     * @returns {string} HTML content for review state
     */
    generateReviewState(prompts) {
        const content = `
            <div class="prompts-content">
                <div class="prompts-summary">
                    <h3>Generated Prompts</h3>
                    ${this.formatPrompts(prompts)}
                </div>
                <div class="prompts-actions">
                    ${this.createButton('✅ Continue to Final Review', 'workflowManager.navigateToStage5()', 'btn btn-success')}
                    ${this.createButton('📥 Export Prompts', 'workflowManager.exportPrompts()', 'btn btn-secondary')}
                    ${this.createButton('🔄 Regenerate All', 'workflowManager.regeneratePrompts()', 'btn btn-outline')}
                </div>
            </div>
        `;

        return this.createStageContainer('Prompt Review', content);
    }

    /**
     * Generate ready state content for prompts
     * @param {Array} approvedStories - Array of approved story objects
     * @returns {string} HTML content for ready state
     */
    generateReadyState(approvedStories) {
        const content = `
            <div class="stage-status ready">
                <div class="status-icon">✅</div>
                <h3>Ready for Prompt Generation</h3>
                <p>${approvedStories.length} approved stories are ready for prompt generation.</p>
                <div class="stage-actions">
                    ${this.createButton(`🤖 Generate Prompts for ${approvedStories.length} Stories`, 'workflowManager.generateAllPrompts()', 'btn btn-primary')}
                    ${this.createButton('📋 Manage Individual Stories', 'workflowManager.navigateToStoriesOverview()', 'btn btn-secondary')}
                </div>
            </div>
        `;

        return this.createStageContainer('Prompt Review', content);
    }

    /**
     * Generate empty state content for prompts
     * @returns {string} HTML content for empty state
     */
    generateEmptyState() {
        return this.createStageContainer(
            'Prompt Review',
            this.createEmptyState(
                '🤖',
                'No Prompts Available',
                'No prompts have been generated yet. Please ensure you have approved stories and generate prompts first.',
                this.createButton('📋 Go to Stories Overview', 'workflowManager.navigateToStoriesOverview()')
            )
        );
    }

    /**
     * Format prompts data into HTML
     * @param {Array} prompts - Array of prompt objects
     * @returns {string} Formatted prompts HTML
     */
    formatPrompts(prompts) {
        if (!prompts || prompts.length === 0) {
            return '<p>No prompts available.</p>';
        }

        const promptCards = prompts.map(prompt => {
            const truncatedContent = this.truncateText(prompt.content, 200);
            const escapedContent = this.escapeHtml(truncatedContent);

            return `
                <div class="prompt-card" data-prompt-id="${prompt.id}">
                    <div class="prompt-header">
                        <h4>${this.escapeHtml(prompt.title)}</h4>
                        <span class="prompt-status ${prompt.status}">${prompt.status}</span>
                    </div>
                    <div class="prompt-content">
                        <pre>${escapedContent}</pre>
                    </div>
                    <div class="prompt-meta">
                        <span class="prompt-language">${prompt.language || 'Not specified'}</span>
                        <span class="prompt-type">${prompt.type || 'General'}</span>
                    </div>
                    <div class="prompt-actions">
                        ${this.createButton('View Full Prompt', `workflowManager.viewPrompt('${prompt.id}')`, 'btn btn-sm btn-primary')}
                        ${this.createButton('Copy', `workflowManager.copyPrompt('${prompt.id}')`, 'btn btn-sm btn-secondary')}
                    </div>
                </div>
            `;
        }).join('');

        return `<div class="prompts-grid">${promptCards}</div>`;
    }

    /**
     * Check if prompts stage is accessible
     * @returns {boolean} True if stories are approved
     */
    isStageAccessible() {
        return this.workflowState?.storyGeneration?.isApproved === true;
    }

    /**
     * Get prompts stage status
     * @returns {object} Stage status information
     */
    getStageStatus() {
        return this.getStageStatus('promptGeneration');
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PromptsGenerator;
} else if (typeof window !== 'undefined') {
    window.PromptsGenerator = PromptsGenerator;
}
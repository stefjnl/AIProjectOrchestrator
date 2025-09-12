/**
 * StoriesGenerator - Handles stories stage content generation
 * Extends BaseContentGenerator for common functionality
 */
class StoriesGenerator extends BaseContentGenerator {
    /**
     * Initialize the stories generator
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        super(workflowManager, apiClient);
    }

    /**
     * Generate stories stage content
     * @returns {Promise<string>} HTML content for stories stage
     */
    async generateContent() {
        try {
            // Check if we have a generation ID from the workflow state
            const generationId = this.workflowState?.storyGeneration?.generationId;
            const canAccess = this.workflowState?.requirementsAnalysis?.isApproved === true &&
                this.workflowState?.projectPlanning?.isApproved === true;

            if (!canAccess) {
                return this.generateLockedState();
            }

            if (generationId) {
                // Try to get the actual stories
                try {
                    const stories = await this.apiClient.getStories(generationId);
                    const isApproved = this.workflowState?.storyGeneration?.isApproved === true;

                    if (isApproved && stories) {
                        return this.generateCompletedState(stories);
                    }
                } catch (apiError) {
                    console.warn('Could not load story generation details:', apiError);
                    // Continue with state-based logic even if API call fails
                }
            }

            return this.generateActiveState();
        } catch (error) {
            console.error('Error in StoriesGenerator.generateContent:', error);
            return this.generateEmptyState();
        }
    }

    /**
     * Generate locked state content for stories
     * @returns {string} HTML content for locked state
     */
    generateLockedState() {
        const workflowState = this.workflowState;

        const requirementsNotApproved = !workflowState?.requirementsAnalysis?.isApproved;
        const planningNotApproved = !workflowState?.projectPlanning?.isApproved;

        const requirementsItem = requirementsNotApproved ? `
            <div class="requirement-item">
                <span class="status-icon">❌</span>
                <span>Requirements Analysis - Not completed</span>
                <button class="btn btn-sm btn-primary" onclick="workflowManager.jumpToStage(1)">Go</button>
            </div>
        ` : '';

        const planningItem = planningNotApproved ? `
            <div class="requirement-item">
                <span class="status-icon">❌</span>
                <span>Project Planning - Not completed</span>
                <button class="btn btn-sm btn-primary" onclick="workflowManager.jumpToStage(2)">Go</button>
            </div>
        ` : '';

        const requirementsContent = this.createLockedState(
            'Stage Locked',
            'You must complete both <strong>Requirements Analysis</strong> and <strong>Project Planning</strong> before accessing this stage.',
            `<div class="locked-requirements">${requirementsItem}${planningItem}</div>`
        );

        return this.createStageContainer('User Stories', requirementsContent);
    }

    /**
     * Generate active state content for stories
     * @returns {string} HTML content for active state
     */
    generateActiveState() {
        const workflowState = this.workflowState;
        const storyStatus = workflowState?.storyGeneration?.status;
        const isPending = storyStatus === 'PendingReview';
        const isNotStarted = storyStatus === 'NotStarted' || storyStatus === 0 || !storyStatus;
        const hasGenerationId = workflowState?.storyGeneration?.generationId;

        console.log('generateActiveState - status:', storyStatus, 'isPending:', isPending, 'isNotStarted:', isNotStarted, 'hasGenerationId:', hasGenerationId);

        if (isPending) {
            return this.createStageContainer(
                'User Stories',
                this.createStatusIndicator(
                    'pending',
                    '⏳',
                    'Stories Pending Review',
                    'Your user stories are currently under review. Please check the <a href="/Reviews/Queue">Review Queue</a>.'
                )
            );
        }

        // CRITICAL FIX: Check if stories haven't been generated yet
        // Empty string ("") is falsy, so !hasGenerationId will be true
        const hasNoGenerationId = !hasGenerationId || hasGenerationId === '';
        console.log('hasNoGenerationId:', hasNoGenerationId, 'hasGenerationId:', hasGenerationId);

        if (isNotStarted && hasNoGenerationId) {
            console.log('Stories not started and no generation ID, showing empty state');
            return this.generateEmptyState();
        }

        // If we have a generation ID but it's not approved, show active state
        if (hasGenerationId && !workflowState?.storyGeneration?.isApproved) {
            console.log('Has generation ID but not approved, showing active state');
            return this.generateActiveStateWithRegenerate();
        }

        console.log('Falling back to empty state for stories');
        return this.generateEmptyState();
    }

    /**
     * Generate active state with regenerate option
     * @returns {string} HTML content for active state with regenerate
     */
    generateActiveStateWithRegenerate() {
        return this.createStageContainer(
            'User Stories',
            this.createStatusIndicator(
                'active',
                '📖',
                'Stories Generation in Progress',
                'Your user stories are being processed. Check the Review Queue for status updates.',
                [
                    this.createButton('📋 View Review Details', 'workflowManager.viewRequirementsReview()'),
                    this.createButton('✨ Generate Stories', 'workflowManager.regenerateStories()', 'btn btn-success')
                ].join('')
            )
        );
    }

    /**
     * Generate completed state content for stories
     * @param {Array} stories - Array of story objects
     * @returns {string} HTML content for completed state
     */
    generateCompletedState(stories) {
        const content = `
            <div class="stage-status completed">
                <div class="status-icon">✅</div>
                <h3>User Stories Completed</h3>
                <p>Your user stories have been successfully generated and approved.</p>
                <div class="stories-summary">
                    <h4>Generated Stories</h4>
                    ${this.formatStories(stories)}
                </div>
            </div>
        `;

        const actions = [
            this.createButton('🤖 Generate Code Prompts', 'workflowManager.generateAllPrompts()'),
            this.createButton('➕ Add Custom Story', 'workflowManager.addCustomStory()', 'btn btn-secondary')
        ].join('');

        return this.createStageContainer('User Stories', content, actions);
    }

    /**
     * Generate empty state content for stories
     * @returns {string} HTML content for empty state
     */
    generateEmptyState() {
        return this.createStageContainer(
            'User Stories',
            this.createEmptyState(
                '📖',
                'No User Stories Found',
                'Generate user stories based on your requirements and planning.',
                this.createButton('✨ Generate Stories', 'workflowManager.generateStories()')
            )
        );
    }

    /**
     * Format stories data into HTML
     * @param {Array} stories - Array of story objects
     * @returns {string} Formatted stories HTML
     */
    formatStories(stories) {
        if (!stories || stories.length === 0) {
            return '<p>No user stories available.</p>';
        }

        const storyCards = stories.map(story => {
            const actions = [];

            actions.push(this.createButton('View Details', `workflowManager.viewStory('${story.id}')`, 'btn btn-sm btn-primary'));

            if (story.status === 'pending') {
                actions.push(this.createButton('Approve', `workflowManager.approveStory('${story.id}')`, 'btn btn-sm btn-success'));
                actions.push(this.createButton('Reject', `workflowManager.rejectStory('${story.id}')`, 'btn btn-sm btn-danger'));
            }

            return `
                <div class="story-card" data-story-id="${story.id}">
                    <div class="story-header">
                        <h4>${this.escapeHtml(story.title)}</h4>
                        <span class="story-status ${story.status}">${story.status}</span>
                    </div>
                    <p class="story-description">${this.escapeHtml(story.description)}</p>
                    <div class="story-meta">
                        <span class="story-points">Points: ${story.storyPoints || 'N/A'}</span>
                        <span class="story-priority">Priority: ${story.priority || 'Normal'}</span>
                    </div>
                    <div class="story-actions">
                        ${actions.join('')}
                    </div>
                </div>
            `;
        }).join('');

        return `<div class="stories-grid">${storyCards}</div>`;
    }

    /**
     * Check if stories stage is accessible
     * @returns {boolean} True if requirements and planning are approved
     */
    isStageAccessible() {
        return this.workflowState?.requirementsAnalysis?.isApproved === true &&
            this.workflowState?.projectPlanning?.isApproved === true;
    }

    /**
     * Get stories stage status
     * @returns {object} Stage status information
     */
    getStageStatus() {
        return this.getStageStatus('storyGeneration');
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = StoriesGenerator;
} else if (typeof window !== 'undefined') {
    window.StoriesGenerator = StoriesGenerator;
}
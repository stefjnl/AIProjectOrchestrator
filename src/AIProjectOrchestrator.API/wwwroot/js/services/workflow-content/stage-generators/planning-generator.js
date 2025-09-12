/**
 * PlanningGenerator - Handles planning stage content generation
 * Extends BaseContentGenerator for common functionality
 */
class PlanningGenerator extends BaseContentGenerator {
    /**
     * Initialize the planning generator
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        super(workflowManager, apiClient);
    }

    /**
     * Generate planning stage content
     * @returns {Promise<string>} HTML content for planning stage
     */
    async generateContent() {
        try {
            const canAccess = this.workflowState?.requirementsAnalysis?.isApproved === true;

            if (!canAccess) {
                return this.generateLockedState();
            }

            // Check if we have a planning ID from the workflow state
            const planningId = this.workflowState?.projectPlanning?.planningId;
            const planningStatus = this.workflowState?.projectPlanning?.status;
            const isApproved = this.workflowState?.projectPlanning?.isApproved === true;

            console.log('Planning stage check - planningId:', planningId, 'status:', planningStatus, 'isApproved:', isApproved);

            if (planningId) {
                // Try to get the actual planning results
                try {
                    const planning = await this.apiClient.getProjectPlan(planningId);

                    if (isApproved && planning) {
                        return this.generateCompletedState(planning);
                    }
                } catch (apiError) {
                    console.warn('Could not load project planning details:', apiError);
                    // Continue with state-based logic even if API call fails
                }
            }

            // Check status - if NotStarted (status 0) and no planningId, show empty state
            if (planningStatus === 0 || planningStatus === 'NotStarted' || !planningId) {
                console.log('Planning not started, showing empty state');
                return this.generateEmptyState();
            }

            if (isApproved) {
                console.log('Planning approved, showing completed state');
                return this.generateCompletedState(null);
            }

            return this.generateActiveState();
        } catch (error) {
            console.error('Error in PlanningGenerator.generateContent:', error);
            return this.generateEmptyState();
        }
    }

    /**
     * Generate locked state content for planning
     * @returns {string} HTML content for locked state
     */
    generateLockedState() {
        return this.createStageContainer(
            'Project Planning',
            this.createLockedState(
                'Stage Locked',
                'You must complete <strong>Requirements Analysis</strong> before accessing this stage.',
                this.createButton('Go to Requirements Analysis', 'workflowManager.jumpToStage(1)', 'btn btn-primary')
            )
        );
    }

    /**
     * Generate active state content for planning
     * @returns {string} HTML content for active state
     */
    generateActiveState() {
        const workflowState = this.workflowState;
        const planningStatus = workflowState?.projectPlanning?.status;
        const isPending = planningStatus === 'PendingReview';
        const isNotStarted = planningStatus === 'NotStarted' || planningStatus === 0;
        const hasPlanningId = workflowState?.projectPlanning?.planningId;
        const isApproved = workflowState?.projectPlanning?.isApproved === true;

        console.log('generateActiveState - status:', planningStatus, 'isPending:', isPending, 'isNotStarted:', isNotStarted, 'hasPlanningId:', hasPlanningId, 'isApproved:', isApproved);
        console.log('planningId truthy check:', !!hasPlanningId, 'planningId value:', hasPlanningId);

        if (isPending) {
            return this.createStageContainer(
                'Project Planning',
                this.createStatusIndicator(
                    'pending',
                    '⏳',
                    'Planning Pending Review',
                    'Your project planning is currently under review. Please check the <a href="/Reviews/Queue">Review Queue</a>.'
                )
            );
        }

        // CRITICAL FIX: Check if planning hasn't been generated yet
        // Empty string ("") is falsy, so !hasPlanningId will be true
        const hasNoPlanningId = !hasPlanningId || hasPlanningId === '';
        console.log('hasNoPlanningId:', hasNoPlanningId, 'hasPlanningId:', hasPlanningId);

        if (isNotStarted && hasNoPlanningId) {
            console.log('Planning not started and no planning ID, showing empty state');
            return this.generateEmptyState();
        }

        // If planning is approved, show completed state
        if (isApproved) {
            console.log('Planning approved, showing completed state');
            return this.generateCompletedState(null);
        }

        // If we have a planning ID but it's not approved, show active state with regenerate option
        if (hasPlanningId && !isApproved) {
            console.log('Has planning ID but not approved, showing active state');
            return this.generateActiveStateWithRegenerate();
        }

        console.log('Falling back to empty state');
        return this.generateEmptyState();
    }

    /**
     * Generate active state with regenerate option
     * @returns {string} HTML content for active state with regenerate
     */
    generateActiveStateWithRegenerate() {
        return this.createStageContainer(
            'Project Planning',
            this.createStatusIndicator(
                'active',
                '📋',
                'Analysis in Progress',
                'Your project planning is being processed. Check the Review Queue for status updates.',
                [
                    this.createButton('📋 View Review Details', 'workflowManager.viewRequirementsReview()'),
                    this.createButton('🚀 Generate Project Plan', 'workflowManager.regeneratePlan()', 'btn btn-success')
                ].join('')
            )
        );
    }

    /**
     * Generate completed state content for planning
     * @param {object} planning - Planning data (optional)
     * @returns {string} HTML content for completed state
     */
    generateCompletedState(planning) {
        const content = `
            <div class="stage-status completed">
                <div class="status-icon">✅</div>
                <h3>Project Planning Completed</h3>
                <p>Your project plan has been successfully created and approved.</p>
                <div class="architecture-overview">
                    <h4>Technical Architecture</h4>
                    ${planning ? this.formatPlanning(planning) : '<p>Project planning data loaded successfully.</p>'}
                </div>
            </div>
        `;

        const actions = [
            this.createButton('✨ Generate User Stories', 'workflowManager.generateStories()'),
            this.createButton('✏️ Edit Plan', 'workflowManager.editPlanning()', 'btn btn-secondary')
        ].join('');

        return this.createStageContainer('Project Planning', content, actions);
    }

    /**
     * Generate empty state content for planning
     * @returns {string} HTML content for empty state
     */
    generateEmptyState() {
        return this.createStageContainer(
            'Project Planning',
            this.createEmptyState(
                '🏗️',
                'No Project Plan Found',
                'Create a technical architecture plan for your project.',
                this.createButton('🚀 Generate Project Plan', 'workflowManager.regeneratePlan()')
            )
        );
    }

    /**
     * Format planning data into HTML
     * @param {object} planning - Planning data
     * @returns {string} Formatted planning HTML
     */
    formatPlanning(planning) {
        if (!planning || !planning.plan) {
            return '<p>No planning data available.</p>';
        }

        const sections = [
            {
                title: 'Architecture Overview',
                content: `<p>${planning.plan.architecture || 'No architecture overview'}</p>`
            },
            {
                title: 'Technology Stack',
                content: this.formatList(planning.plan.techStack, 'No tech stack specified')
            },
            {
                title: 'Development Phases',
                content: this.formatOrderedList(planning.plan.phases, 'No phases defined')
            }
        ];

        return this.createSummaryGrid(sections);
    }

    /**
     * Check if planning stage is accessible
     * @returns {boolean} True if requirements are approved
     */
    isStageAccessible() {
        return this.workflowState?.requirementsAnalysis?.isApproved === true;
    }

    /**
     * Get planning stage status
     * @returns {object} Stage status information
     */
    getStageStatus() {
        return this.getStageStatus('projectPlanning');
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PlanningGenerator;
} else if (typeof window !== 'undefined') {
    window.PlanningGenerator = PlanningGenerator;
}
/**
 * RequirementsGenerator - Handles requirements stage content generation
 * Extends BaseContentGenerator for common functionality
 */
class RequirementsGenerator extends BaseContentGenerator {
    /**
     * Initialize the requirements generator
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        super(workflowManager, apiClient);
    }

    /**
     * Generate requirements stage content
     * @returns {Promise<string>} HTML content for requirements stage
     */
    async generateContent() {
        try {
            console.log('=== RequirementsGenerator.generateContent called ===');
            const workflowState = this.workflowState;
            console.log('Workflow state:', workflowState);
            console.log('Requirements analysis:', workflowState?.requirementsAnalysis);

            // Check if we have an analysis ID from the workflow state
            const analysisId = workflowState?.requirementsAnalysis?.analysisId;
            const status = workflowState?.requirementsAnalysis?.status;
            const isApproved = workflowState?.requirementsAnalysis?.isApproved === true;

            console.log('Analysis ID:', analysisId);
            console.log('Status:', status);
            console.log('Is Approved:', isApproved);

            if (analysisId) {
                console.log('Found analysis ID, trying to load requirements details');
                // Try to get the actual requirements analysis results
                try {
                    const requirements = await this.apiClient.getRequirements(analysisId);
                    console.log('Loaded requirements:', requirements);

                    if (isApproved && requirements) {
                        console.log('Requirements are approved, showing completed state');
                        return this.generateCompletedState(requirements);
                    }
                } catch (apiError) {
                    console.warn('Could not load requirements analysis details:', apiError);
                    // Continue with state-based logic even if API call fails
                }
            }

            console.log('No analysis ID or requirements not approved, determining state based on workflow');
            const content = this.generateActiveState();
            console.log('Generated content length:', content.length);
            console.log('Content preview:', content.substring(0, 200) + '...');
            return content;
        } catch (error) {
            console.error('Error in RequirementsGenerator.generateContent:', error);
            return this.generateEmptyState();
        }
    }

    /**
     * Generate active state content for requirements
     * @returns {string} HTML content for active state
     */
    generateActiveState() {
        console.log('=== RequirementsGenerator.generateActiveState called ===');
        const workflowState = this.workflowState;
        const hasAnalysis = workflowState?.requirementsAnalysis?.status !== 'NotStarted';
        const isPending = workflowState?.requirementsAnalysis?.status === 'PendingReview';
        const isApproved = workflowState?.requirementsAnalysis?.isApproved === true;

        console.log(`Requirements state - hasAnalysis: ${hasAnalysis}, isPending: ${isPending}, isApproved: ${isApproved}`);
        console.log('Raw requirements analysis:', workflowState?.requirementsAnalysis);

        if (isApproved) {
            console.log('Requirements are approved, showing completed state');
            return this.generateCompletedState(null);
        }

        if (isPending) {
            return this.createStageContainer(
                'Requirements Analysis',
                this.createStatusIndicator(
                    'pending',
                    '⏳',
                    'Analysis Pending Review',
                    'Your requirements analysis is currently under review. Please check the <a href="/Reviews/Queue">Review Queue</a>.',
                    this.createButton('📋 View Review Details', 'workflowManager.viewRequirementsReview()')
                )
            );
        }

        // For new projects or when requirements exist but need to be regenerated
        if (hasAnalysis) {
            console.log('Requirements exist but not approved, showing active state with regenerate option');
            return this.createStageContainer(
                'Requirements Analysis',
                this.createStatusIndicator(
                    'active',
                    '📋',
                    'Analysis in Progress',
                    'Your requirements analysis is being processed. Check the Review Queue for status updates.',
                    [
                        this.createButton('📋 View Review Details', 'workflowManager.viewRequirementsReview()'),
                        this.createButton('🚀 Start Requirements Analysis', 'workflowManager.analyzeRequirements()', 'btn btn-success')
                    ].join('')
                )
            );
        }

        return this.generateEmptyState();
    }

    /**
     * Generate completed state content for requirements
     * @param {object} requirements - Requirements data (optional)
     * @returns {string} HTML content for completed state
     */
    generateCompletedState(requirements) {
        const content = `
            <div class="stage-status completed">
                <div class="status-icon">✅</div>
                <h3>Requirements Analysis Completed</h3>
                <p>Your requirements have been successfully analyzed and approved.</p>
                <div class="requirements-summary">
                    <h4>Analysis Results</h4>
                    ${requirements ? this.formatRequirements(requirements) : '<p>Requirements analysis data loaded successfully.</p>'}
                </div>
            </div>
        `;

        const actions = [
            this.createButton('🚀 Generate Project Plan', 'workflowManager.generatePlan()'),
            this.createButton('✏️ Edit Requirements', 'workflowManager.editRequirements()', 'btn btn-secondary')
        ].join('');

        return this.createStageContainer('Requirements Analysis', content, actions);
    }

    /**
     * Generate empty state content for requirements
     * @returns {string} HTML content for empty state
     */
    generateEmptyState() {
        const emptyContent = this.createEmptyState(
            '📋',
            'No Requirements Found',
            'Start by analyzing your project requirements.',
            this.createButton('🚀 Start Requirements Analysis', 'workflowManager.analyzeRequirements()', 'btn btn-primary btn-lg', '', 'font-size: 16px; padding: 12px 24px;')
        );

        const gettingStartedContent = this.createGettingStartedSection(
            'Getting Started',
            'Click the button above to begin requirements analysis. You\'ll be prompted to describe:',
            [
                'What problem your project solves',
                'Key features and functionality',
                'Technology constraints or preferences',
                'Timeline and budget considerations'
            ],
            this.createButton('🚀 Start Analysis Now', 'workflowManager.analyzeRequirements()', 'btn btn-success', '', 'background: #28a745; border-color: #28a745;')
        );

        return this.createStageContainer('Requirements Analysis', emptyContent + gettingStartedContent);
    }

    /**
     * Format requirements data into HTML
     * @param {object} requirements - Requirements data
     * @returns {string} Formatted requirements HTML
     */
    formatRequirements(requirements) {
        if (!requirements || !requirements.analysis) {
            return '<p>No requirements analysis available.</p>';
        }

        const categories = [
            {
                title: 'Functional Requirements',
                items: requirements.analysis.functional,
                emptyMessage: 'No functional requirements'
            },
            {
                title: 'Non-Functional Requirements',
                items: requirements.analysis.nonFunctional,
                emptyMessage: 'No non-functional requirements'
            },
            {
                title: 'Technical Constraints',
                items: requirements.analysis.constraints,
                emptyMessage: 'No constraints'
            }
        ];

        const sections = categories.map(category => `
            <div class="requirement-category">
                <h4>${category.title}</h4>
                ${this.formatList(category.items, category.emptyMessage)}
            </div>
        `).join('');

        return `<div class="requirements-grid">${sections}</div>`;
    }

    /**
     * Check if requirements stage is accessible
     * @returns {boolean} Always true for requirements stage
     */
    isStageAccessible() {
        return true; // Requirements stage is always accessible
    }

    /**
     * Get requirements stage status
     * @returns {object} Stage status information
     */
    getStageStatus() {
        return this.getStageStatus('requirementsAnalysis');
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = RequirementsGenerator;
} else if (typeof window !== 'undefined') {
    window.RequirementsGenerator = RequirementsGenerator;
}
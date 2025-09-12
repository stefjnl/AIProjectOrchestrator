/**
 * ReviewGenerator - Handles review stage content generation
 * Extends BaseContentGenerator for common functionality
 */
class ReviewGenerator extends BaseContentGenerator {
    /**
     * Initialize the review generator
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        super(workflowManager, apiClient);
    }

    /**
     * Generate review stage content
     * @returns {Promise<string>} HTML content for review stage
     */
    async generateContent() {
        try {
            const reviews = await this.apiClient.getPendingReviews();
            return this.generateReviewState(reviews);
        } catch (error) {
            console.error('Error in ReviewGenerator.generateContent:', error);
            return this.generateEmptyState();
        }
    }

    /**
     * Generate review state content
     * @param {Array} reviews - Array of review objects
     * @returns {string} HTML content for review state
     */
    generateReviewState(reviews) {
        const content = `
            <div class="review-content">
                <div class="review-summary">
                    <h3>Review Summary</h3>
                    ${this.formatReviewSummary(reviews)}
                </div>
                <div class="review-actions">
                    ${this.createButton('✅ Complete Project', 'workflowManager.completeProject()', 'btn btn-success')}
                    ${this.createButton('📥 Export Results', 'workflowManager.exportProject()', 'btn btn-secondary')}
                    ${this.createButton('📊 Generate Report', 'workflowManager.generateReport()', 'btn btn-outline')}
                </div>
            </div>
        `;

        return this.createStageContainer('Final Review', content);
    }

    /**
     * Generate empty state content for review
     * @returns {string} HTML content for empty state
     */
    generateEmptyState() {
        return this.createStageContainer(
            'Final Review',
            this.createEmptyState(
                '✅',
                'Ready for Review',
                'All prompts have been generated and are ready for final review.',
                this.createButton('✅ Complete Project', 'workflowManager.completeProject()', 'btn btn-success')
            )
        );
    }

    /**
     * Format review summary data into HTML
     * @param {Array} reviews - Array of review objects
     * @returns {string} Formatted review summary HTML
     */
    formatReviewSummary(reviews) {
        const total = reviews.length;
        const pending = reviews.filter(r => r.status === 'pending').length;
        const approved = reviews.filter(r => r.status === 'approved').length;
        const rejected = reviews.filter(r => r.status === 'rejected').length;
        const approvalPercentage = total > 0 ? (approved / total) * 100 : 0;

        const stats = [
            { title: 'Total Reviews', value: total, cssClass: '' },
            { title: 'Pending', value: pending, cssClass: 'pending' },
            { title: 'Approved', value: approved, cssClass: 'approved' },
            { title: 'Rejected', value: rejected, cssClass: 'rejected' }
        ];

        const statsGrid = stats.map(stat => `
            <div class="summary-stat">
                <h4>${stat.title}</h4>
                <span class="stat-number ${stat.cssClass}">${stat.value}</span>
            </div>
        `).join('');

        return `
            <div class="review-summary-grid">
                ${statsGrid}
            </div>
            <div class="review-progress">
                ${this.createProgressBar(approvalPercentage, `${approved} of ${total} reviews approved`)}
            </div>
        `;
    }

    /**
     * Check if review stage is accessible
     * @returns {boolean} True if all previous stages are approved
     */
    isStageAccessible() {
        const workflowState = this.workflowState;
        return workflowState?.requirementsAnalysis?.isApproved === true &&
            workflowState?.projectPlanning?.isApproved === true &&
            workflowState?.storyGeneration?.isApproved === true;
    }

    /**
     * Get review stage status
     * @returns {object} Stage status information
     */
    getStageStatus() {
        // Review stage doesn't have a dedicated status key, check if accessible
        return {
            status: this.isStageAccessible() ? 'Ready' : 'Locked',
            isApproved: false,
            hasData: false,
            isAccessible: this.isStageAccessible()
        };
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ReviewGenerator;
} else if (typeof window !== 'undefined') {
    window.ReviewGenerator = ReviewGenerator;
}
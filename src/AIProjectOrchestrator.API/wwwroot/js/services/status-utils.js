/**
 * Status Utilities Service
 * Provides utility functions for handling story statuses and state management
 */
class StatusUtils {
    constructor() {
        this.statusMap = {
            'pending': 'Pending',
            'approved': 'Approved',
            'rejected': 'Rejected',
            'generated': 'Generated',
            'inprogress': 'In Progress',
            'completed': 'Completed',
            'draft': 'Draft',
            'review': 'Review'
        };

        this.statusClasses = {
            'pending': 'status-pending',
            'approved': 'status-approved',
            'rejected': 'status-rejected',
            'generated': 'status-generated',
            'inprogress': 'status-inprogress',
            'completed': 'status-completed',
            'draft': 'status-draft',
            'review': 'status-review'
        };

        this.priorityClasses = {
            'low': 'priority-low',
            'medium': 'priority-medium',
            'high': 'priority-high',
            'critical': 'priority-critical'
        };
    }

    /**
     * Normalize story status string
     * @param {string} status - Raw status string
     * @returns {string} Normalized status
     */
    normalizeStoryStatus(status) {
        if (!status) return 'pending';

        const normalizedStatus = status.toLowerCase().replace(/\s+/g, '');
        return this.statusMap[normalizedStatus] ? normalizedStatus : 'pending';
    }

    /**
     * Get human-readable status name
     * @param {string} status - Normalized status
     * @returns {string} Human-readable status name
     */
    getStatusName(status) {
        const normalizedStatus = this.normalizeStoryStatus(status);
        return this.statusMap[normalizedStatus] || 'Pending';
    }

    /**
     * Get CSS class for status
     * @param {string} status - Status string
     * @returns {string} CSS class name
     */
    getStatusClass(status) {
        const normalizedStatus = this.normalizeStoryStatus(status);
        return this.statusClasses[normalizedStatus] || 'status-pending';
    }

    /**
     * Get CSS class for priority
     * @param {string} priority - Priority string
     * @returns {string} CSS class name
     */
    getPriorityClass(priority) {
        const normalizedPriority = (priority || 'medium').toLowerCase();
        return this.priorityClasses[normalizedPriority] || 'priority-medium';
    }

    /**
     * Check if story can be approved
     * @param {string} status - Story status
     * @returns {boolean} Whether story can be approved
     */
    canApproveStory(status) {
        const normalizedStatus = this.normalizeStoryStatus(status);
        return ['pending', 'rejected', 'draft'].includes(normalizedStatus);
    }

    /**
     * Check if story can be rejected
     * @param {string} status - Story status
     * @returns {boolean} Whether story can be rejected
     */
    canRejectStory(status) {
        const normalizedStatus = this.normalizeStoryStatus(status);
        return ['pending', 'approved', 'draft'].includes(normalizedStatus);
    }

    /**
     * Check if prompt can be generated
     * @param {string} status - Story status
     * @param {boolean} hasPrompt - Whether story already has a prompt
     * @returns {boolean} Whether prompt can be generated
     */
    canGeneratePrompt(status, hasPrompt = false) {
        const normalizedStatus = this.normalizeStoryStatus(status);
        return normalizedStatus === 'approved' && !hasPrompt;
    }

    /**
     * Check if story is actionable (can be approved/rejected)
     * @param {string} status - Story status
     * @returns {boolean} Whether story is actionable
     */
    isStoryActionable(status) {
        const normalizedStatus = this.normalizeStoryStatus(status);
        return ['pending', 'draft'].includes(normalizedStatus);
    }

    /**
     * Check if story is in final state
     * @param {string} status - Story status
     * @returns {boolean} Whether story is in final state
     */
    isStoryFinalized(status) {
        const normalizedStatus = this.normalizeStoryStatus(status);
        return ['approved', 'rejected'].includes(normalizedStatus);
    }

    /**
     * Calculate approval percentage
     * @param {Array} stories - Array of story objects
     * @returns {Object} Statistics object
     */
    calculateApprovalStats(stories) {
        if (!Array.isArray(stories) || stories.length === 0) {
            return {
                total: 0,
                approved: 0,
                rejected: 0,
                pending: 0,
                approvalPercentage: 0,
                rejectionPercentage: 0,
                pendingPercentage: 0
            };
        }

        const total = stories.length;
        const approved = stories.filter(s => this.normalizeStoryStatus(s.status) === 'approved').length;
        const rejected = stories.filter(s => this.normalizeStoryStatus(s.status) === 'rejected').length;
        const pending = total - approved - rejected;

        return {
            total,
            approved,
            rejected,
            pending,
            approvalPercentage: Math.round((approved / total) * 100),
            rejectionPercentage: Math.round((rejected / total) * 100),
            pendingPercentage: Math.round((pending / total) * 100)
        };
    }

    /**
     * Calculate prompt generation statistics
     * @param {Array} stories - Array of story objects
     * @returns {Object} Prompt statistics object
     */
    calculatePromptStats(stories) {
        if (!Array.isArray(stories) || stories.length === 0) {
            return {
                totalStories: 0,
                storiesWithPrompts: 0,
                storiesWithoutPrompts: 0,
                promptPercentage: 0
            };
        }

        const totalStories = stories.length;
        const storiesWithPrompts = stories.filter(s => s.hasPrompt).length;
        const storiesWithoutPrompts = totalStories - storiesWithPrompts;

        return {
            totalStories,
            storiesWithPrompts,
            storiesWithoutPrompts,
            promptPercentage: Math.round((storiesWithPrompts / totalStories) * 100)
        };
    }

    /**
     * Get button states based on story collection
     * @param {Array} stories - Array of story objects
     * @returns {Object} Button states object
     */
    getButtonStates(stories) {
        if (!Array.isArray(stories) || stories.length === 0) {
            return {
                approveAll: { disabled: true, text: 'Approve All (0)' },
                generatePrompts: { disabled: true, text: 'Generate Prompts (0)' },
                continueWorkflow: { visible: false }
            };
        }

        const approvedStories = stories.filter(s => this.normalizeStoryStatus(s.status) === 'approved').length;
        const actionableStories = stories.filter(s => this.isStoryActionable(s.status)).length;
        const storiesEligibleForPrompts = stories.filter(s => this.canGeneratePrompt(s.status, s.hasPrompt)).length;

        return {
            approveAll: {
                disabled: actionableStories === 0,
                text: `Approve All (${actionableStories})`
            },
            generatePrompts: {
                disabled: storiesEligibleForPrompts === 0,
                text: `Generate Prompts (${storiesEligibleForPrompts})`
            },
            continueWorkflow: {
                visible: approvedStories > 0 && storiesEligibleForPrompts === 0
            }
        };
    }

    /**
     * Validate story data
     * @param {Object} story - Story object
     * @returns {Object} Validation result
     */
    validateStory(story) {
        const errors = [];

        if (!story.title || story.title.trim().length === 0) {
            errors.push('Title is required');
        }

        if (!story.description || story.description.trim().length === 0) {
            errors.push('Description is required');
        }

        if (story.storyPoints && (isNaN(story.storyPoints) || story.storyPoints < 1 || story.storyPoints > 100)) {
            errors.push('Story points must be between 1 and 100');
        }

        if (story.priority && !['Low', 'Medium', 'High', 'Critical'].includes(story.priority)) {
            errors.push('Priority must be one of: Low, Medium, High, Critical');
        }

        return {
            isValid: errors.length === 0,
            errors
        };
    }

    /**
     * Format story for display
     * @param {Object} story - Raw story object
     * @returns {Object} Formatted story object
     */
    formatStoryForDisplay(story) {
        return {
            ...story,
            status: this.normalizeStoryStatus(story.status),
            statusName: this.getStatusName(story.status),
            statusClass: this.getStatusClass(story.status),
            priorityClass: this.getPriorityClass(story.priority),
            canApprove: this.canApproveStory(story.status),
            canReject: this.canRejectStory(story.status),
            canGeneratePrompt: this.canGeneratePrompt(story.status, story.hasPrompt),
            isActionable: this.isStoryActionable(story.status),
            isFinalized: this.isStoryFinalized(story.status)
        };
    }

    /**
     * Get status color (for charts/graphs)
     * @param {string} status - Status string
     * @returns {string} Color hex code
     */
    getStatusColor(status) {
        const colors = {
            'pending': '#ffc107',
            'approved': '#28a745',
            'rejected': '#dc3545',
            'generated': '#17a2b8',
            'inprogress': '#fd7e14',
            'completed': '#6f42c1',
            'draft': '#6c757d',
            'review': '#007bff'
        };

        const normalizedStatus = this.normalizeStoryStatus(status);
        return colors[normalizedStatus] || '#6c757d';
    }

    /**
     * Get priority color
     * @param {string} priority - Priority string
     * @returns {string} Color hex code
     */
    getPriorityColor(priority) {
        const colors = {
            'low': '#28a745',
            'medium': '#ffc107',
            'high': '#fd7e14',
            'critical': '#dc3545'
        };

        const normalizedPriority = (priority || 'medium').toLowerCase();
        return colors[normalizedPriority] || '#6c757d';
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { StatusUtils };
}

// Make available globally for backward compatibility
window.StatusUtils = StatusUtils;
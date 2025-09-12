/**
 * StoryUtils - Utility functions for stories overview functionality
 * Provides helper functions for export, navigation, and other utilities
 */
class StoryUtils extends BaseStoriesManager {
    constructor() {
        super();
        console.log('StoryUtils initialized');
    }

    /**
     * Export all stories to JSON file
     */
    exportStories() {
        if (!this.stories || this.stories.length === 0) {
            this.showNotification('No stories to export.', 'warning');
            return;
        }

        const data = {
            generationId: this.generationId,
            projectId: this.projectId,
            exportDate: new Date().toISOString(),
            stories: this.stories,
            summary: {
                total: this.getStoriesCount(),
                approved: this.manager && this.manager.actions ? this.manager.actions.getApprovedStoriesCount() : 0,
                rejected: this.manager && this.manager.actions ? this.manager.actions.getRejectedStoriesCount() : 0,
                pending: this.manager && this.manager.actions ? this.manager.actions.getPendingStoriesCount() : 0,
                withPrompts: this.manager && this.manager.promptGen ? this.manager.promptGen.getStoriesWithPromptsCount() : 0
            }
        };

        try {
            const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `stories-overview-${this.generationId}.json`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);

            this.showNotification('Stories exported successfully!', 'success');
            console.log(`Exported ${this.stories.length} stories`);

        } catch (error) {
            console.error('Failed to export stories:', error);
            this.showNotification('Failed to export stories. Please try again.', 'error');
        }
    }

    /**
     * Continue to workflow (navigate to Stage 4 - Prompt Review)
     */
    continueToWorkflow() {
        if (!this.projectId) {
            this.showNotification('Project ID not available.', 'error');
            return;
        }

        // Navigate back to the workflow at Stage 4 (Prompt Review)
        window.location.href = `/Projects/Workflow?projectId=${this.projectId}`;
    }

    /**
     * Calculate quality score for a prompt
     * @param {string} prompt - Prompt text
     * @returns {string} Quality score as percentage
     */
    calculateQualityScore(prompt) {
        if (!prompt || typeof prompt !== 'string') return '0%';

        // Simple quality scoring based on prompt characteristics
        let score = 0;
        if (prompt.includes('Context') || prompt.includes('Architecture')) score += 20;
        if (prompt.includes('Requirements') || prompt.includes('Deliverables')) score += 20;
        if (prompt.includes('Testing') || prompt.includes('Quality')) score += 15;
        if (prompt.length > 1000) score += 15;
        if (prompt.includes('Code') || prompt.includes('Implementation')) score += 10;

        return `${Math.min(score, 100)}%`;
    }

    /**
     * Escape HTML to prevent XSS
     * @param {string} text - Text to escape
     * @returns {string} Escaped text
     */
    escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Format file size for display
     * @param {number} bytes - File size in bytes
     * @returns {string} Formatted file size
     */
    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    /**
     * Format date for display
     * @param {string|Date} date - Date to format
     * @returns {string} Formatted date
     */
    formatDate(date) {
        if (!date) return '';
        const d = new Date(date);
        return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
    }

    /**
     * Generate summary statistics
     * @returns {Object} Summary statistics
     */
    generateSummary() {
        const total = this.getStoriesCount();
        if (total === 0) {
            return {
                total: 0,
                approved: 0,
                rejected: 0,
                pending: 0,
                withPrompts: 0,
                approvalPercentage: 0,
                promptPercentage: 0
            };
        }

        const approved = this.manager && this.manager.actions ? this.manager.actions.getApprovedStoriesCount() : 0;
        const rejected = this.manager && this.manager.actions ? this.manager.actions.getRejectedStoriesCount() : 0;
        const pending = this.manager && this.manager.actions ? this.manager.actions.getPendingStoriesCount() : 0;
        const withPrompts = this.manager && this.manager.promptGen ? this.manager.promptGen.getStoriesWithPromptsCount() : 0;

        return {
            total,
            approved,
            rejected,
            pending,
            withPrompts,
            approvalPercentage: Math.round((approved / total) * 100),
            promptPercentage: Math.round((withPrompts / total) * 100),
            averageStoryPoints: this.calculateAverageStoryPoints(),
            priorityDistribution: this.getPriorityDistribution()
        };
    }

    /**
     * Calculate average story points
     * @returns {number} Average story points
     */
    calculateAverageStoryPoints() {
        if (!this.stories || this.stories.length === 0) return 0;

        const storiesWithPoints = this.stories.filter(s => s.storyPoints && typeof s.storyPoints === 'number');
        if (storiesWithPoints.length === 0) return 0;

        const totalPoints = storiesWithPoints.reduce((sum, story) => sum + story.storyPoints, 0);
        return Math.round(totalPoints / storiesWithPoints.length);
    }

    /**
     * Get priority distribution
     * @returns {Object} Priority distribution
     */
    getPriorityDistribution() {
        const distribution = {
            Low: 0,
            Medium: 0,
            High: 0,
            Critical: 0
        };

        if (!this.stories) return distribution;

        this.stories.forEach(story => {
            const priority = story.priority || 'Medium';
            if (distribution.hasOwnProperty(priority)) {
                distribution[priority]++;
            }
        });

        return distribution;
    }

    /**
     * Generate export filename
     * @param {string} prefix - Filename prefix
     * @param {string} extension - File extension
     * @returns {string} Generated filename
     */
    generateExportFilename(prefix = 'stories', extension = 'json') {
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        return `${prefix}-${this.generationId}-${timestamp}.${extension}`;
    }

    /**
     * Validate export data
     * @param {Object} data - Data to validate
     * @returns {Object} Validation result { valid: boolean, message: string }
     */
    validateExportData(data) {
        if (!data) {
            return { valid: false, message: 'No data to export' };
        }

        if (!data.stories || !Array.isArray(data.stories)) {
            return { valid: false, message: 'Invalid stories data format' };
        }

        if (data.stories.length === 0) {
            return { valid: false, message: 'No stories to export' };
        }

        return { valid: true, message: '' };
    }

    /**
     * Create data blob for export
     * @param {Object} data - Data to export
     * @param {string} type - MIME type
     * @returns {Blob} Data blob
     */
    createExportBlob(data, type = 'application/json') {
        try {
            return new Blob([JSON.stringify(data, null, 2)], { type });
        } catch (error) {
            console.error('Failed to create export blob:', error);
            throw new Error('Failed to create export data');
        }
    }

    /**
     * Download file
     * @param {Blob} blob - File blob
     * @param {string} filename - Filename
     */
    downloadFile(blob, filename) {
        try {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);

            console.log(`File downloaded: ${filename}`);
        } catch (error) {
            console.error('Failed to download file:', error);
            throw new Error('Failed to download file');
        }
    }

    /**
     * Get navigation URL for workflow
     * @param {number} stage - Workflow stage number (optional)
     * @returns {string} Navigation URL
     */
    getWorkflowUrl(stage = null) {
        if (!this.projectId) {
            throw new Error('Project ID not available');
        }

        let url = `/Projects/Workflow?projectId=${this.projectId}`;
        if (stage) {
            url += `&stage=${stage}`;
        }
        return url;
    }

    /**
     * Check if navigation to workflow is allowed
     * @returns {boolean} True if navigation is allowed
     */
    canNavigateToWorkflow() {
        if (!this.projectId) return false;

        // Check if any stories have prompts
        const hasPrompts = this.stories.some(s => s.hasPrompt);
        return hasPrompts;
    }

    /**
     * Get utility functions status
     * @returns {Object} Status information
     */
    getUtilityStatus() {
        return {
            hasStories: this.getStoriesCount() > 0,
            hasProjectId: !!this.projectId,
            hasGenerationId: !!this.generationId,
            canExport: this.getStoriesCount() > 0,
            canNavigateToWorkflow: this.canNavigateToWorkflow()
        };
    }
}
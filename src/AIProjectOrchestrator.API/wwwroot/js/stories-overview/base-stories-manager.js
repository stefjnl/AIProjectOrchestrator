/**
 * BaseStoriesManager - Core functionality and state management for stories overview
 * Provides common functionality shared across all stories overview components
 */
class BaseStoriesManager {
    constructor() {
        // Core state
        this.generationId = null;
        this.projectId = null;
        this.stories = [];
        this.currentStory = null;
        this.isLoading = false;
        this.autoRefreshInterval = null;

        // Modal state
        this.currentPrompt = null;

        console.log('BaseStoriesManager initialized');
    }

    /**
     * Initialize the stories manager with required parameters
     * @param {string} generationId - The story generation ID
     * @param {string} projectId - The project ID
     */
    initialize(generationId, projectId) {
        if (!generationId || !projectId) {
            throw new Error('Both generationId and projectId are required');
        }

        this.generationId = generationId;
        this.projectId = projectId;

        console.log(`BaseStoriesManager initialized with generationId=${generationId}, projectId=${projectId}`);

        // Validate GUID format
        if (!this.isValidGuid(generationId)) {
            console.warn(`Invalid generationId format: ${generationId}. Expected a valid GUID string.`);
        }

        return true;
    }

    /**
     * Validate GUID format
     * @param {string} guid - The GUID to validate
     * @returns {boolean} True if valid GUID format
     */
    isValidGuid(guid) {
        if (!guid || typeof guid !== 'string') return false;
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        return guidRegex.test(guid);
    }

    /**
     * Show loading state
     * @param {boolean} show - Whether to show loading state
     */
    setLoadingState(show) {
        this.isLoading = show;
        const loadingElement = document.querySelector('.loading-state');
        if (loadingElement) {
            loadingElement.style.display = show ? 'block' : 'none';
        }

        // Disable/enable action buttons during loading
        const actionButtons = document.querySelectorAll('.stories-actions .btn');
        actionButtons.forEach(btn => {
            if (!btn.onclick || !btn.onclick.toString().includes('refreshStories')) {
                btn.disabled = show;
            }
        });
    }

    /**
     * Show error message in the stories grid
     * @param {string} message - Error message to display
     */
    showError(message) {
        const storiesGrid = document.getElementById('stories-grid');
        if (storiesGrid) {
            storiesGrid.innerHTML = `
                <div class="error-state">
                    <div class="error-icon">❌</div>
                    <h3>Error Loading Stories</h3>
                    <p>${message}</p>
                    <button class="btn btn-primary" onclick="window.storiesOverviewManager.refreshStories()">
                        🔄 Try Again
                    </button>
                </div>
            `;
        }

        console.error('StoriesOverview error:', message);
    }

    /**
     * Show notification using the global App notification system
     * @param {string} message - Notification message
     * @param {string} type - Notification type (success, error, warning, info)
     */
    showNotification(message, type = 'info') {
        if (window.App && window.App.showNotification) {
            window.App.showNotification(message, type);
        } else {
            console.log(`[${type.toUpperCase()}] ${message}`);
        }
    }

    /**
     * Show loading overlay
     * @param {string} message - Loading message
     * @returns {HTMLElement} Loading overlay element
     */
    showLoadingOverlay(message = 'Loading...') {
        const overlay = document.createElement('div');
        overlay.className = 'loading-overlay';
        overlay.innerHTML = `
            <div class="loading-content">
                <div class="loading-spinner"></div>
                <p>${message}</p>
            </div>
        `;

        document.body.appendChild(overlay);
        return overlay;
    }

    /**
     * Hide loading overlay
     * @param {HTMLElement} overlay - Loading overlay element to remove
     */
    hideLoadingOverlay(overlay) {
        if (overlay && overlay.parentNode) {
            overlay.parentNode.removeChild(overlay);
        }
    }

    /**
     * Start auto-refresh interval
     * @param {number} intervalMs - Interval in milliseconds (default: 30 seconds)
     */
    startAutoRefresh(intervalMs = 30000) {
        if (this.autoRefreshInterval) return;

        this.autoRefreshInterval = setInterval(() => {
            this.refreshStories();
        }, intervalMs);

        console.log('Auto-refresh started for StoriesOverview');
    }

    /**
     * Stop auto-refresh interval
     */
    stopAutoRefresh() {
        if (this.autoRefreshInterval) {
            clearInterval(this.autoRefreshInterval);
            this.autoRefreshInterval = null;
            console.log('Auto-refresh stopped for StoriesOverview');
        }
    }

    /**
     * Refresh stories (to be implemented by subclasses)
     */
    refreshStories() {
        // Override in subclasses
        console.warn('refreshStories() should be implemented by subclass');
    }

    /**
     * Cleanup resources
     */
    destroy() {
        this.stopAutoRefresh();
        console.log('BaseStoriesManager destroyed');
    }

    /**
     * Get current stories count
     * @returns {number} Number of stories
     */
    getStoriesCount() {
        return this.stories ? this.stories.length : 0;
    }

    /**
     * Get story by ID
     * @param {string} storyId - Story ID
     * @returns {Object|null} Story object or null
     */
    getStoryById(storyId) {
        if (!this.stories || !storyId) return null;
        return this.stories.find(s => s.id === storyId) || null;
    }

    /**
     * Get story by index
     * @param {number} index - Story index
     * @returns {Object|null} Story object or null
     */
    getStoryByIndex(index) {
        if (!this.stories || index < 0 || index >= this.stories.length) return null;
        return this.stories[index];
    }

    /**
     * Update local story status
     * @param {string} storyId - Story ID
     * @param {string} status - New status
     * @param {Object} additionalData - Additional data to update
     */
    updateLocalStoryStatus(storyId, status, additionalData = {}) {
        const story = this.getStoryById(storyId);
        if (story) {
            story.status = status;
            Object.assign(story, additionalData);
            console.log(`Updated local story ${storyId} status to ${status}`, additionalData);
            return true;
        }
        console.warn(`Story ${storyId} not found for status update`);
        return false;
    }

    /**
     * Calculate approval progress
     * @returns {Object} Progress data { approved, total, percentage, rejected }
     */
    calculateProgress() {
        const total = this.getStoriesCount();
        if (total === 0) {
            return { approved: 0, total: 0, percentage: 0, rejected: 0 };
        }

        const approved = this.stories.filter(s =>
            window.StatusUtils.normalizeStoryStatus(s.status) === window.StatusUtils.StoryStatus.APPROVED
        ).length;

        const rejected = this.stories.filter(s =>
            window.StatusUtils.normalizeStoryStatus(s.status) === window.StatusUtils.StoryStatus.REJECTED
        ).length;

        const percentage = Math.round((approved / total) * 100);

        return { approved, total, percentage, rejected };
    }

    /**
     * Generate mock prompt for development/testing
     * @param {Object} story - Story object
     * @returns {string} Mock prompt content
     */
    generateMockPrompt(story) {
        return `// Mock prompt for story: ${story.title || 'Untitled Story'}
// Generated at: ${new Date().toISOString()}

## Context
${story.description || 'No description provided'}

## Requirements
Based on the story requirements, implement the following:

${story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)
                ? story.acceptanceCriteria.map(criterion => `- ${criterion}`).join('\n')
                : '- No specific acceptance criteria provided'}

## Technical Implementation
- Priority: ${story.priority || 'Medium'}
- Story Points: ${story.storyPoints || 'Not specified'}
- Status: ${story.status || 'Pending'}

## Deliverables
- Working implementation
- Unit tests
- Documentation
- Code review

This is a mock prompt generated for development purposes.`;
    }
}
/**
 * Story API Service
 * Handles all API interactions for story management
 */
class StoryApiService {
    constructor(apiClient) {
        this.apiClient = apiClient;
    }

    /**
     * Load stories for a specific generation
     * @param {string} generationId - The generation ID
     * @returns {Promise<Array>} Array of stories
     */
    async loadStories(generationId) {
        try {
            console.log(`Loading stories for generation ${generationId}`);
            const stories = await this.apiClient.getStories(generationId);
            return stories || [];
        } catch (error) {
            console.error('Failed to load stories:', error);
            throw new Error('Failed to load stories. Please try again.');
        }
    }

    /**
     * Approve a story
     * @param {string} storyId - The story ID
     * @returns {Promise<void>}
     */
    async approveStory(storyId) {
        return this.apiClient.approveStory(storyId);
    }

    /**
     * Reject a story with feedback
     * @param {string} storyId - The story ID
     * @param {string} feedback - Rejection feedback
     * @returns {Promise<void>}
     */
    async rejectStory(storyId, feedback) {
        return this.apiClient.rejectStory(storyId, { feedback });
    }

    /**
     * Edit a story
     * @param {string} storyId - The story ID
     * @param {Object} updatedStory - Updated story data
     * @returns {Promise<void>}
     */
    async editStory(storyId, updatedStory) {
        return this.apiClient.editStory(storyId, updatedStory);
    }

    /**
     * Approve all pending stories
     * @param {Array<string>} storyIds - Array of story IDs
     * @returns {Promise<void>}
     */
    async approveAllStories(storyIds) {
        const promises = storyIds.map(storyId => this.approveStory(storyId));
        await Promise.all(promises);
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { StoryApiService };
}

// Make available globally for backward compatibility
window.StoryApiService = StoryApiService;
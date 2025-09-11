/**
 * Prompt Service
 * Handles prompt generation, quality scoring, and mock fallback functionality
 */
class PromptService {
    constructor(apiClient) {
        this.apiClient = apiClient;
    }

    /**
     * Generate a prompt for a story
     * @param {Object} request - Prompt generation request
     * @param {string} request.StoryGenerationId - Story generation ID
     * @param {number} request.StoryIndex - Story index
     * @param {Object} request.TechnicalPreferences - Technical preferences
     * @param {string} request.PromptStyle - Prompt style
     * @returns {Promise<Object>} Result with promptId
     */
    async generatePrompt(request) {
        try {
            console.log(`Calling APIClient.generatePrompt with request:`, request);
            const result = await this.apiClient.generatePrompt(request);

            // Enhanced validation for different response formats
            if (!result) {
                console.error('Empty response from generatePrompt');
                throw new Error('Empty response from prompt generation service.');
            }

            // Handle different possible response formats
            let promptId = result.promptId || result.PromptId || result.id || result.Id;

            if (!promptId) {
                console.error('Invalid response from generatePrompt - no prompt ID found. Response:', result);
                throw new Error('Invalid response from prompt generation service - no prompt ID found.');
            }

            console.log(`Extracted promptId: ${promptId}`);
            return { promptId };

        } catch (error) {
            console.error('Error in generatePrompt call:', error);
            console.error('Error details:', {
                message: error.message,
                stack: error.stack,
                request: request
            });

            // Handle backend "not implemented" errors with graceful fallback
            if (error.message.includes('500') && error.message.includes('not implemented')) {
                console.warn('Backend prompt generation is not implemented - using mock response for development');

                // Generate a mock prompt ID for development/testing
                const mockPromptId = `mock-prompt-${request.StoryGenerationId}-${Date.now()}`;

                return { promptId: mockPromptId };
            } else {
                throw error;
            }
        }
    }

    /**
     * Get a prompt by ID
     * @param {string} promptId - The prompt ID
     * @returns {Promise<Object>} Prompt data
     */
    async getPrompt(promptId) {
        return this.apiClient.getPrompt(promptId);
    }

    /**
     * Generate a mock prompt for development/testing
     * @param {Object} story - Story object
     * @returns {string} Mock prompt content
     */
    generateMockPrompt(story) {
        return `Mock prompt for story: ${story.title || 'Untitled Story'}

Context:
- Story: ${story.description || 'No description available'}
- Priority: ${story.priority || 'Medium'}
- Story Points: ${story.storyPoints || 'Not specified'}

Requirements:
- Implement the story functionality as described
- Follow best practices for code quality
- Include appropriate error handling
- Add necessary tests

Technical Considerations:
- Use appropriate design patterns
- Consider performance implications
- Ensure maintainability and readability

Acceptance Criteria:
${story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)
                ? story.acceptanceCriteria.map(criterion => `- ${criterion}`).join('\n')
                : '- Basic functionality works as expected'}`;
    }

    /**
     * Calculate quality score for a prompt based on content characteristics
     * @param {string} prompt - The prompt content
     * @returns {string} Quality score as percentage
     */
    calculateQualityScore(prompt) {
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
     * Validate prompt generation request
     * @param {Object} request - The request object
     * @returns {boolean} Whether the request is valid
     */
    validatePromptRequest(request) {
        if (!request || typeof request !== 'object') {
            return false;
        }

        if (!request.StoryGenerationId || typeof request.StoryGenerationId !== 'string') {
            console.error('Invalid StoryGenerationId format');
            return false;
        }

        if (request.StoryGenerationId.length !== 36) {
            console.error(`Invalid StoryGenerationId format: ${request.StoryGenerationId}. Expected a valid GUID string.`);
            return false;
        }

        if (typeof request.StoryIndex !== 'number' || request.StoryIndex < 0) {
            console.error('Invalid StoryIndex format');
            return false;
        }

        return true;
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { PromptService };
}

// Make available globally for backward compatibility
window.PromptService = PromptService;
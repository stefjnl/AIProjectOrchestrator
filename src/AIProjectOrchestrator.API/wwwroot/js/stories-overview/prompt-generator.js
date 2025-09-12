/**
 * PromptGenerator - Handles prompt generation for individual stories and bulk operations
 * Extends BaseStoriesManager for shared functionality
 */
class PromptGenerator extends BaseStoriesManager {
    constructor() {
        super();
        console.log('PromptGenerator initialized');
    }

    /**
     * Generate prompt for a single story
     * @param {string} storyId - Story ID
     * @param {number} storyIndex - Story index in the array
     */
    async generatePromptForStory(storyId, storyIndex) {
        console.log(`=== generatePromptForStory STARTED ===`);
        console.log(`storyId: ${storyId}, storyIndex: ${storyIndex}`);
        console.log(`Current generationId: ${this.generationId}, projectId: ${this.projectId}`);

        const story = this.getStoryById(storyId);
        if (!story) {
            console.error(`Story not found with ID: ${storyId}`);
            this.showNotification('Story not found.', 'error');
            return;
        }

        console.log(`Found story:`, story);

        // Check story status - only approved stories can have prompts generated
        const storyStatus = window.StatusUtils.normalizeStoryStatus(story.status);
        console.log(`Story status: ${storyStatus} (${window.StatusUtils.getStatusName(storyStatus)})`);

        if (!window.StatusUtils.canGeneratePrompt(storyStatus, story.hasPrompt)) {
            console.warn(`Cannot generate prompt - status: ${storyStatus}, hasPrompt: ${story.hasPrompt}`);
            if (storyStatus !== window.StatusUtils.StoryStatus.APPROVED) {
                this.showNotification('Story must be approved before generating a prompt.', 'warning');
            } else {
                this.showNotification('Prompt has already been generated for this story.', 'info');
            }
            return;
        }

        console.log(`✅ Story validation passed - status is approved`);
        const loadingOverlay = this.showLoadingOverlay('Generating prompt...');

        try {
            console.log(`Generating prompt for story ${storyId} at index ${storyIndex}`);

            // Validate GUID format
            const generationId = this.generationId;
            if (!generationId || typeof generationId !== 'string' || generationId.length !== 36) {
                console.error(`Invalid generationId format: ${generationId}. Expected a valid GUID string.`);
                this.showNotification('Invalid generation ID format. Please refresh the page and try again.', 'error');
                return;
            }

            // Frontend already has all information needed - skip unnecessary API call
            console.log(`Frontend validation passed - proceeding directly to prompt generation`);

            // Create prompt generation request
            // Use the individual story ID instead of the generation container ID
            const request = {
                StoryGenerationId: storyId,  // This is actually the individual UserStory ID
                StoryIndex: storyIndex,
                TechnicalPreferences: {},
                PromptStyle: null
            };

            console.log(`Sending prompt generation request:`, request);

            let result;
            try {
                console.log(`Calling APIClient.generatePrompt with request:`, request);
                result = await APIClient.generatePrompt(request);
                console.log(`APIClient.generatePrompt returned:`, result);

                // Enhanced validation for different response formats
                if (!result) {
                    console.error('Empty response from generatePrompt');
                    this.showNotification('Empty response from prompt generation service.', 'error');
                    return;
                }

                // Handle different possible response formats
                let promptId = result.promptId || result.PromptId || result.id || result.Id;

                if (!promptId) {
                    console.error('Invalid response from generatePrompt - no prompt ID found. Response:', result);
                    this.showNotification('Invalid response from prompt generation service - no prompt ID found.', 'error');
                    return;
                }

                console.log(`Extracted promptId: ${promptId}`);
                result = { promptId: promptId }; // Normalize the result

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
                    this.showNotification('Prompt generation service is being developed. Using mock prompt for now.', 'info');

                    // Generate a mock prompt ID for development/testing
                    const mockPromptId = `mock-prompt-${storyId}-${Date.now()}`;
                    result = { promptId: mockPromptId };

                    // Create a mock prompt content for the story
                    const mockPrompt = this.manager.utils.generateMockPrompt(story);
                    console.log('Generated mock prompt:', mockPrompt);

                } else {
                    this.showNotification(`Failed to generate prompt: ${error.message}`, 'error');
                    return;
                }
            }

            // Update local story state with prompt information
            story.hasPrompt = true;
            story.promptId = result.promptId;

            // Trigger re-render to update the UI
            if (this.manager && this.manager.renderer) {
                this.manager.renderer.renderStories();
            }
            if (this.manager && this.manager.renderer) {
                this.manager.renderer.updateProgress();
            }
            this.showNotification('Prompt generated successfully!', 'success');

        } catch (error) {
            console.error('Failed to generate prompt:', error);
            console.error('Error details:', {
                message: error.message,
                stack: error.stack,
                storyId: storyId,
                storyIndex: storyIndex,
                generationId: this.generationId
            });
            this.showNotification('Failed to generate prompt. Please try again.', 'error');
        } finally {
            this.hideLoadingOverlay(loadingOverlay);
        }
    }

    /**
     * Generate prompts for all approved stories that don't have prompts
     */
    async generatePromptsForApproved() {
        const approvedStoriesWithoutPrompts = this.stories.filter(s =>
            window.StatusUtils.canGeneratePrompt(s.status, s.hasPrompt));

        if (approvedStoriesWithoutPrompts.length === 0) {
            this.showNotification('No approved stories without prompts found.', 'info');
            return;
        }

        if (!confirm(`Generate prompts for ${approvedStoriesWithoutPrompts.length} approved stories?`)) {
            return;
        }

        if (this.isLoading) {
            this.showNotification('Please wait for current operations to complete.', 'warning');
            return;
        }

        // Validate GUID format
        const generationId = this.generationId;
        if (!generationId || typeof generationId !== 'string' || generationId.length !== 36) {
            console.error(`Invalid generationId format: ${generationId}. Expected a valid GUID string.`);
            this.showNotification('Invalid generation ID format. Please refresh the page and try again.', 'error');
            return;
        }

        const loadingOverlay = this.showLoadingOverlay('Generating prompts for approved stories...');

        try {
            console.log(`Generating prompts for ${approvedStoriesWithoutPrompts.length} approved stories`);

            // Generate prompts one by one
            for (const story of approvedStoriesWithoutPrompts) {
                const storyIndex = this.stories.indexOf(story);

                // Frontend already validated - proceed directly to generation
                console.log(`Proceeding with prompt generation for story ${story.id} at index ${storyIndex}`);

                // Generate prompt
                // Use the individual story ID instead of the generation container ID
                const request = {
                    StoryGenerationId: story.id,  // This is actually the individual UserStory ID
                    StoryIndex: storyIndex,
                    TechnicalPreferences: {},
                    PromptStyle: null
                };

                const result = await APIClient.generatePrompt(request);
                story.hasPrompt = true;
                story.promptId = result.promptId;
            }

            // Update UI
            if (this.manager && this.manager.renderer) {
                this.manager.renderer.renderStories();
            }
            if (this.manager && this.manager.renderer) {
                this.manager.renderer.updateProgress();
            }
            this.showNotification(`Prompts generated for ${approvedStoriesWithoutPrompts.length} stories!`, 'success');

        } catch (error) {
            console.error('Failed to generate prompts:', error);
            this.showNotification('Failed to generate some prompts. Please try again.', 'error');
        } finally {
            this.hideLoadingOverlay(loadingOverlay);
        }
    }

    /**
     * Generate prompt for current story (convenience method)
     */
    generatePromptForCurrentStory() {
        if (this.currentStory) {
            this.generatePromptForStory(this.currentStory.id, this.currentStory.index);
        } else {
            this.showNotification('No story selected.', 'warning');
        }
    }

    /**
     * Check if a story can have a prompt generated
     * @param {Object} story - Story object
     * @returns {boolean} True if prompt can be generated
     */
    canGeneratePromptForStory(story) {
        if (!story) return false;
        return window.StatusUtils.canGeneratePrompt(story.status, story.hasPrompt);
    }

    /**
     * Get stories that can have prompts generated
     * @returns {Array} Array of stories eligible for prompt generation
     */
    getStoriesEligibleForPromptGeneration() {
        return this.stories.filter(s => this.canGeneratePromptForStory(s));
    }

    /**
     * Get count of stories with prompts
     * @returns {number} Number of stories with prompts
     */
    getStoriesWithPromptsCount() {
        return this.stories.filter(s => s.hasPrompt).length;
    }

    /**
     * Get count of stories eligible for prompt generation
     * @returns {number} Number of stories eligible for prompt generation
     */
    getStoriesEligibleForPromptsCount() {
        return this.getStoriesEligibleForPromptGeneration().length;
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
     * Validate prompt generation request
     * @param {Object} story - Story object
     * @param {number} storyIndex - Story index
     * @returns {Object} Validation result { valid: boolean, message: string }
     */
    validatePromptGeneration(story, storyIndex) {
        if (!story) {
            return { valid: false, message: 'Story not found' };
        }

        if (this.isLoading) {
            return { valid: false, message: 'Please wait for current operations to complete' };
        }

        if (!this.canGeneratePromptForStory(story)) {
            const storyStatus = window.StatusUtils.normalizeStoryStatus(story.status);
            if (storyStatus !== window.StatusUtils.StoryStatus.APPROVED) {
                return { valid: false, message: 'Story must be approved before generating a prompt' };
            } else {
                return { valid: false, message: 'Prompt has already been generated for this story' };
            }
        }

        // Validate GUID format
        if (!this.generationId || typeof this.generationId !== 'string' || this.generationId.length !== 36) {
            return { valid: false, message: 'Invalid generation ID format' };
        }

        return { valid: true, message: '' };
    }

    /**
     * Get prompt generation summary
     * @returns {Object} Summary data
     */
    getPromptGenerationSummary() {
        const total = this.getStoriesCount();
        const withPrompts = this.getStoriesWithPromptsCount();
        const eligible = this.getStoriesEligibleForPromptsCount();

        return {
            total,
            withPrompts,
            eligible,
            withoutPrompts: total - withPrompts,
            percentageWithPrompts: total > 0 ? Math.round((withPrompts / total) * 100) : 0,
            canGeneratePrompts: eligible > 0
        };
    }
}
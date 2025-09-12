/**
 * StoriesOverviewManager - Main orchestrator for stories overview functionality
 * Coordinates all modular components and provides unified API
 */
class StoriesOverviewManager extends BaseStoriesManager {
    constructor() {
        super();

        // Initialize modular components
        this.renderer = new StoryRenderer();
        this.actions = new StoryActions();
        this.promptGen = new PromptGenerator();
        this.modals = new ModalManager();
        this.utils = new StoryUtils();

        console.log('StoriesOverviewManager initialized with modular components');

        // Set up component references
        this.setupComponentReferences();
    }

    /**
     * Set up cross-references between components
     */
    setupComponentReferences() {
        console.log('Setting up component references...');

        // Set up manager reference for all components
        [this.renderer, this.actions, this.promptGen, this.modals, this.utils].forEach(component => {
            component.manager = this;

            // Share core state properties
            Object.defineProperty(component, 'generationId', {
                get: () => this.generationId,
                set: (value) => { this.generationId = value; }
            });

            Object.defineProperty(component, 'projectId', {
                get: () => this.projectId,
                set: (value) => { this.projectId = value; }
            });

            Object.defineProperty(component, 'stories', {
                get: () => this.stories,
                set: (value) => { this.stories = value; }
            });

            Object.defineProperty(component, 'currentStory', {
                get: () => this.currentStory,
                set: (value) => { this.currentStory = value; }
            });

            Object.defineProperty(component, 'isLoading', {
                get: () => this.isLoading,
                set: (value) => { this.isLoading = value; }
            });

            // Share utility methods - use bind to avoid circular references
            component.showNotification = this.showNotification.bind(this);
            component.showLoadingOverlay = this.showLoadingOverlay.bind(this);
            component.hideLoadingOverlay = this.hideLoadingOverlay.bind(this);
            component.setLoadingState = this.setLoadingState.bind(this);
            component.showError = this.showError.bind(this);
            component.isValidGuid = this.isValidGuid.bind(this);
            component.generateMockPrompt = this.generateMockPrompt.bind(this);
        });

        console.log('Component references setup complete');
    }

    /**
     * Initialize the stories overview manager
     * @param {string} generationId - The story generation ID
     * @param {string} projectId - The project ID
     */
    async initialize(generationId, projectId) {
        try {
            console.log(`StoriesOverviewManager initializing with generationId=${generationId}, projectId=${projectId}`);

            // Initialize base manager
            super.initialize(generationId, projectId);

            // Initialize all components
            await this.initializeComponents();

            // Setup modal event handlers
            this.modals.setupModalEventHandlers();

            // Load initial data
            await this.loadStories();

            // Start auto-refresh
            this.startAutoRefresh();

            console.log('StoriesOverviewManager initialized successfully');

        } catch (error) {
            console.error('Failed to initialize StoriesOverviewManager:', error);
            this.showError('Failed to initialize Stories Overview. Please refresh the page.');
            throw error;
        }
    }

    /**
     * Initialize all modular components
     */
    async initializeComponents() {
        console.log('Initializing modular components...');

        // Set up cross-references between components
        this.setupComponentReferences();

        console.log('All modular components initialized');
    }

    /**
     * Load stories from API
     */
    async loadStories() {
        if (this.isLoading) return;

        this.setLoadingState(true);

        try {
            console.log(`Loading stories for generation ${this.generationId}`);

            // Validate GUID format before API call
            if (!this.isValidGuid(this.generationId)) {
                throw new Error('Invalid generation ID format');
            }

            const stories = await APIClient.getStories(this.generationId);
            console.log('Loaded stories:', stories);

            this.stories = stories || [];

            // Render stories using the renderer component
            this.renderer.renderStories();
            this.renderer.updateProgress();

            console.log(`Successfully loaded ${this.stories.length} stories`);

        } catch (error) {
            console.error('Failed to load stories:', error);
            this.showError('Failed to load stories. Please try again.');
        } finally {
            this.setLoadingState(false);
        }
    }

    /**
     * Refresh stories (reload from API)
     */
    refreshStories() {
        console.log('Refreshing stories...');
        return this.loadStories();
    }

    /**
     * Update progress display
     */
    updateProgress() {
        this.renderer.updateProgress();
    }

    /**
     * Export all stories
     */
    exportStories() {
        this.utils.exportStories();
    }

    /**
     * Continue to workflow
     */
    continueToWorkflow() {
        this.utils.continueToWorkflow();
    }

    /**
     * View story details
     * @param {number} index - Story index
     */
    viewStory(index) {
        this.modals.viewStory(index);
    }

    /**
     * Approve story
     * @param {string} storyId - Story ID
     */
    async approveStory(storyId) {
        await this.actions.approveStory(storyId);
    }

    /**
     * Reject story
     * @param {string} storyId - Story ID
     */
    async rejectStory(storyId) {
        await this.actions.rejectStory(storyId);
    }

    /**
     * Approve all pending stories
     */
    async approveAllStories() {
        await this.actions.approveAllStories();
    }

    /**
     * Generate prompt for story
     * @param {string} storyId - Story ID
     * @param {number} storyIndex - Story index
     */
    async generatePromptForStory(storyId, storyIndex) {
        await this.promptGen.generatePromptForStory(storyId, storyIndex);
    }

    /**
     * Generate prompts for all approved stories
     */
    async generatePromptsForApproved() {
        await this.promptGen.generatePromptsForApproved();
    }

    /**
     * View prompt
     * @param {string} promptId - Prompt ID
     */
    async viewPrompt(promptId) {
        await this.modals.viewPrompt(promptId);
    }

    /**
     * Edit current story
     */
    editCurrentStory() {
        this.modals.editCurrentStory();
    }

    /**
     * Save edited story
     */
    async saveEditedStory() {
        // This method needs to be implemented with story editing logic
        if (this.currentStory) {
            const title = document.getElementById('edit-title').value.trim();
            const description = document.getElementById('edit-description').value.trim();
            const criteriaText = document.getElementById('edit-criteria').value.trim();
            const priority = document.getElementById('edit-priority').value;
            const points = parseInt(document.getElementById('edit-points').value) || null;

            if (!title || !description) {
                this.showNotification('Title and description are required.', 'warning');
                return;
            }

            // Parse acceptance criteria
            const acceptanceCriteria = criteriaText
                ? criteriaText.split('\n').filter(line => line.trim()).map(line => line.trim())
                : [];

            const updatedStory = {
                title,
                description,
                acceptanceCriteria,
                priority,
                storyPoints: points
            };

            try {
                console.log(`Saving edited story ${this.currentStory.id}:`, updatedStory);

                // Validate GUID format
                if (!this.isValidGuid(this.currentStory.id)) {
                    throw new Error('Invalid story ID format');
                }

                await APIClient.editStory(this.currentStory.id, updatedStory);

                // Update local story
                const storyIndex = this.stories.findIndex(s => s.id === this.currentStory.id);
                if (storyIndex !== -1) {
                    this.stories[storyIndex] = { ...this.stories[storyIndex], ...updatedStory };
                }

                this.modals.closeEditModal();
                this.renderer.renderStories();
                this.showNotification('Story updated successfully!', 'success');

            } catch (error) {
                console.error('Failed to update story:', error);
                this.showNotification('Failed to update story. Please try again.', 'error');
            }
        } else {
            this.showNotification('No story selected for editing.', 'warning');
        }
    }

    /**
     * Modal action methods (for backward compatibility)
     */
    closeStoryModal() {
        this.modals.closeStoryModal();
    }

    closeEditModal() {
        this.modals.closeEditModal();
    }

    closePromptModal() {
        this.modals.closePromptModal();
    }

    copyPrompt() {
        this.modals.copyPrompt();
    }

    editPrompt() {
        this.modals.editPrompt();
    }

    savePromptEdit() {
        this.modals.savePromptEdit();
    }

    exportPrompt() {
        this.modals.exportPrompt();
    }

    approveCurrentStory() {
        this.actions.approveCurrentStory();
    }

    rejectCurrentStory() {
        this.actions.rejectCurrentStory();
    }

    generatePromptForCurrentStory() {
        this.promptGen.generatePromptForCurrentStory();
    }

    /**
     * Get current story
     * @returns {Object|null} Current story or null
     */
    getCurrentStory() {
        return this.currentStory;
    }

    /**
     * Get stories
     * @returns {Array} Stories array
     */
    getStories() {
        return this.stories;
    }

    /**
     * Get summary statistics
     * @returns {Object} Summary statistics
     */
    getSummary() {
        return this.utils.generateSummary();
    }

    /**
     * Get action summary
     * @returns {Object} Action summary
     */
    getActionSummary() {
        return this.actions.getActionSummary();
    }

    /**
     * Get prompt generation summary
     * @returns {Object} Prompt generation summary
     */
    getPromptGenerationSummary() {
        return this.promptGen.getPromptGenerationSummary();
    }

    /**
     * Cleanup resources
     */
    destroy() {
        console.log('Destroying StoriesOverviewManager...');

        // Cleanup all components
        this.modals.cleanupModals();
        this.stopAutoRefresh();

        console.log('StoriesOverviewManager destroyed');
    }

    /**
     * Get manager status
     * @returns {Object} Status information
     */
    getStatus() {
        return {
            initialized: !!this.generationId && !!this.projectId,
            loading: this.isLoading,
            storiesCount: this.getStoriesCount(),
            currentStory: this.currentStory,
            components: {
                renderer: !!this.renderer,
                actions: !!this.actions,
                promptGen: !!this.promptGen,
                modals: !!this.modals,
                utils: !!this.utils
            }
        };
    }
}

// Initialize global manager (for backward compatibility)
window.storiesOverviewManager = new StoriesOverviewManager();
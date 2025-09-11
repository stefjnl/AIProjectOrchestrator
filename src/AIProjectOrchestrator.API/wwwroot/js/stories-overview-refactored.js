/**
 * StoriesOverview Manager - Refactored Version
 * Handles individual story management and approval with service-based architecture
 */

class StoriesOverviewManager {
    constructor() {
        // Core properties
        this.generationId = null;
        this.projectId = null;
        this.stories = [];
        this.currentStory = null;
        this.isLoading = false;
        this.autoRefreshInterval = null;

        // Initialize services
        this.initializeServices();
    }

    initializeServices() {
        // Core services
        this.storyApiService = new StoryApiService();
        this.statusUtils = new StatusUtils();
        this.storyRenderer = new StoryRenderer(this.statusUtils);
        this.progressRenderer = new ProgressRenderer();
        this.exportService = new ExportService();

        // Modal services
        this.storyModalService = new StoryModalService();
        this.promptModalService = new PromptModalService();

        // Prompt service
        this.promptService = new PromptService();
    }

    initialize(generationId, projectId) {
        this.generationId = generationId;
        this.projectId = projectId;
        console.log(`StoriesOverview initialized with generationId=${generationId}, projectId=${projectId}`);

        this.loadStories();
        this.startAutoRefresh();
    }

    // Core Data Operations
    async loadStories() {
        if (this.isLoading) return;

        this.isLoading = true;
        this.progressRenderer.showLoadingSpinner('Loading stories...');

        try {
            console.log(`Loading stories for generation ${this.generationId}`);
            const stories = await this.storyApiService.getStories(this.generationId);
            console.log('Loaded stories:', stories);

            this.stories = stories || [];
            this.renderStories();
            this.updateProgress();

        } catch (error) {
            console.error('Failed to load stories:', error);
            this.showError('Failed to load stories. Please try again.');
        } finally {
            this.isLoading = false;
            this.progressRenderer.hideLoadingSpinner();
        }
    }

    refreshStories() {
        this.loadStories();
    }

    // UI Rendering Operations
    renderStories() {
        const storiesGrid = document.getElementById('stories-grid');
        if (!storiesGrid) return;

        try {
            if (!this.stories || this.stories.length === 0) {
                storiesGrid.innerHTML = this.storyRenderer.renderEmptyState();
                return;
            }

            storiesGrid.innerHTML = this.storyRenderer.renderStories(this.stories);
            this.attachStoryCardListeners();

        } catch (error) {
            console.error('Error rendering stories:', error);
            storiesGrid.innerHTML = this.storyRenderer.renderErrorState('Error displaying stories');
        }
    }

    attachStoryCardListeners() {
        // Event listeners are attached via onclick attributes in the rendered HTML
        // This method is kept for future event delegation if needed
    }

    updateProgress() {
        try {
            const stats = this.statusUtils.calculateApprovalStats(this.stories);
            const promptStats = this.statusUtils.calculatePromptStats(this.stories);

            // Render progress summary
            const progressContainer = document.getElementById('stories-summary');
            if (progressContainer) {
                progressContainer.innerHTML = this.storyRenderer.renderStorySummary(stats);
            }

            // Update button states
            const buttonStates = this.statusUtils.getButtonStates(this.stories);
            this.progressRenderer.updateButtonStates(buttonStates);

        } catch (error) {
            console.error('Error updating progress:', error);
        }
    }

    // Story Actions
    async approveStory(storyId) {
        const confirmed = await this.progressRenderer.showConfirmation(
            'Are you sure you want to approve this story?',
            'Confirm Approval'
        );

        if (!confirmed) return;

        const loadingOverlay = this.progressRenderer.showLoadingSpinner('Approving story...');

        try {
            console.log(`Approving story ${storyId}`);
            await this.storyApiService.approveStory(storyId);

            // Update local story status
            const story = this.stories.find(s => s.id === storyId);
            if (story) {
                story.status = 'approved';
            }

            this.renderStories();
            this.updateProgress();
            this.progressRenderer.showNotification('Story approved successfully!', 'success');

        } catch (error) {
            console.error('Failed to approve story:', error);
            this.progressRenderer.showNotification('Failed to approve story. Please try again.', 'error');
        } finally {
            this.progressRenderer.hideLoadingSpinner(loadingOverlay);
        }
    }

    async rejectStory(storyId) {
        const feedback = prompt('Please provide feedback for rejecting this story:');
        if (!feedback) {
            this.progressRenderer.showNotification('Rejection cancelled - feedback required.', 'info');
            return;
        }

        const loadingOverlay = this.progressRenderer.showLoadingSpinner('Rejecting story...');

        try {
            console.log(`Rejecting story ${storyId} with feedback: ${feedback}`);
            await this.storyApiService.rejectStory(storyId, { feedback });

            // Update local story status
            const story = this.stories.find(s => s.id === storyId);
            if (story) {
                story.status = 'rejected';
                story.rejectionFeedback = feedback;
            }

            this.renderStories();
            this.updateProgress();
            this.progressRenderer.showNotification('Story rejected successfully!', 'success');

        } catch (error) {
            console.error('Failed to reject story:', error);
            this.progressRenderer.showNotification('Failed to reject story. Please try again.', 'error');
        } finally {
            this.progressRenderer.hideLoadingSpinner(loadingOverlay);
        }
    }

    async approveAllStories() {
        const pendingStories = this.stories.filter(s =>
            this.statusUtils.canApproveStory(s.status));

        if (pendingStories.length === 0) {
            this.progressRenderer.showNotification('No pending stories to approve.', 'info');
            return;
        }

        const confirmed = await this.progressRenderer.showConfirmation(
            `Are you sure you want to approve all ${pendingStories.length} pending stories?`,
            'Confirm Bulk Approval'
        );

        if (!confirmed) return;

        const loadingOverlay = this.progressRenderer.showLoadingSpinner('Approving all stories...');

        try {
            console.log(`Approving all ${pendingStories.length} pending stories`);

            // Approve stories one by one (since there's no bulk approve endpoint)
            for (const story of pendingStories) {
                await this.storyApiService.approveStory(story.id);
                story.status = 'approved';
            }

            this.renderStories();
            this.updateProgress();
            this.progressRenderer.showNotification(
                `All ${pendingStories.length} stories approved successfully!`,
                'success'
            );

        } catch (error) {
            console.error('Failed to approve all stories:', error);
            this.progressRenderer.showNotification(
                'Failed to approve some stories. Please try again.',
                'error'
            );
        } finally {
            this.progressRenderer.hideLoadingSpinner(loadingOverlay);
        }
    }

    // Prompt Generation
    async generatePromptForStory(storyId, storyIndex) {
        console.log(`=== generatePromptForStory STARTED ===`);
        console.log(`storyId: ${storyId}, storyIndex: ${storyIndex}`);
        console.log(`Current generationId: ${this.generationId}, projectId: ${this.projectId}`);

        const story = this.stories.find(s => s.id === storyId);
        if (!story) {
            console.error(`Story not found with ID: ${storyId}`);
            this.progressRenderer.showNotification('Story not found.', 'error');
            return;
        }

        console.log(`Found story:`, story);

        // Validate story status and prompt eligibility
        const validation = this.promptService.validatePromptGeneration(story);
        if (!validation.isValid) {
            this.progressRenderer.showNotification(validation.message, validation.type);
            return;
        }

        console.log(`✅ Story validation passed`);

        const loadingOverlay = this.progressRenderer.showLoadingSpinner('Generating prompt...');

        try {
            console.log(`Generating prompt for story ${storyId} at index ${storyIndex}`);

            // Create prompt generation request
            const request = {
                StoryGenerationId: storyId,
                StoryIndex: storyIndex,
                TechnicalPreferences: {},
                PromptStyle: null
            };

            console.log(`Sending prompt generation request:`, request);

            // Generate prompt with fallback support
            const result = await this.promptService.generatePrompt(request, story);

            if (!result.success) {
                this.progressRenderer.showNotification(result.message, 'error');
                return;
            }

            // Update local story state with prompt information
            story.hasPrompt = true;
            story.promptId = result.promptId;

            this.renderStories();
            this.updateProgress();
            this.progressRenderer.showNotification('Prompt generated successfully!', 'success');

        } catch (error) {
            console.error('Failed to generate prompt:', error);
            this.progressRenderer.showNotification('Failed to generate prompt. Please try again.', 'error');
        } finally {
            this.progressRenderer.hideLoadingSpinner(loadingOverlay);
        }
    }

    async generatePromptsForApproved() {
        const approvedStoriesWithoutPrompts = this.stories.filter(s =>
            this.statusUtils.canGeneratePrompt(s.status, s.hasPrompt));

        if (approvedStoriesWithoutPrompts.length === 0) {
            this.progressRenderer.showNotification('No approved stories without prompts found.', 'info');
            return;
        }

        const confirmed = await this.progressRenderer.showConfirmation(
            `Generate prompts for ${approvedStoriesWithoutPrompts.length} approved stories?`,
            'Confirm Bulk Prompt Generation'
        );

        if (!confirmed) return;

        const loadingOverlay = this.progressRenderer.showLoadingSpinner(
            'Generating prompts for approved stories...'
        );

        try {
            console.log(`Generating prompts for ${approvedStoriesWithoutPrompts.length} approved stories`);

            // Generate prompts one by one with progress tracking
            const steps = approvedStoriesWithoutPrompts.map((story, index) => ({
                name: `Generating prompt for "${story.title || 'Untitled Story'}"`
            }));

            for (let i = 0; i < approvedStoriesWithoutPrompts.length; i++) {
                const story = approvedStoriesWithoutPrompts[i];
                const storyIndex = this.stories.indexOf(story);

                this.progressRenderer.showDetailedProgress(steps, i);

                const request = {
                    StoryGenerationId: story.id,
                    StoryIndex: storyIndex,
                    TechnicalPreferences: {},
                    PromptStyle: null
                };

                const result = await this.promptService.generatePrompt(request, story);

                if (result.success) {
                    story.hasPrompt = true;
                    story.promptId = result.promptId;
                }
            }

            this.renderStories();
            this.updateProgress();
            this.progressRenderer.showNotification(
                `Prompts generated for ${approvedStoriesWithoutPrompts.length} stories!`,
                'success'
            );

        } catch (error) {
            console.error('Failed to generate prompts:', error);
            this.progressRenderer.showNotification(
                'Failed to generate some prompts. Please try again.',
                'error'
            );
        } finally {
            this.progressRenderer.hideLoadingSpinner(loadingOverlay);
            this.progressRenderer.hideProgress();
        }
    }

    // Modal Management
    viewStory(index) {
        console.log(`viewStory called with index: ${index}`);
        console.log(`Available stories:`, this.stories);
        console.log(`this.stories length:`, this.stories ? this.stories.length : 'undefined');

        // Safety checks
        if (!this.stories || this.stories.length === 0) {
            console.error('No stories loaded yet');
            this.progressRenderer.showNotification(
                'Stories are still loading. Please wait a moment and try again.',
                'warning'
            );
            return;
        }

        if (index < 0 || index >= this.stories.length) {
            console.error(`Invalid index ${index}. Stories array has ${this.stories.length} items.`);
            this.progressRenderer.showNotification('Invalid story index. Please try again.', 'error');
            return;
        }

        const story = this.stories[index];
        if (!story) {
            console.error(`No story found at index ${index}`);
            this.progressRenderer.showNotification('Story not found. Please try again.', 'error');
            return;
        }

        console.log(`Found story:`, story);
        this.currentStory = { ...story, index };
        this.storyModalService.showStoryModal(story, {
            onApprove: () => this.approveCurrentStory(),
            onReject: () => this.rejectCurrentStory(),
            onEdit: () => this.editCurrentStory(),
            onGeneratePrompt: () => this.generatePromptForCurrentStory(),
            onClose: () => this.closeStoryModal()
        });
    }

    closeStoryModal() {
        this.storyModalService.closeModal();
        this.currentStory = null;
    }

    editCurrentStory() {
        if (!this.currentStory) return;

        this.storyModalService.closeModal();

        this.storyModalService.showEditModal(this.currentStory, {
            onSave: (updatedStory) => this.saveEditedStory(updatedStory),
            onCancel: () => this.closeEditModal()
        });
    }

    closeEditModal() {
        this.storyModalService.closeEditModal();
    }

    async saveEditedStory(updatedStory) {
        if (!this.currentStory) return;

        // Validate the updated story
        const validation = this.statusUtils.validateStory(updatedStory);
        if (!validation.isValid) {
            this.progressRenderer.showNotification(
                validation.errors.join('. '),
                'warning'
            );
            return;
        }

        const loadingOverlay = this.progressRenderer.showLoadingSpinner('Saving story changes...');

        try {
            console.log(`Saving edited story ${this.currentStory.id}:`, updatedStory);
            await this.storyApiService.editStory(this.currentStory.id, updatedStory);

            // Update local story
            const storyIndex = this.stories.findIndex(s => s.id === this.currentStory.id);
            if (storyIndex !== -1) {
                this.stories[storyIndex] = { ...this.stories[storyIndex], ...updatedStory };
            }

            this.closeEditModal();
            this.renderStories();
            this.progressRenderer.showNotification('Story updated successfully!', 'success');

        } catch (error) {
            console.error('Failed to update story:', error);
            this.progressRenderer.showNotification('Failed to update story. Please try again.', 'error');
        } finally {
            this.progressRenderer.hideLoadingSpinner(loadingOverlay);
        }
    }

    // Current story actions from modal
    approveCurrentStory() {
        if (this.currentStory) {
            this.approveStory(this.currentStory.id);
        }
    }

    rejectCurrentStory() {
        if (this.currentStory) {
            this.rejectStory(this.currentStory.id);
        }
    }

    generatePromptForCurrentStory() {
        if (this.currentStory) {
            this.generatePromptForStory(this.currentStory.id, this.currentStory.index);
        }
    }

    // Prompt Viewer
    async viewPrompt(promptId) {
        if (!promptId) {
            this.progressRenderer.showNotification('No prompt ID available.', 'error');
            return;
        }

        const loadingOverlay = this.progressRenderer.showLoadingSpinner('Loading prompt...');

        try {
            const promptData = await this.storyApiService.getPrompt(promptId);

            if (!promptData || !promptData.generatedPrompt) {
                this.progressRenderer.showNotification('Prompt not found or empty.', 'error');
                return;
            }

            this.promptModalService.showPromptModal(promptData, {
                onCopy: () => this.progressRenderer.showNotification('Prompt copied to clipboard!', 'success'),
                onEdit: () => this.progressRenderer.showNotification('Prompt is now editable.', 'info'),
                onExport: () => this.progressRenderer.showNotification('Prompt exported successfully!', 'success'),
                onClose: () => this.promptModalService.closeModal()
            });

        } catch (error) {
            console.error('Failed to load prompt:', error);
            this.progressRenderer.showNotification('Failed to load prompt. Please try again.', 'error');
        } finally {
            this.progressRenderer.hideLoadingSpinner(loadingOverlay);
        }
    }

    // Utility Operations
    continueToWorkflow() {
        // Navigate back to the workflow at Stage 4 (Prompt Review)
        window.location.href = `/Projects/Workflow?projectId=${this.projectId}`;
    }

    exportStories() {
        if (!this.stories || this.stories.length === 0) {
            this.progressRenderer.showNotification('No stories to export.', 'warning');
            return;
        }

        try {
            const data = {
                generationId: this.generationId,
                projectId: this.projectId,
                exportDate: new Date().toISOString(),
                stories: this.stories
            };

            this.exportService.exportAsJson(data, `stories-overview-${this.generationId}.json`);
            this.progressRenderer.showNotification('Stories exported successfully!', 'success');

        } catch (error) {
            console.error('Failed to export stories:', error);
            this.progressRenderer.showNotification('Failed to export stories. Please try again.', 'error');
        }
    }

    showError(message) {
        const storiesGrid = document.getElementById('stories-grid');
        if (storiesGrid) {
            storiesGrid.innerHTML = this.storyRenderer.renderErrorState(message);
        }
    }

    // Auto-refresh functionality
    startAutoRefresh() {
        if (this.autoRefreshInterval) return;

        this.autoRefreshInterval = setInterval(() => {
            this.refreshStories();
        }, 30000); // Refresh every 30 seconds

        console.log('Auto-refresh started for StoriesOverview');
    }

    stopAutoRefresh() {
        if (this.autoRefreshInterval) {
            clearInterval(this.autoRefreshInterval);
            this.autoRefreshInterval = null;
            console.log('Auto-refresh stopped for StoriesOverview');
        }
    }

    // Mock prompt generation (fallback)
    generateMockPrompt(story) {
        return this.promptService.generateMockPrompt(story);
    }

    // Cleanup
    destroy() {
        this.stopAutoRefresh();
        this.storyModalService.closeModal();
        this.promptModalService.closeModal();
    }
}

// Initialize global manager
window.storiesOverviewManager = new StoriesOverviewManager();

// Event handlers for backward compatibility
window.viewStory = (index) => window.storiesOverviewManager.viewStory(index);
window.approveStory = (storyId) => window.storiesOverviewManager.approveStory(storyId);
window.rejectStory = (storyId) => window.storiesOverviewManager.rejectStory(storyId);
window.generatePromptForStory = (storyId, index) => window.storiesOverviewManager.generatePromptForStory(storyId, index);
window.viewPrompt = (promptId) => window.storiesOverviewManager.viewPrompt(promptId);
window.refreshStories = () => window.storiesOverviewManager.refreshStories();
window.approveAllStories = () => window.storiesOverviewManager.approveAllStories();
window.generatePromptsForApproved = () => window.storiesOverviewManager.generatePromptsForApproved();
window.continueToWorkflow = () => window.storiesOverviewManager.continueToWorkflow();
window.exportStories = () => window.storiesOverviewManager.exportStories();

// Handle modal close on outside click
document.addEventListener('click', function (event) {
    const storyModal = document.getElementById('story-modal');
    const editModal = document.getElementById('edit-modal');
    const promptModal = document.getElementById('prompt-viewer-modal');

    if (event.target === storyModal) {
        window.storiesOverviewManager.closeStoryModal();
    }
    if (event.target === editModal) {
        window.storiesOverviewManager.closeEditModal();
    }
    if (event.target === promptModal) {
        window.storiesOverviewManager.promptModalService.closeModal();
    }
});

// Handle ESC key to close modals
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        window.storiesOverviewManager.closeStoryModal();
        window.storiesOverviewManager.closeEditModal();
        window.storiesOverviewManager.promptModalService.closeModal();
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    if (window.storiesOverviewManager) {
        window.storiesOverviewManager.destroy();
    }
});
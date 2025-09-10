// StoriesOverview Manager - Handles individual story management and approval
class StoriesOverviewManager {
    constructor() {
        this.generationId = null;
        this.projectId = null;
        this.stories = [];
        this.currentStory = null;
        this.isLoading = false;
        this.autoRefreshInterval = null;
    }

    initialize(generationId, projectId) {
        this.generationId = generationId;
        this.projectId = projectId;
        console.log(`StoriesOverview initialized with generationId=${generationId}, projectId=${projectId}`);

        this.loadStories();
        this.startAutoRefresh();
    }

    async loadStories() {
        if (this.isLoading) return;

        this.isLoading = true;
        const loadingElement = document.querySelector('.loading-state');
        if (loadingElement) {
            loadingElement.style.display = 'block';
        }

        try {
            console.log(`Loading stories for generation ${this.generationId}`);
            const stories = await APIClient.getStories(this.generationId);
            console.log('Loaded stories:', stories);

            this.stories = stories || [];
            this.renderStories();
            this.updateProgress();

        } catch (error) {
            console.error('Failed to load stories:', error);
            this.showError('Failed to load stories. Please try again.');
        } finally {
            this.isLoading = false;
            if (loadingElement) {
                loadingElement.style.display = 'none';
            }
        }
    }

    renderStories() {
        const storiesGrid = document.getElementById('stories-grid');
        if (!storiesGrid) return;

        if (!this.stories || this.stories.length === 0) {
            storiesGrid.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">📖</div>
                    <h3>No Stories Found</h3>
                    <p>No user stories are available for this generation.</p>
                    <button class="btn btn-primary" onclick="window.storiesOverviewManager.refreshStories()">
                        🔄 Try Again
                    </button>
                </div>
            `;
            return;
        }

        storiesGrid.innerHTML = `
            <div class="stories-grid">
                ${this.stories.map((story, index) => this.createStoryCard(story, index)).join('')}
            </div>
        `;

        // Add event listeners to story cards
        this.attachStoryCardListeners();
    }

    createStoryCard(story, index) {
        // Safely handle story status - ensure it's a string
        const storyStatus = story.status ? String(story.status) : 'pending';
        const statusClass = storyStatus.toLowerCase();
        const canApprove = storyStatus === 'pending';
        const canGeneratePrompt = storyStatus === 'approved' && !story.hasPrompt;

        return `
            <div class="story-card" data-story-id="${story.id}" data-story-index="${index}">
                <div class="story-header">
                    <h4>${story.title || 'Untitled Story'}</h4>
                    <span class="story-status ${statusClass}">${story.status || 'Unknown'}</span>
                </div>
                <p class="story-description">${story.description || 'No description available'}</p>
                <div class="story-meta">
                    <span class="story-points">Points: ${story.storyPoints || 'N/A'}</span>
                    <span class="story-priority priority-${(story.priority || 'medium').toLowerCase()}">${story.priority || 'Medium'}</span>
                    ${story.hasPrompt ? '<span class="prompt-indicator">✅ Prompt Generated</span>' : ''}
                </div>
                <div class="story-actions">
                    <button class="btn btn-sm btn-primary" onclick="window.storiesOverviewManager.viewStory(${index})">
                        👁️ View
                    </button>
                    ${canApprove ? `
                        <button class="btn btn-sm btn-success" onclick="window.storiesOverviewManager.approveStory('${story.id}')">
                            ✅ Approve
                        </button>
                        <button class="btn btn-sm btn-danger" onclick="window.storiesOverviewManager.rejectStory('${story.id}')">
                            ❌ Reject
                        </button>
                    ` : ''}
                    ${canGeneratePrompt ? `
                        <button class="btn btn-sm btn-primary" onclick="window.storiesOverviewManager.generatePromptForStory('${story.id}', ${index})">
                            🤖 Generate Prompt
                        </button>
                    ` : ''}
                </div>
            </div>
        `;
    }

    attachStoryCardListeners() {
        // Event listeners are already attached via onclick attributes
    }

    updateProgress() {
        const total = this.stories.length;
        const approved = this.stories.filter(s => s.status === 'approved' || String(s.status).toLowerCase() === 'approved').length;
        const rejected = this.stories.filter(s => s.status === 'rejected' || String(s.status).toLowerCase() === 'rejected').length;
        const promptsGenerated = this.stories.filter(s => s.hasPrompt).length;

        const approvalPercentage = total > 0 ? Math.round((approved / total) * 100) : 0;

        // Update progress display
        document.getElementById('approved-count').textContent = approved;
        document.getElementById('total-count').textContent = total;
        document.getElementById('prompts-count').textContent = promptsGenerated;
        document.getElementById('approval-progress').textContent = `${approvalPercentage}%`;

        // Update progress bar
        const progressFill = document.getElementById('progress-fill');
        if (progressFill) {
            progressFill.style.width = `${approvalPercentage}%`;
        }

        // Update button states
        this.updateActionButtons(approved, total, promptsGenerated);
    }

    updateActionButtons(approved, total, promptsGenerated) {
        const approveAllBtn = document.getElementById('approve-all-btn');
        const generatePromptsBtn = document.getElementById('generate-prompts-btn');
        const continueWorkflowBtn = document.getElementById('continue-workflow-btn');

        // Enable/disable approve all button
        if (approveAllBtn) {
            const hasPending = this.stories.some(s => s.status === 'pending' || String(s.status).toLowerCase() === 'pending');
            approveAllBtn.disabled = !hasPending;
            approveAllBtn.textContent = hasPending ? '✅ Approve All' : '✅ All Approved';
        }

        // Enable/disable generate prompts button
        if (generatePromptsBtn) {
            const hasApprovedWithoutPrompts = this.stories.some(s => (s.status === 'approved' || String(s.status).toLowerCase() === 'approved') && !s.hasPrompt);
            generatePromptsBtn.disabled = !hasApprovedWithoutPrompts;
            generatePromptsBtn.textContent = hasApprovedWithoutPrompts ? '🤖 Generate Prompts' : '🤖 All Prompts Generated';
        }

        // Show/hide continue to workflow button
        if (continueWorkflowBtn) {
            const hasPrompts = promptsGenerated > 0;
            continueWorkflowBtn.style.display = hasPrompts ? 'inline-block' : 'none';
        }
    }

    async approveStory(storyId) {
        if (!confirm('Are you sure you want to approve this story?')) {
            return;
        }

        try {
            console.log(`Approving story ${storyId}`);
            await APIClient.approveStory(storyId);

            // Update local story status
            const story = this.stories.find(s => s.id === storyId);
            if (story) {
                story.status = 'approved';
            }

            this.renderStories();
            this.updateProgress();
            window.App.showNotification('Story approved successfully!', 'success');

        } catch (error) {
            console.error('Failed to approve story:', error);
            window.App.showNotification('Failed to approve story. Please try again.', 'error');
        }
    }

    async rejectStory(storyId) {
        const feedback = prompt('Please provide feedback for rejecting this story:');
        if (!feedback) {
            window.App.showNotification('Rejection cancelled - feedback required.', 'info');
            return;
        }

        try {
            console.log(`Rejecting story ${storyId} with feedback: ${feedback}`);
            await APIClient.rejectStory(storyId, { feedback });

            // Update local story status
            const story = this.stories.find(s => s.id === storyId);
            if (story) {
                story.status = 'rejected';
                story.rejectionFeedback = feedback;
            }

            this.renderStories();
            this.updateProgress();
            window.App.showNotification('Story rejected successfully!', 'success');

        } catch (error) {
            console.error('Failed to reject story:', error);
            window.App.showNotification('Failed to reject story. Please try again.', 'error');
        }
    }

    async approveAllStories() {
        const pendingStories = this.stories.filter(s => s.status === 'pending' || String(s.status).toLowerCase() === 'pending');
        if (pendingStories.length === 0) {
            window.App.showNotification('No pending stories to approve.', 'info');
            return;
        }

        if (!confirm(`Are you sure you want to approve all ${pendingStories.length} pending stories?`)) {
            return;
        }

        const loadingOverlay = showLoading('Approving all stories...');
        try {
            console.log(`Approving all ${pendingStories.length} pending stories`);

            // Approve stories one by one (since there's no bulk approve endpoint)
            for (const story of pendingStories) {
                await APIClient.approveStory(story.id);
                story.status = 'approved';
            }

            this.renderStories();
            this.updateProgress();
            window.App.showNotification(`All ${pendingStories.length} stories approved successfully!`, 'success');

        } catch (error) {
            console.error('Failed to approve all stories:', error);
            window.App.showNotification('Failed to approve some stories. Please try again.', 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async generatePromptForStory(storyId, storyIndex) {
        const story = this.stories.find(s => s.id === storyId);
        if (!story || (story.status !== 'approved' && String(story.status).toLowerCase() !== 'approved')) {
            window.App.showNotification('Story must be approved before generating a prompt.', 'warning');
            return;
        }

        const loadingOverlay = showLoading('Generating prompt...');
        try {
            console.log(`Generating prompt for story ${storyId} at index ${storyIndex}`);

            // Check if prompt can be generated for this story
            const canGenerate = await APIClient.canGeneratePrompt(this.generationId, storyIndex);
            if (!canGenerate) {
                window.App.showNotification('Cannot generate prompt for this story at this time.', 'warning');
                return;
            }

            // Create prompt generation request
            const request = {
                StoryGenerationId: this.generationId,
                StoryIndex: storyIndex,
                TechnicalPreferences: {},
                PromptStyle: null
            };

            const result = await APIClient.generatePrompt(request);
            console.log('Prompt generation result:', result);

            // Update local story state
            story.hasPrompt = true;
            story.promptId = result.promptId;

            this.renderStories();
            this.updateProgress();
            window.App.showNotification('Prompt generated successfully!', 'success');

        } catch (error) {
            console.error('Failed to generate prompt:', error);
            window.App.showNotification('Failed to generate prompt. Please try again.', 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async generatePromptsForApproved() {
        const approvedStoriesWithoutPrompts = this.stories.filter(s => s.status === 'approved' || String(s.status).toLowerCase() === 'approved' && !s.hasPrompt);

        if (approvedStoriesWithoutPrompts.length === 0) {
            window.App.showNotification('No approved stories without prompts found.', 'info');
            return;
        }

        if (!confirm(`Generate prompts for ${approvedStoriesWithoutPrompts.length} approved stories?`)) {
            return;
        }

        const loadingOverlay = showLoading('Generating prompts for approved stories...');
        try {
            console.log(`Generating prompts for ${approvedStoriesWithoutPrompts.length} approved stories`);

            // Generate prompts one by one
            for (const story of approvedStoriesWithoutPrompts) {
                const storyIndex = this.stories.indexOf(story);

                // Check if prompt can be generated
                const canGenerate = await APIClient.canGeneratePrompt(this.generationId, storyIndex);
                if (!canGenerate) {
                    console.warn(`Cannot generate prompt for story ${story.id} at index ${storyIndex}`);
                    continue;
                }

                // Generate prompt
                const request = {
                    StoryGenerationId: this.generationId,
                    StoryIndex: storyIndex,
                    TechnicalPreferences: {},
                    PromptStyle: null
                };

                const result = await APIClient.generatePrompt(request);
                story.hasPrompt = true;
                story.promptId = result.promptId;
            }

            this.renderStories();
            this.updateProgress();
            window.App.showNotification(`Prompts generated for ${approvedStoriesWithoutPrompts.length} stories!`, 'success');

        } catch (error) {
            console.error('Failed to generate prompts:', error);
            window.App.showNotification('Failed to generate some prompts. Please try again.', 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    // Modal functions
    viewStory(index) {
        const story = this.stories[index];
        if (!story) return;

        this.currentStory = { ...story, index };
        this.showStoryModal(story);
    }

    showStoryModal(story) {
        const modal = document.getElementById('story-modal');
        if (!modal) return;

        // Populate modal content
        document.getElementById('modal-story-title').textContent = story.title || 'Untitled Story';
        document.getElementById('modal-story-description').textContent = story.description || 'No description available';

        // Format acceptance criteria
        const criteriaElement = document.getElementById('modal-story-criteria');
        if (story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)) {
            criteriaElement.innerHTML = `
                <ul>
                    ${story.acceptanceCriteria.map(criterion => `<li>${criterion}</li>`).join('')}
                </ul>
            `;
        } else {
            criteriaElement.innerHTML = '<p>No acceptance criteria specified.</p>';
        }

        document.getElementById('modal-story-priority').textContent = story.priority || 'Medium';
        document.getElementById('modal-story-points').textContent = story.storyPoints || 'N/A';
        document.getElementById('modal-story-status').textContent = storyStatus;
        document.getElementById('modal-story-prompt-status').textContent = story.hasPrompt ? 'Yes' : 'No';

        // Update button states
        const approveBtn = document.getElementById('modal-approve-btn');
        const rejectBtn = document.getElementById('modal-reject-btn');
        const generatePromptBtn = document.getElementById('modal-generate-prompt-btn');

        if (approveBtn) approveBtn.disabled = storyStatus !== 'pending';
        if (rejectBtn) rejectBtn.disabled = storyStatus !== 'pending';
        if (generatePromptBtn) generatePromptBtn.disabled = storyStatus !== 'approved' || story.hasPrompt;

        modal.style.display = 'block';
    }

    closeStoryModal() {
        const modal = document.getElementById('story-modal');
        if (modal) {
            modal.style.display = 'none';
        }
        this.currentStory = null;
    }

    editCurrentStory() {
        if (!this.currentStory) return;

        const story = this.currentStory;
        const editModal = document.getElementById('edit-modal');
        if (!editModal) return;

        // Populate edit form
        document.getElementById('edit-title').value = story.title || '';
        document.getElementById('edit-description').value = story.description || '';
        document.getElementById('edit-priority').value = story.priority || 'Medium';
        document.getElementById('edit-points').value = story.storyPoints || '';

        // Format acceptance criteria
        const criteriaText = story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)
            ? story.acceptanceCriteria.join('\n')
            : '';
        document.getElementById('edit-criteria').value = criteriaText;

        // Close story modal and open edit modal
        this.closeStoryModal();
        editModal.style.display = 'block';
    }

    closeEditModal() {
        const modal = document.getElementById('edit-modal');
        if (modal) {
            modal.style.display = 'none';
        }
    }

    async saveEditedStory() {
        if (!this.currentStory) return;

        const title = document.getElementById('edit-title').value.trim();
        const description = document.getElementById('edit-description').value.trim();
        const criteriaText = document.getElementById('edit-criteria').value.trim();
        const priority = document.getElementById('edit-priority').value;
        const points = parseInt(document.getElementById('edit-points').value) || null;

        if (!title || !description) {
            window.App.showNotification('Title and description are required.', 'warning');
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
            await APIClient.editStory(this.currentStory.id, updatedStory);

            // Update local story
            const storyIndex = this.stories.findIndex(s => s.id === this.currentStory.id);
            if (storyIndex !== -1) {
                this.stories[storyIndex] = { ...this.stories[storyIndex], ...updatedStory };
            }

            this.closeEditModal();
            this.renderStories();
            window.App.showNotification('Story updated successfully!', 'success');

        } catch (error) {
            console.error('Failed to update story:', error);
            window.App.showNotification('Failed to update story. Please try again.', 'error');
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

    // Utility functions
    refreshStories() {
        this.loadStories();
    }

    continueToWorkflow() {
        // Navigate back to the workflow at Stage 4 (Prompt Review)
        window.location.href = `/Projects/Workflow?projectId=${this.projectId}`;
    }

    exportStories() {
        if (!this.stories || this.stories.length === 0) {
            window.App.showNotification('No stories to export.', 'warning');
            return;
        }

        const data = {
            generationId: this.generationId,
            projectId: this.projectId,
            exportDate: new Date().toISOString(),
            stories: this.stories
        };

        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `stories-overview-${this.generationId}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);

        window.App.showNotification('Stories exported successfully!', 'success');
    }

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
    }

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

    // Cleanup
    destroy() {
        this.stopAutoRefresh();
        this.closeStoryModal();
        this.closeEditModal();
    }
}

// Initialize global manager
window.storiesOverviewManager = new StoriesOverviewManager();

// Handle modal close on outside click
document.addEventListener('click', function (event) {
    const storyModal = document.getElementById('story-modal');
    const editModal = document.getElementById('edit-modal');

    if (event.target === storyModal) {
        window.storiesOverviewManager.closeStoryModal();
    }
    if (event.target === editModal) {
        window.storiesOverviewManager.closeEditModal();
    }
});

// Handle ESC key to close modals
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        window.storiesOverviewManager.closeStoryModal();
        window.storiesOverviewManager.closeEditModal();
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    if (window.storiesOverviewManager) {
        window.storiesOverviewManager.destroy();
    }
});

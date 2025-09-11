// StoriesOverview Manager - Handles individual story management and approval
// Depends on status-utils.js for consistent status handling
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
        // Use status utilities for consistent handling
        const storyStatus = window.StatusUtils.normalizeStoryStatus(story.status);
        const statusClass = window.StatusUtils.getStatusClass(storyStatus);
        const statusName = window.StatusUtils.getStatusName(storyStatus);
        const canApprove = window.StatusUtils.canApproveStory(storyStatus);
        const canReject = window.StatusUtils.canRejectStory(storyStatus);
        const canGeneratePrompt = window.StatusUtils.canGeneratePrompt(storyStatus, story.hasPrompt);

        console.log(`Creating story card for index ${index}:`, {
            originalStatus: story.status,
            normalizedStatus: storyStatus,
            statusName: statusName,
            canApprove: canApprove,
            canReject: canReject,
            canGeneratePrompt: canGeneratePrompt,
            hasPrompt: story.hasPrompt
        });

        return `
            <div class="story-card" data-story-id="${story.id}" data-story-index="${index}">
                <div class="story-header">
                    <h4>${story.title || 'Untitled Story'}</h4>
                    <span class="story-status ${statusClass}">${statusName}</span>
                </div>
                <p class="story-description">${story.description || 'No description available'}</p>
                <div class="story-meta">
                    <span class="story-points">Points: ${story.storyPoints || 'N/A'}</span>
                    <span class="story-priority priority-${(story.priority || 'medium').toLowerCase()}">${story.priority || 'Medium'}</span>
                </div>
                <div class="story-actions">
                    <button class="btn btn-sm btn-primary" onclick="window.storiesOverviewManager.viewStory(${index})">
                        👁️ View
                    </button>
                    ${canApprove ? `
                        <button class="btn btn-sm btn-success" onclick="window.storiesOverviewManager.approveStory('${story.id}')">
                            ✅ Approve
                        </button>
                    ` : ''}
                    ${canReject ? `
                        <button class="btn btn-sm btn-danger" onclick="window.storiesOverviewManager.rejectStory('${story.id}')">
                            ❌ Reject
                        </button>
                    ` : ''}
                    ${canGeneratePrompt ? `
                        <button class="btn btn-sm btn-primary" onclick="console.log('Story card Generate Prompt clicked for story ${story.id}, index ${index}'); window.storiesOverviewManager.generatePromptForStory('${story.id}', ${index})">
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
        const approved = this.stories.filter(s =>
            window.StatusUtils.normalizeStoryStatus(s.status) === window.StatusUtils.StoryStatus.APPROVED).length;
        const rejected = this.stories.filter(s =>
            window.StatusUtils.normalizeStoryStatus(s.status) === window.StatusUtils.StoryStatus.REJECTED).length;

        const approvalPercentage = total > 0 ? Math.round((approved / total) * 100) : 0;

        // Update progress display
        document.getElementById('approved-count').textContent = approved;
        document.getElementById('total-count').textContent = total;
        document.getElementById('approval-progress').textContent = `${approvalPercentage}%`;

        // Update progress bar
        const progressFill = document.getElementById('progress-fill');
        if (progressFill) {
            progressFill.style.width = `${approvalPercentage}%`;
        }

        // Update button states
        this.updateActionButtons(approved, total);
    }

    updateActionButtons(approved, total) {
        const approveAllBtn = document.getElementById('approve-all-btn');
        const generatePromptsBtn = document.getElementById('generate-prompts-btn');
        const continueWorkflowBtn = document.getElementById('continue-workflow-btn');

        // Enable/disable approve all button
        if (approveAllBtn) {
            const hasPending = this.stories.some(s =>
                window.StatusUtils.canApproveStory(s.status));
            approveAllBtn.disabled = !hasPending;
            approveAllBtn.textContent = hasPending ? '✅ Approve All' : '✅ All Approved';
        }

        // Enable/disable generate prompts button
        if (generatePromptsBtn) {
            const hasApprovedWithoutPrompts = this.stories.some(s =>
                window.StatusUtils.canGeneratePrompt(s.status, s.hasPrompt));
            generatePromptsBtn.disabled = !hasApprovedWithoutPrompts;
            generatePromptsBtn.textContent = hasApprovedWithoutPrompts ? '🤖 Generate Prompts' : '🤖 All Prompts Generated';
        }

        // Show/hide continue to workflow button
        if (continueWorkflowBtn) {
            const hasPrompts = this.stories.some(s => s.hasPrompt);
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
                story.status = window.StatusUtils.StoryStatus.APPROVED;
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
                story.status = window.StatusUtils.StoryStatus.REJECTED;
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
        const pendingStories = this.stories.filter(s =>
            window.StatusUtils.canApproveStory(s.status));
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
                story.status = window.StatusUtils.StoryStatus.APPROVED;
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
        console.log(`=== generatePromptForStory STARTED ===`);
        console.log(`storyId: ${storyId}, storyIndex: ${storyIndex}`);
        console.log(`Current generationId: ${this.generationId}, projectId: ${this.projectId}`);

        const story = this.stories.find(s => s.id === storyId);
        if (!story) {
            console.error(`Story not found with ID: ${storyId}`);
            window.App.showNotification('Story not found.', 'error');
            return;
        }

        console.log(`Found story:`, story);

        // Check story status - only approved stories can have prompts generated
        const storyStatus = window.StatusUtils.normalizeStoryStatus(story.status);
        console.log(`Story status: ${storyStatus} (${window.StatusUtils.getStatusName(storyStatus)})`);

        if (!window.StatusUtils.canGeneratePrompt(storyStatus, story.hasPrompt)) {
            console.warn(`Cannot generate prompt - status: ${storyStatus}, hasPrompt: ${story.hasPrompt}`);
            if (storyStatus !== window.StatusUtils.StoryStatus.APPROVED) {
                window.App.showNotification('Story must be approved before generating a prompt.', 'warning');
            } else {
                window.App.showNotification('Prompt has already been generated for this story.', 'info');
            }
            return;
        }

        console.log(`✅ Story validation passed - status is approved (1)`);
        const loadingOverlay = showLoading('Generating prompt...');

        try {
            console.log(`Generating prompt for story ${storyId} at index ${storyIndex}`);

            // Validate GUID format
            const generationId = this.generationId;
            if (!generationId || typeof generationId !== 'string' || generationId.length !== 36) {
                console.error(`Invalid generationId format: ${generationId}. Expected a valid GUID string.`);
                window.App.showNotification('Invalid generation ID format. Please refresh the page and try again.', 'error');
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
                    window.App.showNotification('Empty response from prompt generation service.', 'error');
                    return;
                }

                // Handle different possible response formats
                let promptId = result.promptId || result.PromptId || result.id || result.Id;

                if (!promptId) {
                    console.error('Invalid response from generatePrompt - no prompt ID found. Response:', result);
                    window.App.showNotification('Invalid response from prompt generation service - no prompt ID found.', 'error');
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
                    window.App.showNotification('Prompt generation service is being developed. Using mock prompt for now.', 'info');

                    // Generate a mock prompt ID for development/testing
                    const mockPromptId = `mock-prompt-${storyId}-${Date.now()}`;
                    result = { promptId: mockPromptId };

                    // Create a mock prompt content for the story
                    const mockPrompt = this.generateMockPrompt(story);
                    console.log('Generated mock prompt:', mockPrompt);

                } else {
                    window.App.showNotification(`Failed to generate prompt: ${error.message}`, 'error');
                    return;
                }
            }

            // Update local story state with prompt information
            story.hasPrompt = true;
            story.promptId = result.promptId;

            this.renderStories();
            this.updateProgress();
            window.App.showNotification('Prompt generated successfully!', 'success');

        } catch (error) {
            console.error('Failed to generate prompt:', error);
            console.error('Error details:', {
                message: error.message,
                stack: error.stack,
                storyId: storyId,
                storyIndex: storyIndex,
                generationId: this.generationId
            });
            window.App.showNotification('Failed to generate prompt. Please try again.', 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async generatePromptsForApproved() {
        const approvedStoriesWithoutPrompts = this.stories.filter(s =>
            window.StatusUtils.canGeneratePrompt(s.status, s.hasPrompt));

        if (approvedStoriesWithoutPrompts.length === 0) {
            window.App.showNotification('No approved stories without prompts found.', 'info');
            return;
        }

        if (!confirm(`Generate prompts for ${approvedStoriesWithoutPrompts.length} approved stories?`)) {
            return;
        }

        // Validate GUID format
        const generationId = this.generationId;
        if (!generationId || typeof generationId !== 'string' || generationId.length !== 36) {
            console.error(`Invalid generationId format: ${generationId}. Expected a valid GUID string.`);
            window.App.showNotification('Invalid generation ID format. Please refresh the page and try again.', 'error');
            return;
        }

        const loadingOverlay = showLoading('Generating prompts for approved stories...');
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
        console.log(`viewStory called with index: ${index}`);
        console.log(`Available stories:`, this.stories);
        console.log(`StoriesOverviewManager instance:`, this);
        console.log(`this.stories length:`, this.stories ? this.stories.length : 'undefined');

        // Safety check - ensure stories are loaded
        if (!this.stories || this.stories.length === 0) {
            console.error('No stories loaded yet');
            window.App.showNotification('Stories are still loading. Please wait a moment and try again.', 'warning');
            return;
        }

        // Safety check - ensure index is valid
        if (index < 0 || index >= this.stories.length) {
            console.error(`Invalid index ${index}. Stories array has ${this.stories.length} items.`);
            window.App.showNotification('Invalid story index. Please try again.', 'error');
            return;
        }

        const story = this.stories[index];
        if (!story) {
            console.error(`No story found at index ${index}`);
            window.App.showNotification('Story not found. Please try again.', 'error');
            return;
        }

        console.log(`Found story:`, story);
        this.currentStory = { ...story, index };
        this.showStoryModal(story);
    }

    showStoryModal(story) {
        console.log(`showStoryModal called with story:`, story);
        const modal = document.getElementById('story-modal');
        console.log(`Modal element found:`, modal);

        if (!modal) {
            console.error('Story modal element not found!');
            window.App.showNotification('Story modal not found. Please refresh the page.', 'error');
            return;
        }

        // Use status utilities for consistent handling
        const storyStatus = window.StatusUtils.normalizeStoryStatus(story.status);
        const statusText = window.StatusUtils.getStatusName(storyStatus);
        const hasPrompt = Boolean(story.hasPrompt);
        console.log(`Story status: ${storyStatus}, statusText: ${statusText}, hasPrompt: ${hasPrompt}`);

        try {
            // Populate modal content
            const titleElement = document.getElementById('modal-story-title');
            const descriptionElement = document.getElementById('modal-story-description');
            const priorityElement = document.getElementById('modal-story-priority');
            const pointsElement = document.getElementById('modal-story-points');
            const statusElement = document.getElementById('modal-story-status');
            const promptStatusElement = document.getElementById('modal-story-prompt-status');

            console.log('Modal elements found:', {
                title: titleElement,
                description: descriptionElement,
                priority: priorityElement,
                points: pointsElement,
                status: statusElement,
                promptStatus: promptStatusElement
            });

            if (titleElement) titleElement.textContent = story.title || 'Untitled Story';
            if (descriptionElement) descriptionElement.textContent = story.description || 'No description available';
            if (priorityElement) priorityElement.textContent = story.priority || 'Medium';
            if (pointsElement) pointsElement.textContent = story.storyPoints || 'N/A';
            if (statusElement) statusElement.textContent = storyStatus;
            if (promptStatusElement) promptStatusElement.textContent = story.hasPrompt ? 'Yes' : 'No';

            // Format acceptance criteria
            const criteriaElement = document.getElementById('modal-story-criteria');
            if (criteriaElement) {
                if (story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)) {
                    criteriaElement.innerHTML = `
                        <ul class="acceptance-criteria-list">
                            ${story.acceptanceCriteria.map(criterion => `<li class="acceptance-criteria-item">${criterion}</li>`).join('')}
                        </ul>
                    `;
                } else {
                    criteriaElement.innerHTML = '<p class="text-gray-500">No acceptance criteria specified.</p>';
                }
            }

            // Update button states
            const approveBtn = document.getElementById('modal-approve-btn');
            const rejectBtn = document.getElementById('modal-reject-btn');
            const generatePromptBtn = document.getElementById('modal-generate-prompt-btn');

            console.log('Modal button states:', {
                storyStatus: storyStatus,
                statusText: statusText,
                hasPrompt: hasPrompt,
                approveBtn: approveBtn,
                rejectBtn: rejectBtn,
                generatePromptBtn: generatePromptBtn,
                approveDisabled: storyStatus !== 0,
                rejectDisabled: storyStatus !== 0,
                generateDisabled: storyStatus !== 1 || hasPrompt
            });

            if (approveBtn) approveBtn.disabled = !window.StatusUtils.canApproveStory(storyStatus);
            if (rejectBtn) rejectBtn.disabled = !window.StatusUtils.canRejectStory(storyStatus);
            if (generatePromptBtn) {
                generatePromptBtn.disabled = !window.StatusUtils.canGeneratePrompt(storyStatus, hasPrompt);

                // Add click event listener for debugging
                generatePromptBtn.onclick = () => {
                    console.log('Modal Generate Prompt button clicked!');
                    console.log('Current story:', this.currentStory);
                    if (this.currentStory) {
                        this.generatePromptForStory(this.currentStory.id, this.currentStory.index);
                    }
                };
            }

            console.log('Showing modal with CSS class...');

            // Use CSS class-based approach for better positioning
            modal.classList.add('show');

            // Force reflow to ensure styles are applied
            modal.offsetHeight;

            console.log('Modal should now be visible with proper positioning');

        } catch (error) {
            console.error('Error in showStoryModal:', error);
            window.App.showNotification('Error displaying story details. Please try again.', 'error');
        }
    }

    closeStoryModal() {
        const modal = document.getElementById('story-modal');
        if (modal) {
            modal.classList.remove('show');
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
        editModal.classList.add('show');
    }

    closeEditModal() {
        const modal = document.getElementById('edit-modal');
        if (modal) {
            modal.classList.remove('show');
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

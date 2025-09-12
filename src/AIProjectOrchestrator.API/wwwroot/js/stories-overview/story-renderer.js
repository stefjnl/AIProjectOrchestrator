/**
 * StoryRenderer - Handles story rendering, card creation, and progress display
 * Extends BaseStoriesManager for shared functionality
 */
class StoryRenderer extends BaseStoriesManager {
    constructor() {
        super();
        console.log('StoryRenderer initialized');
    }

    /**
     * Render all stories in the grid
     */
    renderStories() {
        const storiesGrid = document.getElementById('stories-grid');
        if (!storiesGrid) {
            console.error('Stories grid element not found');
            return;
        }

        if (!this.stories || this.stories.length === 0) {
            this.renderEmptyState(storiesGrid);
            return;
        }

        storiesGrid.innerHTML = `
            <div class="stories-grid">
                ${this.stories.map((story, index) => this.createStoryCard(story, index)).join('')}
            </div>
        `;

        // Add event listeners to story cards
        this.attachStoryCardListeners();
        // Progress update will be called by the manager after rendering
    }

    /**
     * Render empty state when no stories are available
     * @param {HTMLElement} container - Container element
     */
    renderEmptyState(container) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">📖</div>
                <h3>No Stories Found</h3>
                <p>No user stories are available for this generation.</p>
                <button class="btn btn-primary" onclick="window.storiesOverviewManager.refreshStories()">
                    🔄 Try Again
                </button>
            </div>
        `;
    }

    /**
     * Create a story card HTML element
     * @param {Object} story - Story object
     * @param {number} index - Story index
     * @returns {string} HTML string for the story card
     */
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
            <div class="card story-card" data-story-id="${story.id}" data-story-index="${index}">
                <div class="story-header">
                    <h4>${this.manager.utils.escapeHtml(story.title || 'Untitled Story')}</h4>
                    <span class="story-status ${statusClass}">${statusName}</span>
                </div>
                <p class="story-description">${this.manager.utils.escapeHtml(story.description || 'No description available')}</p>
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
                        <button class="btn btn-sm btn-primary" onclick="window.storiesOverviewManager.generatePromptForStory('${story.id}', ${index})">
                            🤖 Generate Prompt
                        </button>
                    ` : ''}
                    ${story.hasPrompt ? `
                        <button class="btn btn-sm btn-info" onclick="window.storiesOverviewManager.viewPrompt('${story.promptId || ''}')">
                            👁️ View Prompt
                        </button>
                    ` : ''}
                </div>
            </div>
        `;
    }

    /**
     * Attach event listeners to story cards (currently handled via onclick attributes)
     */
    attachStoryCardListeners() {
        // Event listeners are already attached via onclick attributes
        // This method is kept for future extensibility
        console.log('Story card listeners attached');
    }

    /**
     * Update progress display and progress bar
     */
    updateProgress() {
        const progress = this.calculateProgress();

        // Update progress display
        const approvedElement = document.getElementById('approved-count');
        const totalElement = document.getElementById('total-count');
        const progressElement = document.getElementById('approval-progress');
        const promptsElement = document.getElementById('prompts-count');

        if (approvedElement) approvedElement.textContent = progress.approved;
        if (totalElement) totalElement.textContent = progress.total;
        if (progressElement) progressElement.textContent = `${progress.percentage}%`;

        // Calculate prompts count
        const promptsCount = this.stories.filter(s => s.hasPrompt).length;
        if (promptsElement) promptsElement.textContent = promptsCount;

        // Update progress bar
        const progressFill = document.getElementById('progress-fill');
        if (progressFill) {
            progressFill.style.width = `${progress.percentage}%`;
        }

        // Update action buttons
        this.updateActionButtons(progress.approved, progress.total);
    }

    /**
     * Update action button states based on story status
     * @param {number} approved - Number of approved stories
     * @param {number} total - Total number of stories
     */
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
     * Refresh stories (calls parent method)
     */
    refreshStories() {
        // This will be implemented by the main manager
        if (this.loadStories) {
            this.loadStories();
        } else {
            console.warn('loadStories method not available');
        }
    }
}
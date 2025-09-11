/**
 * Story Renderer Service
 * Handles HTML generation for story cards and related UI components
 */
class StoryRenderer {
    constructor(statusUtils) {
        this.statusUtils = statusUtils;
    }

    /**
     * Render complete stories grid
     * @param {Array} stories - Array of story objects
     * @returns {string} HTML string
     */
    renderStories(stories) {
        if (!stories || stories.length === 0) {
            return this.renderEmptyState();
        }

        return `
            <div class="stories-grid">
                ${stories.map((story, index) => this.createStoryCard(story, index)).join('')}
            </div>
        `;
    }

    /**
     * Render empty state when no stories are available
     * @returns {string} HTML string
     */
    renderEmptyState() {
        return `
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
     * Render error state when stories fail to load
     * @param {string} message - Error message
     * @returns {string} HTML string
     */
    renderErrorState(message) {
        return `
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

    /**
     * Create individual story card HTML
     * @param {Object} story - Story object
     * @param {number} index - Story index
     * @returns {string} HTML string
     */
    createStoryCard(story, index) {
        // Use status utilities for consistent handling
        const storyStatus = this.statusUtils.normalizeStoryStatus(story.status);
        const statusClass = this.statusUtils.getStatusClass(storyStatus);
        const statusName = this.statusUtils.getStatusName(storyStatus);
        const canApprove = this.statusUtils.canApproveStory(storyStatus);
        const canReject = this.statusUtils.canRejectStory(storyStatus);
        const canGeneratePrompt = this.statusUtils.canGeneratePrompt(storyStatus, story.hasPrompt);

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
     * Render story summary statistics
     * @param {Object} stats - Statistics object
     * @returns {string} HTML string
     */
    renderStorySummary(stats) {
        const { total, approved, rejected, approvalPercentage } = stats;

        return `
            <div class="stories-summary">
                <div class="progress-indicator">
                    <div class="progress-item">
                        <span class="progress-number">${total}</span>
                        <span class="progress-label">Total Stories</span>
                    </div>
                    <div class="progress-item">
                        <span class="progress-number">${approved}</span>
                        <span class="progress-label">Approved</span>
                    </div>
                    <div class="progress-item">
                        <span class="progress-number">${total - approved}</span>
                        <span class="progress-label">Pending</span>
                    </div>
                    <div class="progress-item">
                        <span class="progress-number">${approvalPercentage}%</span>
                        <span class="progress-label">Progress</span>
                    </div>
                </div>
                <div style="margin-top: 15px; font-size: 0.9em; color: #6c757d;">
                    Progress: ${approved} of ${total} stories approved (${approvalPercentage}%)
                </div>
            </div>
        `;
    }

    /**
     * Render action buttons based on story states
     * @param {Object} buttonStates - Button states object
     * @returns {string} HTML string
     */
    renderActionButtons(buttonStates) {
        return `
            <div class="stories-actions">
                <button id="approve-all-btn" class="btn btn-success" 
                        ${buttonStates.approveAll.disabled ? 'disabled' : ''}>
                    ${buttonStates.approveAll.text}
                </button>
                <button id="generate-prompts-btn" class="btn btn-primary" 
                        ${buttonStates.generatePrompts.disabled ? 'disabled' : ''}>
                    ${buttonStates.generatePrompts.text}
                </button>
                <button id="continue-workflow-btn" class="btn btn-secondary" 
                        style="display: ${buttonStates.continueWorkflow.visible ? 'inline-block' : 'none'}">
                    Continue to Workflow
                </button>
                <button id="refresh-stories-btn" class="btn btn-primary">
                    🔄 Refresh Stories
                </button>
                <button id="export-stories-btn" class="btn btn-info">
                    📥 Export Stories
                </button>
            </div>
        `;
    }

    /**
     * Render loading state
     * @returns {string} HTML string
     */
    renderLoadingState() {
        return `
            <div class="loading-state">
                <div class="loading-spinner"></div>
                <h3>Loading Stories...</h3>
                <p>Please wait while we fetch your stories.</p>
            </div>
        `;
    }

    /**
     * Render individual story details for modal
     * @param {Object} story - Story object
     * @returns {string} HTML string
     */
    renderStoryDetails(story) {
        const storyStatus = this.statusUtils.normalizeStoryStatus(story.status);
        const hasPrompt = Boolean(story.hasPrompt);

        return `
            <div class="story-details">
                <div class="story-header-details">
                    <h3>${story.title || 'Untitled Story'}</h3>
                    <span class="status-badge ${this.statusUtils.getStatusClass(storyStatus)}">${this.statusUtils.getStatusName(storyStatus)}</span>
                </div>
                
                <div class="story-meta-details">
                    <div class="meta-item">
                        <strong>Priority:</strong> ${story.priority || 'Medium'}
                    </div>
                    <div class="meta-item">
                        <strong>Story Points:</strong> ${story.storyPoints || 'N/A'}
                    </div>
                    <div class="meta-item">
                        <strong>Has Prompt:</strong> ${hasPrompt ? 'Yes' : 'No'}
                    </div>
                </div>
                
                <div class="story-description-details">
                    <h4>Description</h4>
                    <p>${story.description || 'No description available'}</p>
                </div>
                
                ${this.renderAcceptanceCriteria(story.acceptanceCriteria)}
            </div>
        `;
    }

    /**
     * Render acceptance criteria
     * @param {Array} criteria - Array of acceptance criteria
     * @returns {string} HTML string
     */
    renderAcceptanceCriteria(criteria) {
        if (!criteria || !Array.isArray(criteria) || criteria.length === 0) {
            return '<div class="acceptance-criteria-details"><h4>Acceptance Criteria</h4><p class="text-gray-500">No acceptance criteria specified.</p></div>';
        }

        return `
            <div class="acceptance-criteria-details">
                <h4>Acceptance Criteria</h4>
                <ul class="acceptance-criteria-list">
                    ${criteria.map(criterion => `<li class="acceptance-criteria-item">${criterion}</li>`).join('')}
                </ul>
            </div>
        `;
    }

    /**
     * Render action buttons for modal
     * @param {Object} story - Story object
     * @param {number} index - Story index
     * @returns {string} HTML string
     */
    renderModalActionButtons(story, index) {
        const storyStatus = this.statusUtils.normalizeStoryStatus(story.status);
        const hasPrompt = Boolean(story.hasPrompt);

        return `
            <div class="modal-actions">
                <button id="modal-approve-btn" class="btn btn-success" 
                        ${!this.statusUtils.canApproveStory(storyStatus) ? 'disabled' : ''}
                        onclick="window.storiesOverviewManager.approveCurrentStory()">
                    ✅ Approve
                </button>
                <button id="modal-reject-btn" class="btn btn-danger" 
                        ${!this.statusUtils.canRejectStory(storyStatus) ? 'disabled' : ''}
                        onclick="window.storiesOverviewManager.rejectCurrentStory()">
                    ❌ Reject
                </button>
                <button id="modal-edit-btn" class="btn btn-primary" 
                        onclick="window.storiesOverviewManager.editCurrentStory()">
                    ✏️ Edit
                </button>
                <button id="modal-generate-prompt-btn" class="btn btn-primary" 
                        ${!this.statusUtils.canGeneratePrompt(storyStatus, hasPrompt) ? 'disabled' : ''}
                        onclick="window.storiesOverviewManager.generatePromptForCurrentStory()">
                    🤖 Generate Prompt
                </button>
                <button class="btn btn-secondary" onclick="window.storiesOverviewManager.closeStoryModal()">
                    Close
                </button>
            </div>
        `;
    }

    /**
     * Render edit form for modal
     * @param {Object} story - Story object
     * @returns {string} HTML string
     */
    renderEditForm(story) {
        const criteriaText = story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)
            ? story.acceptanceCriteria.join('\n')
            : '';

        return `
            <div class="edit-form">
                <div class="form-group">
                    <label for="edit-title">Title *</label>
                    <input type="text" id="edit-title" class="form-control" value="${story.title || ''}" required>
                </div>
                
                <div class="form-group">
                    <label for="edit-description">Description *</label>
                    <textarea id="edit-description" class="form-control" rows="4" required>${story.description || ''}</textarea>
                </div>
                
                <div class="form-row">
                    <div class="form-group col-md-6">
                        <label for="edit-priority">Priority</label>
                        <select id="edit-priority" class="form-control">
                            <option value="Low" ${story.priority === 'Low' ? 'selected' : ''}>Low</option>
                            <option value="Medium" ${story.priority === 'Medium' ? 'selected' : ''}>Medium</option>
                            <option value="High" ${story.priority === 'High' ? 'selected' : ''}>High</option>
                            <option value="Critical" ${story.priority === 'Critical' ? 'selected' : ''}>Critical</option>
                        </select>
                    </div>
                    
                    <div class="form-group col-md-6">
                        <label for="edit-points">Story Points</label>
                        <input type="number" id="edit-points" class="form-control" value="${story.storyPoints || ''}" min="1" max="100">
                    </div>
                </div>
                
                <div class="form-group">
                    <label for="edit-criteria">Acceptance Criteria</label>
                    <textarea id="edit-criteria" class="form-control" rows="6" placeholder="Enter acceptance criteria, one per line...">${criteriaText}</textarea>
                    <small class="form-text text-muted">Enter each criterion on a new line</small>
                </div>
                
                <div class="form-actions">
                    <button type="button" class="btn btn-primary" onclick="window.storiesOverviewManager.saveEditedStory()">
                        💾 Save Changes
                    </button>
                    <button type="button" class="btn btn-secondary" onclick="window.storiesOverviewManager.closeEditModal()">
                        Cancel
                    </button>
                </div>
            </div>
        `;
    }

    /**
     * Render progress bar
     * @param {number} percentage - Progress percentage
     * @returns {string} HTML string
     */
    renderProgressBar(percentage) {
        return `
            <div class="progress-bar-container">
                <div class="progress-bar">
                    <div class="progress-fill" style="width: ${percentage}%"></div>
                </div>
                <span class="progress-text">${percentage}%</span>
            </div>
        `;
    }

    /**
     * Render story statistics
     * @param {Object} stats - Statistics object
     * @returns {string} HTML string
     */
    renderStoryStats(stats) {
        return `
            <div class="story-stats">
                <div class="stat-item">
                    <span class="stat-number">${stats.totalStories}</span>
                    <span class="stat-label">Total</span>
                </div>
                <div class="stat-item">
                    <span class="stat-number">${stats.approvedStories}</span>
                    <span class="stat-label">Approved</span>
                </div>
                <div class="stat-item">
                    <span class="stat-number">${stats.rejectedStories}</span>
                    <span class="stat-label">Rejected</span>
                </div>
                <div class="stat-item">
                    <span class="stat-number">${stats.storiesWithPrompts}</span>
                    <span class="stat-label">With Prompts</span>
                </div>
            </div>
        `;
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { StoryRenderer };
}

// Make available globally for backward compatibility
window.StoryRenderer = StoryRenderer;
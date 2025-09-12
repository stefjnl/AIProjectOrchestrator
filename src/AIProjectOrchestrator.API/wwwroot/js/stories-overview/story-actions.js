/**
 * StoryActions - Handles story approval, rejection, and bulk operations
 * Extends BaseStoriesManager for shared functionality
 */
class StoryActions extends BaseStoriesManager {
    constructor() {
        super();
        console.log('StoryActions initialized');
    }

    /**
     * Approve a single story
     * @param {string} storyId - Story ID to approve
     */
    async approveStory(storyId) {
        if (!confirm('Are you sure you want to approve this story?')) {
            return;
        }

        if (this.isLoading) {
            this.showNotification('Please wait for current operations to complete.', 'warning');
            return;
        }

        try {
            console.log(`Approving story ${storyId}`);
            this.setLoadingState(true);

            await APIClient.approveStory(storyId);

            // Update local story status
            const updated = this.updateLocalStoryStatus(storyId, window.StatusUtils.StoryStatus.APPROVED);
            if (!updated) {
                console.warn(`Story ${storyId} not found for local update`);
            }

            // Refresh the display
            if (this.manager && this.manager.renderer) {
                this.manager.renderer.renderStories();
            }
            this.showNotification('Story approved successfully!', 'success');

            // Close the story modal if it's open and this is the current story
            if (this.currentStory && this.currentStory.id === storyId) {
                if (this.manager && this.manager.modals) {
                    this.manager.modals.closeStoryModal();
                }
            }

        } catch (error) {
            console.error('Failed to approve story:', error);
            this.showNotification('Failed to approve story. Please try again.', 'error');
        } finally {
            this.setLoadingState(false);
        }
    }

    /**
     * Reject a story with feedback
     * @param {string} storyId - Story ID to reject
     */
    async rejectStory(storyId) {
        const feedback = prompt('Please provide feedback for rejecting this story:');
        if (!feedback) {
            this.showNotification('Rejection cancelled - feedback required.', 'info');
            return;
        }

        if (this.isLoading) {
            this.showNotification('Please wait for current operations to complete.', 'warning');
            return;
        }

        try {
            console.log(`Rejecting story ${storyId} with feedback: ${feedback}`);
            this.setLoadingState(true);

            await APIClient.rejectStory(storyId, { feedback });

            // Update local story status
            const updated = this.updateLocalStoryStatus(storyId, window.StatusUtils.StoryStatus.REJECTED, {
                rejectionFeedback: feedback
            });

            if (this.manager && this.manager.renderer) {
                this.manager.renderer.renderStories();
            }
            this.showNotification('Story rejected successfully!', 'success');

        } catch (error) {
            console.error('Failed to reject story:', error);
            this.showNotification('Failed to reject story. Please try again.', 'error');
        } finally {
            this.setLoadingState(false);
        }
    }

    /**
     * Approve all pending stories in bulk
     */
    async approveAllStories() {
        const pendingStories = this.stories.filter(s =>
            window.StatusUtils.canApproveStory(s.status));

        if (pendingStories.length === 0) {
            this.showNotification('No pending stories to approve.', 'info');
            return;
        }

        if (!confirm(`Are you sure you want to approve all ${pendingStories.length} pending stories?`)) {
            return;
        }

        if (this.isLoading) {
            this.showNotification('Please wait for current operations to complete.', 'warning');
            return;
        }

        const loadingOverlay = this.showLoadingOverlay('Approving all stories...');

        try {
            console.log(`Approving all ${pendingStories.length} pending stories`);

            // Approve stories one by one (since there's no bulk approve endpoint)
            for (const story of pendingStories) {
                await APIClient.approveStory(story.id);
                story.status = window.StatusUtils.StoryStatus.APPROVED;
            }

            if (this.manager && this.manager.renderer) {
                this.manager.renderer.renderStories();
            }
            this.showNotification(`All ${pendingStories.length} stories approved successfully!`, 'success');

            // Close any open modals after bulk approval
            if (this.manager && this.manager.modals) {
                this.manager.modals.closeStoryModal();
                this.manager.modals.closeEditModal();
            }

        } catch (error) {
            console.error('Failed to approve all stories:', error);
            this.showNotification('Failed to approve some stories. Please try again.', 'error');
        } finally {
            this.hideLoadingOverlay(loadingOverlay);
        }
    }

    /**
     * Current story actions from modal (convenience methods)
     */
    approveCurrentStory() {
        if (this.currentStory) {
            this.approveStory(this.currentStory.id);
            // The approveStory function will handle closing the modal if it's the current story
        } else {
            this.showNotification('No story selected.', 'warning');
        }
    }

    rejectCurrentStory() {
        if (this.currentStory) {
            this.rejectStory(this.currentStory.id);
        } else {
            this.showNotification('No story selected.', 'warning');
        }
    }

    /**
     * Check if a story can be approved
     * @param {Object} story - Story object
     * @returns {boolean} True if story can be approved
     */
    canApproveStory(story) {
        if (!story) return false;
        return window.StatusUtils.canApproveStory(story.status);
    }

    /**
     * Check if a story can be rejected
     * @param {Object} story - Story object
     * @returns {boolean} True if story can be rejected
     */
    canRejectStory(story) {
        if (!story) return false;
        return window.StatusUtils.canRejectStory(story.status);
    }

    /**
     * Get count of pending stories
     * @returns {number} Number of pending stories
     */
    getPendingStoriesCount() {
        return this.stories.filter(s => window.StatusUtils.canApproveStory(s.status)).length;
    }

    /**
     * Get count of approved stories
     * @returns {number} Number of approved stories
     */
    getApprovedStoriesCount() {
        return this.stories.filter(s =>
            window.StatusUtils.normalizeStoryStatus(s.status) === window.StatusUtils.StoryStatus.APPROVED
        ).length;
    }

    /**
     * Get count of rejected stories
     * @returns {number} Number of rejected stories
     */
    getRejectedStoriesCount() {
        return this.stories.filter(s =>
            window.StatusUtils.normalizeStoryStatus(s.status) === window.StatusUtils.StoryStatus.REJECTED
        ).length;
    }

    /**
     * Validate story action
     * @param {string} action - Action type ('approve', 'reject')
     * @param {Object} story - Story object
     * @returns {Object} Validation result { valid: boolean, message: string }
     */
    validateStoryAction(action, story) {
        if (!story) {
            return { valid: false, message: 'Story not found' };
        }

        if (this.isLoading) {
            return { valid: false, message: 'Please wait for current operations to complete' };
        }

        switch (action) {
            case 'approve':
                if (!this.canApproveStory(story)) {
                    return { valid: false, message: 'Story cannot be approved in its current status' };
                }
                break;
            case 'reject':
                if (!this.canRejectStory(story)) {
                    return { valid: false, message: 'Story cannot be rejected in its current status' };
                }
                break;
            default:
                return { valid: false, message: 'Invalid action type' };
        }

        return { valid: true, message: '' };
    }

    /**
     * Get story action summary
     * @returns {Object} Action summary data
     */
    getActionSummary() {
        const total = this.getStoriesCount();
        const pending = this.getPendingStoriesCount();
        const approved = this.getApprovedStoriesCount();
        const rejected = this.getRejectedStoriesCount();

        return {
            total,
            pending,
            approved,
            rejected,
            canApproveAll: pending > 0,
            approvalPercentage: total > 0 ? Math.round((approved / total) * 100) : 0
        };
    }
}
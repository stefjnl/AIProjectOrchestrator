/**
 * Story Modal Service
 * Handles story detail and edit modal functionality
 */
class StoryModalService {
    constructor() {
        this.currentStory = null;
        this.modalElement = document.getElementById('story-modal');
        this.editModalElement = document.getElementById('edit-modal');

        // Cache DOM elements
        this.cacheModalElements();
    }

    /**
     * Cache modal DOM elements for better performance
     */
    cacheModalElements() {
        if (this.modalElement) {
            this.elements = {
                title: this.modalElement.querySelector('#modal-story-title'),
                description: this.modalElement.querySelector('#modal-story-description'),
                priority: this.modalElement.querySelector('#modal-story-priority'),
                points: this.modalElement.querySelector('#modal-story-points'),
                status: this.modalElement.querySelector('#modal-story-status'),
                promptStatus: this.modalElement.querySelector('#modal-story-prompt-status'),
                criteria: this.modalElement.querySelector('#modal-story-criteria'),
                approveBtn: this.modalElement.querySelector('#modal-approve-btn'),
                rejectBtn: this.modalElement.querySelector('#modal-reject-btn'),
                generatePromptBtn: this.modalElement.querySelector('#modal-generate-prompt-btn')
            };
        }

        if (this.editModalElement) {
            this.editElements = {
                title: this.editModalElement.querySelector('#edit-title'),
                description: this.editModalElement.querySelector('#edit-description'),
                priority: this.editModalElement.querySelector('#edit-priority'),
                points: this.editModalElement.querySelector('#edit-points'),
                criteria: this.editModalElement.querySelector('#edit-criteria')
            };
        }
    }

    /**
     * Show story detail modal
     * @param {Object} story - Story object
     * @param {number} index - Story index
     */
    showStoryModal(story, index) {
        if (!this.modalElement) {
            console.error('Story modal element not found!');
            window.App.showNotification('Story modal not found. Please refresh the page.', 'error');
            return;
        }

        this.currentStory = { ...story, index };

        try {
            this.populateModalContent(story);
            this.updateButtonStates(story);

            this.modalElement.classList.add('show');
            this.modalElement.offsetHeight; // Force reflow

            console.log('Story modal shown successfully');

        } catch (error) {
            console.error('Error in showStoryModal:', error);
            window.App.showNotification('Error displaying story details. Please try again.', 'error');
        }
    }

    /**
     * Populate modal content with story data
     * @param {Object} story - Story object
     */
    populateModalContent(story) {
        const storyStatus = window.StatusUtils.normalizeStoryStatus(story.status);

        // Update basic story information
        if (this.elements.title) this.elements.title.textContent = story.title || 'Untitled Story';
        if (this.elements.description) this.elements.description.textContent = story.description || 'No description available';
        if (this.elements.priority) this.elements.priority.textContent = story.priority || 'Medium';
        if (this.elements.points) this.elements.points.textContent = story.storyPoints || 'N/A';
        if (this.elements.status) this.elements.status.textContent = storyStatus;
        if (this.elements.promptStatus) this.elements.promptStatus.textContent = story.hasPrompt ? 'Yes' : 'No';

        // Format acceptance criteria
        if (this.elements.criteria) {
            if (story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)) {
                this.elements.criteria.innerHTML = `
                    <ul class="acceptance-criteria-list">
                        ${story.acceptanceCriteria.map(criterion => `<li class="acceptance-criteria-item">${criterion}</li>`).join('')}
                    </ul>
                `;
            } else {
                this.elements.criteria.innerHTML = '<p class="text-gray-500">No acceptance criteria specified.</p>';
            }
        }
    }

    /**
     * Update button states based on story status
     * @param {Object} story - Story object
     */
    updateButtonStates(story) {
        const storyStatus = window.StatusUtils.normalizeStoryStatus(story.status);
        const hasPrompt = Boolean(story.hasPrompt);

        if (this.elements.approveBtn) {
            this.elements.approveBtn.disabled = !window.StatusUtils.canApproveStory(storyStatus);
        }

        if (this.elements.rejectBtn) {
            this.elements.rejectBtn.disabled = !window.StatusUtils.canRejectStory(storyStatus);
        }

        if (this.elements.generatePromptBtn) {
            this.elements.generatePromptBtn.disabled = !window.StatusUtils.canGeneratePrompt(storyStatus, hasPrompt);

            // Add click event listener for debugging
            this.elements.generatePromptBtn.onclick = () => {
                console.log('Modal Generate Prompt button clicked!');
                console.log('Current story:', this.currentStory);
                if (this.currentStory) {
                    // This will be handled by the parent class
                    window.storiesOverviewManager.generatePromptForStory(this.currentStory.id, this.currentStory.index);
                }
            };
        }
    }

    /**
     * Close story detail modal
     */
    closeStoryModal() {
        if (this.modalElement) {
            this.modalElement.classList.remove('show');
        }
        this.currentStory = null;
    }

    /**
     * Show edit story modal
     */
    showEditModal(story) {
        if (!this.editModalElement || !story) return;

        this.currentStory = story;

        // Populate edit form
        if (this.editElements.title) this.editElements.title.value = story.title || '';
        if (this.editElements.description) this.editElements.description.value = story.description || '';
        if (this.editElements.priority) this.editElements.priority.value = story.priority || 'Medium';
        if (this.editElements.points) this.editElements.points.value = story.storyPoints || '';

        // Format acceptance criteria
        if (this.editElements.criteria) {
            const criteriaText = story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)
                ? story.acceptanceCriteria.join('\n')
                : '';
            this.editElements.criteria.value = criteriaText;
        }

        // Close story modal and open edit modal
        this.closeStoryModal();
        this.editModalElement.classList.add('show');
    }

    /**
     * Close edit story modal
     */
    closeEditModal() {
        if (this.editModalElement) {
            this.editModalElement.classList.remove('show');
        }
    }

    /**
     * Get current story data
     * @returns {Object|null} Current story or null
     */
    getCurrentStory() {
        return this.currentStory;
    }

    /**
     * Get edit form data
     * @returns {Object} Form data
     */
    getEditFormData() {
        if (!this.editElements) return {};

        return {
            title: this.editElements.title ? this.editElements.title.value.trim() : '',
            description: this.editElements.description ? this.editElements.description.value.trim() : '',
            priority: this.editElements.priority ? this.editElements.priority.value : 'Medium',
            storyPoints: this.editElements.points ? parseInt(this.editElements.points.value) || null : null,
            acceptanceCriteria: this.editElements.criteria ? this.editElements.criteria.value
                .split('\n')
                .filter(line => line.trim())
                .map(line => line.trim()) : []
        };
    }

    /**
     * Validate edit form data
     * @param {Object} formData - Form data to validate
     * @returns {Object} Validation result
     */
    validateEditFormData(formData) {
        const errors = [];

        if (!formData.title) {
            errors.push('Title is required');
        }

        if (!formData.description) {
            errors.push('Description is required');
        }

        if (formData.storyPoints !== null && (formData.storyPoints < 1 || formData.storyPoints > 100)) {
            errors.push('Story points must be between 1 and 100');
        }

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    /**
     * Show validation errors
     * @param {Array} errors - Array of error messages
     */
    showValidationErrors(errors) {
        if (errors.length === 0) return;

        const errorMessage = errors.join('\n');
        window.App.showNotification(errorMessage, 'error');
    }

    /**
     * Check if modals are available in DOM
     * @returns {boolean} Whether modals are available
     */
    areModalsAvailable() {
        return this.modalElement !== null && this.editModalElement !== null;
    }

    /**
     * Refresh modal element cache (useful for dynamic DOM changes)
     */
    refreshModalCache() {
        this.modalElement = document.getElementById('story-modal');
        this.editModalElement = document.getElementById('edit-modal');
        this.cacheModalElements();
    }

    /**
     * Handle ESC key press to close modals
     * @param {KeyboardEvent} event - Keyboard event
     */
    handleEscapeKey(event) {
        if (event.key === 'Escape') {
            if (this.editModalElement && this.editModalElement.classList.contains('show')) {
                this.closeEditModal();
            } else if (this.modalElement && this.modalElement.classList.contains('show')) {
                this.closeStoryModal();
            }
        }
    }

    /**
     * Handle click outside modal to close
     * @param {MouseEvent} event - Mouse event
     */
    handleOutsideClick(event) {
        if (event.target === this.modalElement) {
            this.closeStoryModal();
        }
        if (event.target === this.editModalElement) {
            this.closeEditModal();
        }
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { StoryModalService };
}

// Make available globally for backward compatibility
window.StoryModalService = StoryModalService;
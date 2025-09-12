/**
 * ModalManager - Handles all modal operations (story detail, edit, prompt viewer)
 * Extends BaseStoriesManager for shared functionality
 */
class ModalManager extends BaseStoriesManager {
    constructor() {
        super();
        this.currentPrompt = null;
        console.log('ModalManager initialized');
    }

    /**
     * View story details in modal
     * @param {number} index - Story index
     */
    viewStory(index) {
        console.log(`viewStory called with index: ${index}`);
        console.log(`Available stories:`, this.stories);
        console.log(`StoriesOverviewManager instance:`, this);
        console.log(`this.stories length:`, this.stories ? this.stories.length : 'undefined');

        // Safety check - ensure stories are loaded
        if (!this.stories || this.stories.length === 0) {
            console.error('No stories loaded yet');
            this.showNotification('Stories are still loading. Please wait a moment and try again.', 'warning');
            return;
        }

        // Safety check - ensure index is valid
        if (index < 0 || index >= this.stories.length) {
            console.error(`Invalid index ${index}. Stories array has ${this.stories.length} items.`);
            this.showNotification('Invalid story index. Please try again.', 'error');
            return;
        }

        const story = this.stories[index];
        if (!story) {
            console.error(`No story found at index ${index}`);
            this.showNotification('Story not found. Please try again.', 'error');
            return;
        }

        console.log(`Found story:`, story);
        this.currentStory = { ...story, index };
        this.showStoryModal(story);
    }

    /**
     * Show story detail modal
     * @param {Object} story - Story object
     */
    showStoryModal(story) {
        console.log(`showStoryModal called with story:`, story);
        const modal = document.getElementById('story-modal');
        console.log(`Modal element found:`, modal);

        if (!modal) {
            console.error('Story modal element not found!');
            this.showNotification('Story modal not found. Please refresh the page.', 'error');
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
            if (statusElement) statusElement.textContent = statusText;
            if (promptStatusElement) promptStatusElement.textContent = story.hasPrompt ? 'Yes' : 'No';

            // Format acceptance criteria
            const criteriaElement = document.getElementById('modal-story-criteria');
            if (criteriaElement) {
                if (story.acceptanceCriteria && Array.isArray(story.acceptanceCriteria)) {
                    criteriaElement.innerHTML = `
                        <ul class="acceptance-criteria-list">
                            ${story.acceptanceCriteria.map(criterion => `<li class="acceptance-criteria-item">${this.manager.utils.escapeHtml(criterion)}</li>`).join('')}
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
                approveDisabled: !window.StatusUtils.canApproveStory(storyStatus),
                rejectDisabled: !window.StatusUtils.canRejectStory(storyStatus),
                generateDisabled: !window.StatusUtils.canGeneratePrompt(storyStatus, hasPrompt)
            });

            if (approveBtn) approveBtn.disabled = !window.StatusUtils.canApproveStory(storyStatus);
            if (rejectBtn) rejectBtn.disabled = !window.StatusUtils.canRejectStory(storyStatus);
            if (generatePromptBtn) {
                generatePromptBtn.disabled = !window.StatusUtils.canGeneratePrompt(storyStatus, hasPrompt);

                // Add click event listener for debugging
                generatePromptBtn.onclick = () => {
                    console.log('Modal Generate Prompt button clicked!');
                    console.log('Current story:', this.currentStory);
                    if (this.currentStory && this.generatePromptForStory) {
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
            this.showNotification('Error displaying story details. Please try again.', 'error');
        }
    }

    /**
     * Close story modal
     */
    closeStoryModal() {
        const modal = document.getElementById('story-modal');
        if (modal) {
            modal.classList.remove('show');
        }
        this.currentStory = null;
    }

    /**
     * Edit current story
     */
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

    /**
     * Close edit modal
     */
    closeEditModal() {
        const modal = document.getElementById('edit-modal');
        if (modal) {
            modal.classList.remove('show');
        }
    }

    /**
     * Show prompt viewer modal
     * @param {Object} promptData - Prompt data object
     */
    showPromptModal(promptData) {
        const modal = document.getElementById('prompt-viewer-modal');
        if (!modal) {
            console.error('Prompt viewer modal not found');
            return;
        }

        // Populate modal content
        document.getElementById('modal-prompt-title').textContent =
            `Generated Prompt - ${promptData.storyTitle || 'Untitled Story'}`;

        document.getElementById('modal-prompt-story-title').textContent =
            promptData.storyTitle || 'Untitled Story';

        document.getElementById('modal-prompt-date').textContent =
            new Date(promptData.createdAt).toLocaleString();

        document.getElementById('modal-prompt-quality').textContent =
            this.manager.utils.calculateQualityScore(promptData.generatedPrompt);

        document.getElementById('modal-prompt-content').textContent =
            promptData.generatedPrompt;

        // Show modal
        modal.classList.add('show');

        // Store current prompt for other operations
        this.currentPrompt = promptData;
    }

    /**
     * Close prompt modal
     */
    closePromptModal() {
        const modal = document.getElementById('prompt-viewer-modal');
        if (modal) {
            modal.classList.remove('show');
        }
        this.currentPrompt = null;
    }

    /**
     * Edit current prompt
     */
    editPrompt() {
        if (!this.currentPrompt) {
            this.showNotification('No prompt to edit.', 'warning');
            return;
        }

        // Enable inline editing
        const contentElement = document.getElementById('modal-prompt-content');
        contentElement.contentEditable = true;
        contentElement.classList.add('editing');

        // Change edit button to save button
        const editBtn = document.querySelector('[onclick="editPrompt()"]');
        if (editBtn) {
            editBtn.textContent = '💾 Save';
            editBtn.onclick = () => this.savePromptEdit();
        }

        this.showNotification('Prompt is now editable. Make your changes and click Save.', 'info');
    }

    /**
     * Save prompt edit
     */
    savePromptEdit() {
        const contentElement = document.getElementById('modal-prompt-content');
        const editedContent = contentElement.textContent;

        if (!this.currentPrompt) {
            this.showNotification('No prompt to save.', 'warning');
            return;
        }

        // Update the current prompt
        this.currentPrompt.generatedPrompt = editedContent;

        // Disable editing
        contentElement.contentEditable = false;
        contentElement.classList.remove('editing');

        // Change save button back to edit button
        const saveBtn = document.querySelector('[onclick="savePromptEdit()"]');
        if (saveBtn) {
            saveBtn.textContent = '✏️ Edit';
            saveBtn.onclick = () => this.editPrompt();
        }

        this.showNotification('Prompt updated successfully!', 'success');
    }

    /**
     * Copy current prompt to clipboard
     */
    copyPrompt() {
        if (!this.currentPrompt || !this.currentPrompt.generatedPrompt) {
            this.showNotification('No prompt to copy.', 'warning');
            return;
        }

        navigator.clipboard.writeText(this.currentPrompt.generatedPrompt)
            .then(() => {
                this.showNotification('Prompt copied to clipboard!', 'success');
            })
            .catch(err => {
                console.error('Failed to copy prompt:', err);
                this.showNotification('Failed to copy prompt. Please try again.', 'error');
            });
    }

    /**
     * Export current prompt
     */
    exportPrompt() {
        if (!this.currentPrompt || !this.currentPrompt.generatedPrompt) {
            this.showNotification('No prompt to export.', 'warning');
            return;
        }

        const data = {
            promptId: this.currentPrompt.promptId,
            storyTitle: this.currentPrompt.storyTitle,
            generatedPrompt: this.currentPrompt.generatedPrompt,
            createdAt: this.currentPrompt.createdAt,
            exportedAt: new Date().toISOString()
        };

        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `prompt-${this.currentPrompt.promptId}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);

        this.showNotification('Prompt exported successfully!', 'success');
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
     * Setup modal event handlers
     */
    setupModalEventHandlers() {
        // Handle modal close on outside click
        document.addEventListener('click', (event) => {
            const storyModal = document.getElementById('story-modal');
            const editModal = document.getElementById('edit-modal');
            const promptModal = document.getElementById('prompt-viewer-modal');

            if (event.target === storyModal) {
                this.closeStoryModal();
            }
            if (event.target === editModal) {
                this.closeEditModal();
            }
            if (event.target === promptModal) {
                this.closePromptModal();
            }
        });

        // Handle ESC key to close modals
        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                this.closeStoryModal();
                this.closeEditModal();
                this.closePromptModal();
            }
        });

        console.log('Modal event handlers setup complete');
    }

    /**
     * Cleanup modal resources
     */
    cleanupModals() {
        this.closeStoryModal();
        this.closeEditModal();
        this.closePromptModal();
        this.currentPrompt = null;
    }
}
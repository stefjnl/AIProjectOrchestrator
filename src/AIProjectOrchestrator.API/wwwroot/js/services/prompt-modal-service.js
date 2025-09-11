/**
 * Prompt Modal Service
 * Handles prompt viewer modal functionality
 */
class PromptModalService {
    constructor() {
        this.currentPrompt = null;
        this.modalElement = document.getElementById('prompt-viewer-modal');
        this.isEditing = false;

        // Cache DOM elements
        this.cacheModalElements();
    }

    /**
     * Cache modal DOM elements for better performance
     */
    cacheModalElements() {
        if (this.modalElement) {
            this.elements = {
                title: this.modalElement.querySelector('#modal-prompt-title'),
                storyTitle: this.modalElement.querySelector('#modal-prompt-story-title'),
                date: this.modalElement.querySelector('#modal-prompt-date'),
                quality: this.modalElement.querySelector('#modal-prompt-quality'),
                content: this.modalElement.querySelector('#modal-prompt-content'),
                copyBtn: this.modalElement.querySelector('#modal-prompt-copy-btn'),
                editBtn: this.modalElement.querySelector('#modal-prompt-edit-btn'),
                exportBtn: this.modalElement.querySelector('#modal-prompt-export-btn'),
                saveBtn: this.modalElement.querySelector('#modal-prompt-save-btn'),
                cancelBtn: this.modalElement.querySelector('#modal-prompt-cancel-btn')
            };
        }
    }

    /**
     * Show prompt viewer modal
     * @param {Object} promptData - Prompt data object
     */
    showPromptModal(promptData) {
        if (!this.modalElement) {
            console.error('Prompt viewer modal not found');
            window.App.showNotification('Prompt viewer modal not found', 'error');
            return;
        }

        this.currentPrompt = promptData;
        this.isEditing = false;

        try {
            this.populateModalContent(promptData);
            this.updateButtonStates();

            this.modalElement.classList.add('show');

            window.App.showNotification('Prompt loaded successfully', 'success');
            console.log('Prompt modal shown successfully');

        } catch (error) {
            console.error('Error showing prompt modal:', error);
            window.App.showNotification('Error displaying prompt', 'error');
        }
    }

    /**
     * Populate modal content with prompt data
     * @param {Object} promptData - Prompt data object
     */
    populateModalContent(promptData) {
        if (this.elements.title) {
            this.elements.title.textContent = `Generated Prompt - ${promptData.storyTitle || 'Untitled Story'}`;
        }

        if (this.elements.storyTitle) {
            this.elements.storyTitle.textContent = promptData.storyTitle || 'Untitled Story';
        }

        if (this.elements.date) {
            this.elements.date.textContent = new Date(promptData.createdAt).toLocaleString();
        }

        if (this.elements.quality) {
            this.elements.quality.textContent = this.calculateQualityScore(promptData.generatedPrompt);
        }

        if (this.elements.content) {
            this.elements.content.textContent = promptData.generatedPrompt;
            this.elements.content.contentEditable = false;
            this.elements.content.classList.remove('editing');
        }
    }

    /**
     * Update button states based on current mode
     */
    updateButtonStates() {
        if (!this.elements) return;

        const isEditing = this.isEditing;

        // Show/hide appropriate buttons
        if (this.elements.copyBtn) {
            this.elements.copyBtn.style.display = isEditing ? 'none' : 'inline-block';
        }

        if (this.elements.editBtn) {
            this.elements.editBtn.style.display = isEditing ? 'none' : 'inline-block';
            this.elements.editBtn.textContent = '✏️ Edit';
            this.elements.editBtn.onclick = () => this.editPrompt();
        }

        if (this.elements.exportBtn) {
            this.elements.exportBtn.style.display = isEditing ? 'none' : 'inline-block';
        }

        if (this.elements.saveBtn) {
            this.elements.saveBtn.style.display = isEditing ? 'inline-block' : 'none';
        }

        if (this.elements.cancelBtn) {
            this.elements.cancelBtn.style.display = isEditing ? 'inline-block' : 'none';
        }
    }

    /**
     * Copy prompt to clipboard
     */
    copyPrompt() {
        if (!this.currentPrompt || !this.currentPrompt.generatedPrompt) {
            window.App.showNotification('No prompt to copy.', 'warning');
            return;
        }

        navigator.clipboard.writeText(this.currentPrompt.generatedPrompt)
            .then(() => {
                window.App.showNotification('Prompt copied to clipboard!', 'success');
            })
            .catch(err => {
                console.error('Failed to copy prompt:', err);
                window.App.showNotification('Failed to copy prompt. Please try again.', 'error');
            });
    }

    /**
     * Enable inline editing of prompt
     */
    editPrompt() {
        if (!this.currentPrompt) {
            window.App.showNotification('No prompt to edit.', 'warning');
            return;
        }

        // Enable inline editing
        if (this.elements.content) {
            this.elements.content.contentEditable = true;
            this.elements.content.classList.add('editing');
        }

        this.isEditing = true;
        this.updateButtonStates();

        // Update edit button
        if (this.elements.editBtn) {
            this.elements.editBtn.textContent = '💾 Save';
            this.elements.editBtn.onclick = () => this.savePromptEdit();
        }

        window.App.showNotification('Prompt is now editable. Make your changes and click Save.', 'info');
    }

    /**
     * Save edited prompt
     */
    savePromptEdit() {
        if (!this.currentPrompt || !this.elements.content) {
            window.App.showNotification('No prompt to save.', 'warning');
            return;
        }

        const editedContent = this.elements.content.textContent;

        // Update the current prompt
        this.currentPrompt.generatedPrompt = editedContent;

        // Disable editing
        this.elements.content.contentEditable = false;
        this.elements.content.classList.remove('editing');

        this.isEditing = false;
        this.updateButtonStates();

        // Update save button back to edit
        if (this.elements.editBtn) {
            this.elements.editBtn.textContent = '✏️ Edit';
            this.elements.editBtn.onclick = () => this.editPrompt();
        }

        window.App.showNotification('Prompt updated successfully!', 'success');
    }

    /**
     * Export current prompt
     */
    exportPrompt() {
        if (!this.currentPrompt || !this.currentPrompt.generatedPrompt) {
            window.App.showNotification('No prompt to export.', 'warning');
            return;
        }

        try {
            const data = {
                promptId: this.currentPrompt.promptId,
                storyTitle: this.currentPrompt.storyTitle,
                generatedPrompt: this.currentPrompt.generatedPrompt,
                createdAt: this.currentPrompt.createdAt,
                exportedAt: new Date().toISOString()
            };

            // Use the export service if available, otherwise do it manually
            if (window.exportService) {
                window.exportService.exportPrompt(this.currentPrompt);
            } else {
                this.downloadJSON(data, `prompt-${this.currentPrompt.promptId}.json`);
            }

            window.App.showNotification('Prompt exported successfully!', 'success');

        } catch (error) {
            console.error('Failed to export prompt:', error);
            window.App.showNotification('Failed to export prompt. Please try again.', 'error');
        }
    }

    /**
     * Calculate quality score for a prompt based on content characteristics
     * @param {string} prompt - The prompt content
     * @returns {string} Quality score as percentage
     */
    calculateQualityScore(prompt) {
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
     * Close prompt viewer modal
     */
    closePromptModal() {
        if (this.modalElement) {
            this.modalElement.classList.remove('show');
        }
        this.currentPrompt = null;
        this.isEditing = false;
    }

    /**
     * Download data as JSON file
     * @param {Object} data - Data to export
     * @param {string} fileName - Output filename
     */
    downloadJSON(data, fileName) {
        try {
            const jsonContent = JSON.stringify(data, null, 2);
            const blob = new Blob([jsonContent], { type: 'application/json' });
            const url = URL.createObjectURL(blob);

            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            link.style.display = 'none';

            document.body.appendChild(link);
            link.click();

            // Cleanup
            setTimeout(() => {
                document.body.removeChild(link);
                URL.revokeObjectURL(url);
            }, 100);

        } catch (error) {
            console.error('Failed to download JSON:', error);
            throw new Error(`Failed to export file: ${error.message}`);
        }
    }

    /**
     * Get current prompt data
     * @returns {Object|null} Current prompt or null
     */
    getCurrentPrompt() {
        return this.currentPrompt;
    }

    /**
     * Check if modal is available in DOM
     * @returns {boolean} Whether modal is available
     */
    isModalAvailable() {
        return this.modalElement !== null;
    }

    /**
     * Refresh modal element cache (useful for dynamic DOM changes)
     */
    refreshModalCache() {
        this.modalElement = document.getElementById('prompt-viewer-modal');
        this.cacheModalElements();
    }

    /**
     * Handle ESC key press to close modal
     * @param {KeyboardEvent} event - Keyboard event
     */
    handleEscapeKey(event) {
        if (event.key === 'Escape' && this.modalElement && this.modalElement.classList.contains('show')) {
            this.closePromptModal();
        }
    }

    /**
     * Handle click outside modal to close
     * @param {MouseEvent} event - Mouse event
     */
    handleOutsideClick(event) {
        if (event.target === this.modalElement) {
            this.closePromptModal();
        }
    }

    /**
     * Enable keyboard navigation
     */
    enableKeyboardNavigation() {
        document.addEventListener('keydown', (event) => this.handleEscapeKey(event));
    }

    /**
     * Disable keyboard navigation
     */
    disableKeyboardNavigation() {
        document.removeEventListener('keydown', (event) => this.handleEscapeKey(event));
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { PromptModalService };
}

// Make available globally for backward compatibility
window.PromptModalService = PromptModalService;
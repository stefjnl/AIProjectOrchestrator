/**
 * BaseContentGenerator - Base class for all stage content generators
 * Provides common functionality and utilities for stage-specific content generation
 */
class BaseContentGenerator {
    /**
     * Initialize the base content generator with dependencies
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        if (!workflowManager) {
            throw new Error('WorkflowManager is required');
        }
        if (!apiClient) {
            throw new Error('APIClient is required');
        }

        this.workflowManager = workflowManager;
        this.apiClient = apiClient;
    }

    /**
     * Get the current workflow state
     * @returns {object} Current workflow state
     */
    get workflowState() {
        return this.workflowManager.workflowState;
    }

    /**
     * Get the project ID
     * @returns {string} Project ID
     */
    get projectId() {
        return this.workflowManager.projectId;
    }

    /**
     * Check if the current project is new
     * @returns {boolean} True if new project
     */
    get isNewProject() {
        return this.workflowManager.isNewProject;
    }

    /**
     * Common error handling for content generation
     * @param {string} methodName - Name of the method that failed
     * @param {Error} error - The error that occurred
     * @param {string} fallbackMessage - Fallback error message
     * @returns {string} Error HTML content
     */
    handleContentGenerationError(methodName, error, fallbackMessage = 'An error occurred while generating content.') {
        console.error(`Error in ${methodName}:`, error);
        return `
            <div class="stage-container">
                <div class="error-state">
                    <div class="error-icon">⚠️</div>
                    <h3>Content Generation Error</h3>
                    <p>${fallbackMessage}</p>
                    <details>
                        <summary>Error Details</summary>
                        <pre>${error.message}</pre>
                    </details>
                </div>
            </div>
        `;
    }

    /**
     * Create a stage container with common structure
     * @param {string} title - Stage title
     * @param {string} content - Main content
     * @param {string} actions - Action buttons HTML (optional)
     * @returns {string} Complete stage container HTML
     */
    createStageContainer(title, content, actions = '') {
        return `
            <div class="stage-container">
                <h2>${title}</h2>
                ${content}
                ${actions ? `<div class="stage-actions">${actions}</div>` : ''}
            </div>
        `;
    }

    /**
     * Create a status indicator with icon and message
     * @param {string} status - Status type (pending, active, completed, locked, empty)
     * @param {string} icon - Emoji icon
     * @param {string} title - Status title
     * @param {string} message - Status message
     * @param {string} actions - Action buttons HTML (optional)
     * @returns {string} Status indicator HTML
     */
    createStatusIndicator(status, icon, title, message, actions = '') {
        return `
            <div class="stage-status ${status}">
                <div class="status-icon">${icon}</div>
                <h3>${title}</h3>
                <p>${message}</p>
                ${actions ? `<div class="stage-actions">${actions}</div>` : ''}
            </div>
        `;
    }

    /**
     * Create an empty state container
     * @param {string} icon - Emoji icon
     * @param {string} title - Empty state title
     * @param {string} message - Empty state message
     * @param {string} primaryAction - Primary action button HTML
     * @returns {string} Empty state HTML
     */
    createEmptyState(icon, title, message, primaryAction = '') {
        return `
            <div class="empty-stage">
                <div class="empty-icon">${icon}</div>
                <h3>${title}</h3>
                <p>${message}</p>
                ${primaryAction ? `<div class="stage-actions">${primaryAction}</div>` : ''}
            </div>
        `;
    }

    /**
     * Create a locked state container
     * @param {string} title - Locked state title
     * @param {string} message - Locked state message
     * @param {string} requirements - Requirements HTML (optional)
     * @returns {string} Locked state HTML
     */
    createLockedState(title, message, requirements = '') {
        const content = `
            <div class="stage-status locked">
                <div class="status-icon">🔒</div>
                <h3>${title}</h3>
                <p>${message}</p>
                ${requirements ? `<div class="locked-requirements">${requirements}</div>` : ''}
            </div>
        `;
        return content;
    }

    /**
     * Create a getting started section with helpful information
     * @param {string} title - Section title
     * @param {string} description - Section description
     * @param {Array} items - List of items (strings)
     * @param {string} actionButton - Action button HTML
     * @returns {string} Getting started section HTML
     */
    createGettingStartedSection(title, description, items = [], actionButton = '') {
        const itemsList = items.length > 0 ?
            `<ul style="text-align: left; margin: 10px 0;">${items.map(item => `<li>${item}</li>`).join('')}</ul>` : '';

        return `
            <div class="getting-started-section" style="margin-top: 20px; padding: 15px; background: #e3f2fd; border-radius: 8px; border-left: 4px solid #2196f3;">
                <h4>${title}</h4>
                <p>${description}</p>
                ${itemsList}
                ${actionButton ? actionButton : ''}
            </div>
        `;
    }

    /**
     * Create a summary grid for displaying data
     * @param {Array} items - Array of {title, content} objects
     * @returns {string} Summary grid HTML
     */
    createSummaryGrid(items) {
        return `
            <div class="summary-grid">
                ${items.map(item => `
                    <div class="summary-section">
                        <h4>${item.title}</h4>
                        ${item.content}
                    </div>
                `).join('')}
            </div>
        `;
    }

    /**
     * Create a progress bar
     * @param {number} percentage - Progress percentage (0-100)
     * @param {string} label - Progress label
     * @returns {string} Progress bar HTML
     */
    createProgressBar(percentage, label = '') {
        return `
            <div class="progress-bar">
                <div class="progress-fill" style="width: ${Math.max(0, Math.min(100, percentage))}%"></div>
            </div>
            ${label ? `<p>${label}</p>` : ''}
        `;
    }

    /**
     * Format a list of items as HTML list
     * @param {Array} items - Array of strings
     * @param {string} emptyMessage - Message when no items
     * @returns {string} Formatted list HTML
     */
    formatList(items, emptyMessage = 'No items available') {
        if (!items || items.length === 0) {
            return `<p>${emptyMessage}</p>`;
        }
        return `<ul>${items.map(item => `<li>${item}</li>`).join('')}</ul>`;
    }

    /**
     * Format a list of items as ordered list
     * @param {Array} items - Array of strings
     * @param {string} emptyMessage - Message when no items
     * @returns {string} Formatted ordered list HTML
     */
    formatOrderedList(items, emptyMessage = 'No items available') {
        if (!items || items.length === 0) {
            return `<p>${emptyMessage}</p>`;
        }
        return `<ol>${items.map(item => `<li>${item}</li>`).join('')}</ol>`;
    }

    /**
     * Create a card grid for displaying items
     * @param {Array} cards - Array of card HTML strings
     * @param {string} cssClass - CSS class for the grid
     * @returns {string} Card grid HTML
     */
    createCardGrid(cards, cssClass = 'card-grid') {
        return `<div class="${cssClass}">${cards.join('')}</div>`;
    }

    /**
     * Create a button with consistent styling
     * @param {string} text - Button text
     * @param {string} onclick - Onclick handler
     * @param {string} cssClass - Additional CSS classes
     * @param {string} icon - Optional icon
     * @returns {string} Button HTML
     */
    createButton(text, onclick, cssClass = 'btn btn-primary', icon = '') {
        const buttonText = icon ? `${icon} ${text}` : text;
        return `<button class="${cssClass}" onclick="${onclick}">${buttonText}</button>`;
    }

    /**
     * Create a link button
     * @param {string} text - Link text
     * @param {string} href - Link href
     * @param {string} cssClass - CSS classes
     * @returns {string} Link HTML
     */
    createLink(text, href, cssClass = '') {
        return `<a href="${href}" class="${cssClass}">${text}</a>`;
    }

    /**
     * Escape HTML special characters
     * @param {string} text - Text to escape
     * @returns {string} Escaped text
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Truncate text to specified length
     * @param {string} text - Text to truncate
     * @param {number} maxLength - Maximum length
     * @returns {string} Truncated text
     */
    truncateText(text, maxLength = 200) {
        if (!text || text.length <= maxLength) {
            return text;
        }
        return text.substring(0, maxLength) + '...';
    }

    /**
     * Check if a stage is accessible based on workflow state
     * @param {number} stageNumber - Stage number (1-5)
     * @returns {boolean} True if stage is accessible
     */
    isStageAccessible(stageNumber) {
        const workflowState = this.workflowState;

        switch (stageNumber) {
            case 1: // Requirements - always accessible
                return true;
            case 2: // Planning - requires approved requirements
                return workflowState?.requirementsAnalysis?.isApproved === true;
            case 3: // Stories - requires approved requirements and planning
                return workflowState?.requirementsAnalysis?.isApproved === true &&
                    workflowState?.projectPlanning?.isApproved === true;
            case 4: // Prompts - requires approved stories
                return workflowState?.requirementsAnalysis?.isApproved === true &&
                    workflowState?.projectPlanning?.isApproved === true &&
                    workflowState?.storyGeneration?.isApproved === true;
            case 5: // Review - requires all previous stages
                return workflowState?.requirementsAnalysis?.isApproved === true &&
                    workflowState?.projectPlanning?.isApproved === true &&
                    workflowState?.storyGeneration?.isApproved === true;
            default:
                return false;
        }
    }

    /**
     * Get stage status based on workflow state
     * @param {string} stageKey - Stage key (e.g., 'requirementsAnalysis', 'projectPlanning')
     * @returns {object} Stage status information
     */
    getStageStatus(stageKey) {
        const stageData = this.workflowState?.[stageKey];
        if (!stageData) {
            return { status: 'NotStarted', isApproved: false, hasData: false };
        }

        return {
            status: stageData.status || 'NotStarted',
            isApproved: stageData.isApproved === true,
            hasData: !!(stageData.analysisId || stageData.planningId || stageData.generationId),
            id: stageData.analysisId || stageData.planningId || stageData.generationId
        };
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = BaseContentGenerator;
} else if (typeof window !== 'undefined') {
    window.BaseContentGenerator = BaseContentGenerator;
}
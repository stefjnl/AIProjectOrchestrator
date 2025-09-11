/**
 * Progress Renderer Service
 * Handles rendering of progress indicators, status updates, and progress tracking UI
 */
class ProgressRenderer {
    constructor() {
        this.progressContainer = null;
        this.progressBar = null;
        this.progressText = null;
        this.progressDetails = null;
        this.isShowing = false;
    }

    /**
     * Initialize progress renderer with DOM elements
     * @param {string} containerId - Container element ID
     * @param {string} barId - Progress bar element ID
     * @param {string} textId - Progress text element ID
     * @param {string} detailsId - Progress details element ID
     */
    initialize(containerId = 'progress-container', barId = 'progress-bar', textId = 'progress-text', detailsId = 'progress-details') {
        this.progressContainer = document.getElementById(containerId);
        this.progressBar = document.getElementById(barId);
        this.progressText = document.getElementById(textId);
        this.progressDetails = document.getElementById(detailsId);

        console.log('ProgressRenderer initialized:', {
            container: this.progressContainer,
            bar: this.progressBar,
            text: this.progressText,
            details: this.progressDetails
        });
    }

    /**
     * Show progress with message
     * @param {string} message - Progress message
     * @param {number} percentage - Progress percentage (0-100)
     */
    showProgress(message, percentage = 0) {
        if (!this.progressContainer) {
            console.warn('ProgressRenderer not initialized');
            this.createProgressElements();
        }

        this.isShowing = true;

        if (this.progressContainer) {
            this.progressContainer.style.display = 'block';
        }

        this.updateProgress(message, percentage);

        console.log('Progress shown:', { message, percentage });
    }

    /**
     * Update progress message and percentage
     * @param {string} message - Progress message
     * @param {number} percentage - Progress percentage (0-100)
     */
    updateProgress(message, percentage = 0) {
        if (this.progressText) {
            this.progressText.textContent = message;
        }

        if (this.progressBar) {
            this.progressBar.style.width = `${Math.min(100, Math.max(0, percentage))}%`;
        }

        if (this.progressDetails) {
            this.progressDetails.textContent = `${percentage}% complete`;
        }

        console.log('Progress updated:', { message, percentage });
    }

    /**
     * Show detailed progress with multiple steps
     * @param {Array} steps - Array of step objects
     * @param {number} currentStep - Current step index
     */
    showDetailedProgress(steps, currentStep = 0) {
        if (!this.progressContainer) {
            this.createProgressElements();
        }

        const totalSteps = steps.length;
        const percentage = totalSteps > 0 ? Math.round((currentStep / totalSteps) * 100) : 0;

        this.showProgress(`Step ${currentStep + 1} of ${totalSteps}: ${steps[currentStep]?.name || 'Processing'}`, percentage);

        // Create step indicators
        const stepIndicators = this.createStepIndicators(steps, currentStep);

        if (this.progressDetails) {
            this.progressDetails.innerHTML = stepIndicators;
        }

        console.log('Detailed progress shown:', { steps, currentStep, percentage });
    }

    /**
     * Create step indicators HTML
     * @param {Array} steps - Array of step objects
     * @param {number} currentStep - Current step index
     * @returns {string} HTML string
     */
    createStepIndicators(steps, currentStep) {
        return `
            <div class="step-indicators">
                ${steps.map((step, index) => `
                    <div class="step-indicator ${index < currentStep ? 'completed' : index === currentStep ? 'active' : 'pending'}">
                        <div class="step-number">${index + 1}</div>
                        <div class="step-name">${step.name}</div>
                    </div>
                `).join('')}
            </div>
        `;
    }

    /**
     * Hide progress
     */
    hideProgress() {
        this.isShowing = false;

        if (this.progressContainer) {
            this.progressContainer.style.display = 'none';
        }

        console.log('Progress hidden');
    }

    /**
     * Create progress elements if they don't exist
     */
    createProgressElements() {
        // Create container if it doesn't exist
        let container = document.getElementById('progress-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'progress-container';
            container.className = 'progress-container';
            container.innerHTML = `
                <div class="progress-header">
                    <span id="progress-text">Processing...</span>
                </div>
                <div class="progress-bar-container">
                    <div class="progress-bar-track">
                        <div id="progress-bar" class="progress-bar-fill"></div>
                    </div>
                    <div id="progress-percentage" class="progress-percentage">0%</div>
                </div>
                <div id="progress-details" class="progress-details"></div>
            `;

            // Add to page (append to body or specific container)
            const mainContainer = document.querySelector('.main-content') || document.body;
            mainContainer.appendChild(container);

            // Re-initialize with new elements
            this.initialize();
        }
    }

    /**
     * Show loading spinner
     * @param {string} message - Loading message
     * @param {HTMLElement} targetElement - Target element to show spinner in
     */
    showLoadingSpinner(message = 'Loading...', targetElement = null) {
        const spinnerHtml = `
            <div class="loading-spinner-container">
                <div class="loading-spinner"></div>
                <div class="loading-message">${message}</div>
            </div>
        `;

        if (targetElement) {
            targetElement.innerHTML = spinnerHtml;
        } else {
            // Show in main content area
            const mainContent = document.querySelector('.main-content');
            if (mainContent) {
                mainContent.innerHTML = spinnerHtml;
            }
        }

        console.log('Loading spinner shown:', message);
    }

    /**
     * Hide loading spinner
     * @param {HTMLElement} targetElement - Target element to clear spinner from
     */
    hideLoadingSpinner(targetElement = null) {
        const spinnerSelector = '.loading-spinner-container';

        if (targetElement) {
            const spinner = targetElement.querySelector(spinnerSelector);
            if (spinner) {
                spinner.remove();
            }
        } else {
            // Remove from main content area
            const spinners = document.querySelectorAll(spinnerSelector);
            spinners.forEach(spinner => spinner.remove());
        }

        console.log('Loading spinner hidden');
    }

    /**
     * Show operation status (success/error)
     * @param {string} message - Status message
     * @param {string} type - Status type ('success', 'error', 'warning', 'info')
     * @param {number} duration - Duration in milliseconds (0 for persistent)
     */
    showStatus(message, type = 'info', duration = 3000) {
        const statusClass = `status-${type}`;
        const statusIcon = this.getStatusIcon(type);

        const statusHtml = `
            <div class="status-message ${statusClass}">
                <span class="status-icon">${statusIcon}</span>
                <span class="status-text">${message}</span>
                <button class="status-close" onclick="this.parentElement.remove()">×</button>
            </div>
        `;

        // Add to status container or create one
        let statusContainer = document.getElementById('status-container');
        if (!statusContainer) {
            statusContainer = document.createElement('div');
            statusContainer.id = 'status-container';
            statusContainer.className = 'status-container';
            document.body.appendChild(statusContainer);
        }

        const statusElement = document.createElement('div');
        statusElement.innerHTML = statusHtml;
        statusContainer.appendChild(statusElement.firstElementChild);

        console.log('Status shown:', { message, type, duration });

        // Auto-remove after duration
        if (duration > 0) {
            setTimeout(() => {
                const element = document.querySelector(`.status-message.${statusClass}`);
                if (element) {
                    element.remove();
                }
            }, duration);
        }
    }

    /**
     * Get status icon based on type
     * @param {string} type - Status type
     * @returns {string} Icon character
     */
    getStatusIcon(type) {
        const icons = {
            success: '✅',
            error: '❌',
            warning: '⚠️',
            info: 'ℹ️'
        };
        return icons[type] || icons.info;
    }

    /**
     * Show confirmation dialog
     * @param {string} message - Confirmation message
     * @param {string} title - Dialog title
     * @returns {Promise<boolean>} User confirmation
     */
    async showConfirmation(message, title = 'Confirm Action') {
        return new Promise((resolve) => {
            const confirmationHtml = `
                <div class="confirmation-overlay">
                    <div class="confirmation-dialog">
                        <h3>${title}</h3>
                        <p>${message}</p>
                        <div class="confirmation-actions">
                            <button class="btn btn-primary" id="confirm-yes">Yes</button>
                            <button class="btn btn-secondary" id="confirm-no">No</button>
                        </div>
                    </div>
                </div>
            `;

            // Add to body
            const overlay = document.createElement('div');
            overlay.innerHTML = confirmationHtml;
            document.body.appendChild(overlay.firstElementChild);

            // Handle button clicks
            document.getElementById('confirm-yes').addEventListener('click', () => {
                document.querySelector('.confirmation-overlay').remove();
                resolve(true);
            });

            document.getElementById('confirm-no').addEventListener('click', () => {
                document.querySelector('.confirmation-overlay').remove();
                resolve(false);
            });

            console.log('Confirmation dialog shown:', { message, title });
        });
    }

    /**
     * Update button states
     * @param {Object} buttonStates - Button states object
     */
    updateButtonStates(buttonStates) {
        Object.keys(buttonStates).forEach(buttonId => {
            const button = document.getElementById(buttonId);
            if (button) {
                const state = buttonStates[buttonId];

                if (state.disabled !== undefined) {
                    button.disabled = state.disabled;
                }

                if (state.text) {
                    button.textContent = state.text;
                }

                if (state.visible !== undefined) {
                    button.style.display = state.visible ? 'inline-block' : 'none';
                }
            }
        });

        console.log('Button states updated:', buttonStates);
    }

    /**
     * Render error message
     * @param {string} message - Error message
     * @param {string} details - Error details
     * @returns {string} HTML string
     */
    renderErrorMessage(message, details = '') {
        return `
            <div class="error-message">
                <div class="error-icon">❌</div>
                <div class="error-content">
                    <h4>Error</h4>
                    <p>${message}</p>
                    ${details ? `<p class="error-details">${details}</p>` : ''}
                </div>
                <button class="error-close" onclick="this.parentElement.remove()">×</button>
            </div>
        `;
    }

    /**
     * Render success message
     * @param {string} message - Success message
     * @param {string} details - Success details
     * @returns {string} HTML string
     */
    renderSuccessMessage(message, details = '') {
        return `
            <div class="success-message">
                <div class="success-icon">✅</div>
                <div class="success-content">
                    <h4>Success</h4>
                    <p>${message}</p>
                    ${details ? `<p class="success-details">${details}</p>` : ''}
                </div>
                <button class="success-close" onclick="this.parentElement.remove()">×</button>
            </div>
        `;
    }

    /**
     * Show notification toast
     * @param {string} message - Notification message
     * @param {string} type - Notification type ('success', 'error', 'warning', 'info')
     * @param {number} duration - Duration in milliseconds
     */
    showNotification(message, type = 'info', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <span class="notification-icon">${this.getStatusIcon(type)}</span>
            <span class="notification-message">${message}</span>
        `;

        // Add to notification container or create one
        let notificationContainer = document.getElementById('notification-container');
        if (!notificationContainer) {
            notificationContainer = document.createElement('div');
            notificationContainer.id = 'notification-container';
            notificationContainer.className = 'notification-container';
            document.body.appendChild(notificationContainer);
        }

        notificationContainer.appendChild(notification);

        // Animate in
        setTimeout(() => notification.classList.add('show'), 100);

        // Auto-remove after duration
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, duration);

        console.log('Notification shown:', { message, type, duration });
    }

    /**
     * Clear all notifications
     */
    clearNotifications() {
        const notifications = document.querySelectorAll('.notification');
        notifications.forEach(notification => notification.remove());

        console.log('All notifications cleared');
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ProgressRenderer };
}

// Make available globally for backward compatibility
window.ProgressRenderer = ProgressRenderer;
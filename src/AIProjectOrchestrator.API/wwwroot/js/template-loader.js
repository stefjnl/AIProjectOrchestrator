/**
 * Template Loader for AI Project Orchestrator
 * Loads the master template and injects page-specific content
 */

document.addEventListener('DOMContentLoaded', function () {
    // Get the current page content
    const pageContent = document.querySelector('.page-content');

    if (pageContent) {
        // Find the dashboard container in the master template
        const dashboardContainer = document.querySelector('.dashboard-container');

        if (dashboardContainer) {
            // Move the page content to the dashboard container
            dashboardContainer.innerHTML = pageContent.innerHTML;

            // Remove the original page content container
            pageContent.remove();
        }
    }

    // Add active class to current navigation button
    updateActiveNavigation();

    // Initialize any page-specific functionality
    initializePageFeatures();
});

/**
 * Update active navigation button based on current page
 */
function updateActiveNavigation() {
    const currentPath = window.location.pathname;
    const navButtons = document.querySelectorAll('.nav-button');

    navButtons.forEach(button => {
        const buttonPath = button.getAttribute('href');

        // Check if this button corresponds to the current page
        if (currentPath === buttonPath ||
            (currentPath === '/' && buttonPath === '/') ||
            (currentPath.includes(buttonPath) && buttonPath !== '/')) {
            button.classList.add('active');
        }
    });
}

/**
 * Initialize page-specific features
 */
function initializePageFeatures() {
    // Initialize markdown rendering if markdown-utils.js is loaded
    if (typeof renderMarkdown === 'function') {
        const markdownElements = document.querySelectorAll('.markdown-content');
        markdownElements.forEach(element => {
            const content = element.textContent || element.innerText;
            element.innerHTML = renderMarkdown(content);
        });
    }

    // Initialize form enhancements
    initializeFormEnhancements();

    // Initialize any dynamic content loading
    initializeDynamicContent();
}

/**
 * Initialize form enhancements
 */
function initializeFormEnhancements() {
    // Add form validation enhancements
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            // Add loading state to submit buttons
            const submitButton = form.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.textContent = 'Processing...';
            }
        });
    });
}

/**
 * Initialize dynamic content loading
 */
function initializeDynamicContent() {
    // Add any dynamic content loading functionality here
    // This can be extended based on specific page requirements
}

/**
 * Utility function to show loading indicators
 */
function showLoading(container, message = 'Loading...') {
    if (container) {
        container.innerHTML = `<div class="loading-indicator">${message}</div>`;
    }
}

/**
 * Utility function to show error messages
 */
function showError(container, message) {
    if (container) {
        container.innerHTML = `<div class="error-message">${message}</div>`;
    }
}

/**
 * Utility function to show success messages
 */
function showSuccess(container, message) {
    if (container) {
        container.innerHTML = `<div class="success-message">${message}</div>`;
    }
}
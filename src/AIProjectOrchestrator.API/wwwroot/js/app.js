// Global Application JavaScript
document.addEventListener('DOMContentLoaded', function () {
    console.log('AI Project Orchestrator - App initialized');

    // Initialize global functionality
    initializeNavigation();
    initializeNotifications();
    initializeLoadingStates();
    initializeFormValidation();
    initializeTooltips();

    // Auto-refresh functionality for workflow pages
    if (window.location.pathname.includes('/Projects/Workflow')) {
        initializeWorkflowAutoRefresh();
    }
});

// Navigation functionality
function initializeNavigation() {
    // Add smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Mobile menu toggle (if needed in future)
    const mobileMenuToggle = document.querySelector('.mobile-menu-toggle');
    if (mobileMenuToggle) {
        mobileMenuToggle.addEventListener('click', function () {
            document.querySelector('.header-nav').classList.toggle('mobile-active');
        });
    }
}

// Notification system
function initializeNotifications() {
    // Create notification container if it doesn't exist
    if (!document.querySelector('.notification-container')) {
        const container = document.createElement('div');
        container.className = 'notification-container';
        container.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 10000;
            display: flex;
            flex-direction: column;
            gap: 10px;
            max-width: 400px;
        `;
        document.body.appendChild(container);
    }
}

// Show notification
function showNotification(message, type = 'info', duration = 3000) {
    const container = document.querySelector('.notification-container');
    if (!container) return;

    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;

    // Set colors based on type
    const colors = {
        success: { bg: 'var(--color-success-500)', text: 'white' },
        error: { bg: 'var(--color-danger-500)', text: 'white' },
        warning: { bg: 'var(--color-warning-500)', text: 'white' },
        info: { bg: 'var(--color-primary-500)', text: 'white' }
    };

    const color = colors[type] || colors.info;

    notification.style.cssText = `
        background: ${color.bg};
        color: ${color.text};
        padding: 12px 16px;
        border-radius: 8px;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
        font-size: 14px;
        font-weight: 500;
        animation: slideInRight 0.3s ease;
        display: flex;
        align-items: center;
        gap: 8px;
    `;

    // Add icon based on type
    const icons = {
        success: '✅',
        error: '❌',
        warning: '⚠️',
        info: 'ℹ️'
    };

    notification.innerHTML = `
        <span class="notification-icon">${icons[type] || 'ℹ️'}</span>
        <span class="notification-message">${message}</span>
        <button class="notification-close" onclick="this.parentElement.remove()">&times;</button>
    `;

    container.appendChild(notification);

    // Auto-remove after duration
    setTimeout(() => {
        if (notification.parentElement) {
            notification.style.animation = 'slideOutRight 0.3s ease';
            setTimeout(() => notification.remove(), 300);
        }
    }, duration);
}

// Loading states
function initializeLoadingStates() {
    // Add loading animation styles
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInRight {
            from { transform: translateX(100%); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
        
        @keyframes slideOutRight {
            from { transform: translateX(0); opacity: 1; }
            to { transform: translateX(100%); opacity: 0; }
        }
        
        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
        }
        
        .loading-overlay {
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.5);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 9999;
            backdrop-filter: blur(4px);
        }
        
        .loading-spinner {
            width: 40px;
            height: 40px;
            border: 4px solid var(--color-gray-200);
            border-top: 4px solid var(--color-primary-500);
            border-radius: 50%;
            animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
        
        .btn-loading {
            position: relative;
            color: transparent;
        }
        
        .btn-loading::after {
            content: '';
            position: absolute;
            width: 16px;
            height: 16px;
            top: 50%;
            left: 50%;
            margin-left: -8px;
            margin-top: -8px;
            border: 2px solid #ffffff;
            border-radius: 50%;
            border-top-color: transparent;
            animation: spin 0.8s linear infinite;
        }
    `;
    document.head.appendChild(style);
}

// Show loading overlay
function showLoading(message = 'Loading...') {
    const overlay = document.createElement('div');
    overlay.className = 'loading-overlay';
    overlay.innerHTML = `
        <div class="loading-content">
            <div class="loading-spinner"></div>
            <p>${message}</p>
        </div>
    `;

    overlay.querySelector('.loading-content').style.cssText = `
        background: white;
        padding: 2rem;
        border-radius: 12px;
        text-align: center;
        box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.1);
    `;

    overlay.querySelector('p').style.cssText = `
        margin-top: 1rem;
        color: var(--color-gray-600);
        font-weight: 500;
    `;

    document.body.appendChild(overlay);
    return overlay;
}

// Hide loading overlay
function hideLoading(overlay) {
    if (overlay && overlay.parentElement) {
        overlay.remove();
    }
}

// Form validation
function initializeFormValidation() {
    // Add real-time validation to form inputs
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function (e) {
            if (!validateForm(this)) {
                e.preventDefault();
                showNotification('Please fix the validation errors before submitting.', 'error');
            }
        });
    });

    // Real-time validation for inputs
    document.querySelectorAll('input[required], textarea[required]').forEach(input => {
        input.addEventListener('blur', function () {
            validateField(this);
        });

        input.addEventListener('input', function () {
            clearFieldError(this);
        });
    });
}

// Validate entire form
function validateForm(form) {
    let isValid = true;
    const requiredFields = form.querySelectorAll('[required]');

    requiredFields.forEach(field => {
        if (!validateField(field)) {
            isValid = false;
        }
    });

    return isValid;
}

// Validate individual field
function validateField(field) {
    const value = field.value.trim();
    let isValid = true;
    let errorMessage = '';

    // Required field validation
    if (field.hasAttribute('required') && !value) {
        isValid = false;
        errorMessage = 'This field is required.';
    }

    // Email validation
    if (field.type === 'email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address.';
        }
    }

    // URL validation
    if (field.type === 'url' && value) {
        try {
            new URL(value);
        } catch {
            isValid = false;
            errorMessage = 'Please enter a valid URL.';
        }
    }

    // Min length validation
    const minLength = field.getAttribute('minlength');
    if (minLength && value.length < minLength) {
        isValid = false;
        errorMessage = `Minimum length is ${minLength} characters.`;
    }

    // Max length validation
    const maxLength = field.getAttribute('maxlength');
    if (maxLength && value.length > maxLength) {
        isValid = false;
        errorMessage = `Maximum length is ${maxLength} characters.`;
    }

    if (!isValid) {
        showFieldError(field, errorMessage);
    } else {
        clearFieldError(field);
    }

    return isValid;
}

// Show field error
function showFieldError(field, message) {
    clearFieldError(field);

    const errorDiv = document.createElement('div');
    errorDiv.className = 'field-error';
    errorDiv.textContent = message;
    errorDiv.style.cssText = `
        color: var(--color-danger-500);
        font-size: 0.875rem;
        margin-top: 0.25rem;
        font-weight: 500;
    `;

    field.classList.add('error');
    field.style.borderColor = 'var(--color-danger-500)';
    field.parentElement.appendChild(errorDiv);
}

// Clear field error
function clearFieldError(field) {
    field.classList.remove('error');
    field.style.borderColor = '';

    const errorDiv = field.parentElement.querySelector('.field-error');
    if (errorDiv) {
        errorDiv.remove();
    }
}

// Tooltips
function initializeTooltips() {
    // Add tooltip functionality to elements with data-tooltip attribute
    document.querySelectorAll('[data-tooltip]').forEach(element => {
        element.addEventListener('mouseenter', function (e) {
            showTooltip(e, this.getAttribute('data-tooltip'));
        });

        element.addEventListener('mouseleave', function () {
            hideTooltip();
        });
    });
}

// Show tooltip
function showTooltip(event, text) {
    const tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    tooltip.textContent = text;
    tooltip.style.cssText = `
        position: fixed;
        background: var(--color-gray-900);
        color: white;
        padding: 8px 12px;
        border-radius: 6px;
        font-size: 0.875rem;
        white-space: nowrap;
        z-index: 10001;
        pointer-events: none;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
    `;

    document.body.appendChild(tooltip);

    // Position tooltip
    const rect = event.target.getBoundingClientRect();
    tooltip.style.left = `${rect.left + rect.width / 2 - tooltip.offsetWidth / 2}px`;
    tooltip.style.top = `${rect.top - tooltip.offsetHeight - 8}px`;
}

// Hide tooltip
function hideTooltip() {
    const tooltip = document.querySelector('.tooltip');
    if (tooltip) {
        tooltip.remove();
    }
}

// Workflow auto-refresh
function initializeWorkflowAutoRefresh() {
    let refreshInterval;

    // Start auto-refresh
    function startAutoRefresh() {
        if (refreshInterval) return;

        refreshInterval = setInterval(() => {
            // Trigger custom event for workflow updates
            window.dispatchEvent(new CustomEvent('workflow-refresh'));
        }, 10000); // Refresh every 10 seconds
    }

    // Stop auto-refresh
    function stopAutoRefresh() {
        if (refreshInterval) {
            clearInterval(refreshInterval);
            refreshInterval = null;
        }
    }

    // Start auto-refresh when page is visible
    startAutoRefresh();

    // Stop auto-refresh when page is hidden
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            stopAutoRefresh();
        } else {
            startAutoRefresh();
        }
    });
}

// Utility functions
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function throttle(func, limit) {
    let inThrottle;
    return function () {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Export utility functions for use in other scripts
window.App = {
    showNotification,
    showLoading,
    hideLoading,
    validateForm,
    validateField,
    debounce,
    throttle
};
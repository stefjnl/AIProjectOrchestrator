// Utility functions for rendering Markdown descriptions securely
// Uses marked.js for parsing and DOMPurify for sanitization

/**
 * Renders Markdown text to safe HTML.
 * @param {string} markdownText - The Markdown content to render.
 * @returns {string} Sanitized HTML string.
 */
function renderMarkdownToHTML(markdownText) {
    if (!markdownText || typeof markdownText !== 'string') {
        return ''; // Fallback for empty or invalid input
    }

    try {
        // Check if required libraries are loaded
        if (typeof marked === 'undefined') {
            console.warn('marked.js not loaded. Falling back to plain text.');
            return escapeHTML(markdownText);
        }
        if (typeof DOMPurify === 'undefined') {
            console.warn('DOMPurify not loaded. Rendering without sanitization (insecure).');
            return marked.parse(markdownText, { breaks: true });
        }

        // Parse Markdown with GitHub Flavored Markdown options
        const rawHtml = marked.parse(markdownText, {
            breaks: true, // Convert line breaks to <br>
            gfm: true,    // Enable GitHub Flavored Markdown
            headerIds: false // Disable auto-generated IDs for security
        });

        // Sanitize the HTML to prevent XSS
        const safeHtml = DOMPurify.sanitize(rawHtml, {
            ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'ul', 'ol', 'li', 'blockquote', 'code', 'pre', 'a', 'img'],
            ALLOWED_ATTR: ['href', 'src', 'alt', 'title']
        });

        return safeHtml;
    } catch (error) {
        console.error('Error rendering Markdown:', error);
        // Fallback to escaped plain text on error
        return escapeHTML(markdownText);
    }
}

/**
 * Escapes HTML to prevent injection (fallback function).
 * @param {string} str - The string to escape.
 * @returns {string} Escaped HTML.
 */
function escapeHTML(str) {
    const div = document.createElement('div');
    div.textContent = str || '';
    return div.innerHTML;
}

// Export for module usage if needed (for modern bundlers)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { renderMarkdownToHTML, escapeHTML };
}

/**
 * Toggles truncated/expanded state for Markdown descriptions.
 * @param {HTMLElement} element - The description element to toggle.
 * @param {string} maxLength - Maximum characters before truncation (default: 150).
 */
function toggleDescription(element, maxLength = 150) {
    if (element.classList.contains('truncated')) {
        // Expand
        element.classList.remove('truncated');
        element.innerHTML = element.dataset.fullContent;
        element.nextElementSibling.textContent = 'Show less';
    } else {
        // Truncate
        const fullText = element.innerHTML;
        element.dataset.fullContent = fullText;
        const truncatedText = fullText.length > maxLength ? fullText.substring(0, maxLength) + '...' : fullText;
        element.innerHTML = truncatedText;
        element.classList.add('truncated');
        element.nextElementSibling.textContent = 'Show more';
    }
}

/**
 * Initializes truncation for a description element.
 * @param {HTMLElement} element - The description element.
 * @param {string} maxLength - Maximum characters before truncation (default: 150).
 */
function initTruncatedDescription(element, maxLength = 150) {
    const fullText = element.innerHTML;
    if (fullText.length > maxLength) {
        element.dataset.fullContent = fullText;
        element.innerHTML = fullText.substring(0, maxLength) + '...';
        element.classList.add('truncated');

        const toggleBtn = document.createElement('button');
        toggleBtn.className = 'toggle-description-btn';
        toggleBtn.textContent = 'Show more';
        toggleBtn.onclick = () => toggleDescription(element, maxLength);
        element.parentNode.insertBefore(toggleBtn, element.nextSibling);
    }
}
// Status utility functions for consistent story status handling
const StoryStatus = {
    DRAFT: 0,      // Pending approval
    APPROVED: 1,   // Approved for prompt generation
    REJECTED: 2    // Rejected with feedback
};

const StatusNames = {
    [StoryStatus.DRAFT]: 'pending',
    [StoryStatus.APPROVED]: 'approved',
    [StoryStatus.REJECTED]: 'rejected'
};

/**
 * Normalizes a story status to numeric format
 * @param {number|string} status - Status value (0-2 or 'pending'/'approved'/'rejected')
 * @returns {number} Normalized numeric status (0-2)
 */
function normalizeStoryStatus(status) {
    if (typeof status === 'string') {
        const lower = status.toLowerCase();
        switch (lower) {
            case 'pending': return StoryStatus.DRAFT;
            case 'approved': return StoryStatus.APPROVED;
            case 'rejected': return StoryStatus.REJECTED;
            default: return StoryStatus.DRAFT;
        }
    }
    // Ensure numeric status is within valid range
    const numericStatus = parseInt(status);
    return (numericStatus >= 0 && numericStatus <= 2) ? numericStatus : StoryStatus.DRAFT;
}

/**
 * Gets the display name for a status
 * @param {number|string} status - Status value
 * @returns {string} Display name ('pending', 'approved', 'rejected')
 */
function getStatusName(status) {
    const normalized = normalizeStoryStatus(status);
    return StatusNames[normalized] || 'pending';
}

/**
 * Checks if a story can be approved
 * @param {number|string} status - Current story status
 * @returns {boolean} True if story can be approved
 */
function canApproveStory(status) {
    return normalizeStoryStatus(status) === StoryStatus.DRAFT;
}

/**
 * Checks if a story can be rejected
 * @param {number|string} status - Current story status
 * @returns {boolean} True if story can be rejected
 */
function canRejectStory(status) {
    return normalizeStoryStatus(status) === StoryStatus.DRAFT;
}

/**
 * Checks if a prompt can be generated for a story
 * @param {number|string} status - Current story status
 * @param {boolean} hasPrompt - Whether story already has a prompt
 * @returns {boolean} True if prompt can be generated
 */
function canGeneratePrompt(status, hasPrompt = false) {
    return normalizeStoryStatus(status) === StoryStatus.APPROVED && !hasPrompt;
}

/**
 * Gets CSS class for status styling
 * @param {number|string} status - Story status
 * @returns {string} CSS class name
 */
function getStatusClass(status) {
    const normalized = normalizeStoryStatus(status);
    switch (normalized) {
        case StoryStatus.APPROVED: return 'approved';
        case StoryStatus.REJECTED: return 'rejected';
        default: return 'pending';
    }
}

// Export for use in other modules
window.StatusUtils = {
    StoryStatus,
    StatusNames,
    normalizeStoryStatus,
    getStatusName,
    canApproveStory,
    canRejectStory,
    canGeneratePrompt,
    getStatusClass
};
// Jest setup file for AI Project Orchestrator frontend tests

// Mock browser APIs and global objects
global.window = global.window || {};
global.document = global.document || {};
global.console = global.console || {
    log: jest.fn(),
    warn: jest.fn(),
    error: jest.fn()
};

// Mock APIClient
global.APIClient = {
    getRequirements: jest.fn(),
    getProjectPlan: jest.fn(),
    getStories: jest.fn(),
    getPrompts: jest.fn(),
    getPendingReviews: jest.fn(),
    getApprovedStories: jest.fn(),
    getProject: jest.fn(),
    analyzeRequirements: jest.fn(),
    createProjectPlan: jest.fn(),
    generateStories: jest.fn(),
    generatePrompt: jest.fn()
};

// Mock window.App
global.window.App = {
    showNotification: jest.fn()
};

// Mock document methods
if (!global.document.getElementById) {
    global.document.getElementById = jest.fn(() => null);
}

if (!global.document.querySelectorAll) {
    global.document.querySelectorAll = jest.fn(() => []);
}

if (!global.document.addEventListener) {
    global.document.addEventListener = jest.fn();
}

// Mock common DOM elements
const mockElements = {
    'stage-content': { innerHTML: '' },
    'prev-stage': { addEventListener: jest.fn(), disabled: false },
    'next-stage': { addEventListener: jest.fn(), disabled: false, textContent: 'Next →' },
    'stage-counter': { textContent: 'Stage 1 of 5' },
    'auto-refresh-toggle': { addEventListener: jest.fn(), checked: false },
    'project-name': { textContent: 'Test Project' },
    'project-status': { textContent: 'Active' },
    'project-created': { textContent: '2024-01-01' },
    'project-progress': { textContent: '0%' }
};

// Enhance getElementById mock
const originalGetElementById = global.document.getElementById;
global.document.getElementById = jest.fn((id) => {
    return mockElements[id] || originalGetElementById(id);
});

// Mock stage indicators
global.document.querySelectorAll = jest.fn((selector) => {
    if (selector === '.stage-indicator') {
        return [
            { id: 'stage-1', addEventListener: jest.fn(), classList: { add: jest.fn(), remove: jest.fn() } },
            { id: 'stage-2', addEventListener: jest.fn(), classList: { add: jest.fn(), remove: jest.fn() } },
            { id: 'stage-3', addEventListener: jest.fn(), classList: { add: jest.fn(), remove: jest.fn() } },
            { id: 'stage-4', addEventListener: jest.fn(), classList: { add: jest.fn(), remove: jest.fn() } },
            { id: 'stage-5', addEventListener: jest.fn(), classList: { add: jest.fn(), remove: jest.fn() } }
        ];
    }
    return [];
});

// Mock console methods to reduce test noise
const originalConsole = { ...console };
beforeEach(() => {
    console.log = jest.fn();
    console.warn = jest.fn();
    console.error = jest.fn();
});

afterEach(() => {
    jest.clearAllMocks();
});

// Utility functions for tests
global.testUtils = {
    // Create a mock workflow manager
    createMockWorkflowManager: (overrides = {}) => ({
        projectId: 'test-project-123',
        currentStage: 1,
        workflowState: {
            requirementsAnalysis: { analysisId: 'req-123', status: 'NotStarted', isApproved: false },
            projectPlanning: { planningId: 'plan-123', status: 'NotStarted', isApproved: false },
            storyGeneration: { generationId: 'story-123', status: 'NotStarted', isApproved: false },
            promptGeneration: { completionPercentage: 0, status: 'NotStarted' }
        },
        isNewProject: false,
        navigateStage: jest.fn(),
        jumpToStage: jest.fn(),
        startAutoRefresh: jest.fn(),
        stopAutoRefresh: jest.fn(),
        getRequirementsStage: jest.fn(),
        getPlanningStage: jest.fn(),
        getStoriesStage: jest.fn(),
        getPromptsStage: jest.fn(),
        getReviewStage: jest.fn(),
        initializeRequirementsStage: jest.fn(),
        initializePlanningStage: jest.fn(),
        initializeStoriesStage: jest.fn(),
        initializePromptsStage: jest.fn(),
        initializeReviewStage: jest.fn(),
        ...overrides
    }),

    // Create mock API responses
    createMockApiResponse: (data, success = true) => ({
        success,
        data,
        message: success ? 'Success' : 'Error',
        timestamp: new Date().toISOString()
    }),

    // Simulate async operations
    async simulateAsyncOperation(operation, delay = 0) {
        if (delay > 0) {
            await new Promise(resolve => setTimeout(resolve, delay));
        }
        return operation();
    },

    // Mock DOM events
    createMockEvent: (type, data = {}) => ({
        type,
        target: data.target || {},
        key: data.key || '',
        preventDefault: jest.fn(),
        stopPropagation: jest.fn(),
        ...data
    })
};

// Test environment configuration
process.env.NODE_ENV = 'test';

// Suppress specific warnings that are expected in tests
const originalWarn = console.warn;
console.warn = (...args) => {
    const message = args.join(' ');
    if (message.includes('Unknown stage') || message.includes('Service failed')) {
        // Expected warnings, don't log them
        return;
    }
    originalWarn(...args);
};
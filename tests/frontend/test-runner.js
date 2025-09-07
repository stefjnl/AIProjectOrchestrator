// Simple Test Runner for Frontend Integration Tests
// This script runs all frontend integration tests and reports results

const fs = require('fs');
const path = require('path');

// Mock environment for testing
global.window = {
    localStorage: {
        store: {},
        getItem: function (key) { return this.store[key] || null; },
        setItem: function (key, value) { this.store[key] = value.toString(); },
        removeItem: function (key) { delete this.store[key]; }
    }
};

global.document = {
    elements: {},
    createElement: function (tag) {
        return { tag, id: null, className: '', disabled: false, textContent: '' };
    },
    getElementById: function (id) {
        return this.elements[id] || null;
    },
    body: {
        appendChild: function (element) {
            if (element.id) {
                this.elements[element.id] = element;
            }
        }
    }
};

global.fetch = function (url, options) {
    return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({}),
        text: () => Promise.resolve(''),
        status: 200
    });
};

// Test results tracker
const testResults = {
    passed: 0,
    failed: 0,
    total: 0,
    details: []
};

// Simple test framework
function describe(description, testFunction) {
    console.log(`\n${description}`);
    testFunction();
}

function test(description, testFunction) {
    testResults.total++;
    try {
        testFunction();
        testResults.passed++;
        testResults.details.push({ description, passed: true });
        console.log(`  ✓ ${description}`);
    } catch (error) {
        testResults.failed++;
        testResults.details.push({ description, passed: false, error: error.message });
        console.log(`  ✗ ${description}`);
        console.log(`    Error: ${error.message}`);
    }
}

function expect(actual) {
    return {
        toBe: function (expected) {
            if (actual !== expected) {
                throw new Error(`Expected ${expected} but got ${actual}`);
            }
        },
        toEqual: function (expected) {
            if (JSON.stringify(actual) !== JSON.stringify(expected)) {
                throw new Error(`Expected ${JSON.stringify(expected)} but got ${JSON.stringify(actual)}`);
            }
        }
    };
}

// Mock the WorkflowManager
class WorkflowManager {
    constructor() {
        this.projectId = null;
        this.workflowState = {
            requirements: { analysisId: null, status: 'not_started', reviewId: null },
            planning: { planningId: null, status: 'not_started', reviewId: null },
            stories: { generationId: null, status: 'not_started', reviewId: null },
            code: { generationId: null, status: 'not_started', reviewId: null }
        };
    }

    setProjectId(projectId) {
        this.projectId = projectId;
        this.loadState();
    }

    loadState() {
        if (!this.projectId) return;
        try {
            const savedState = window.localStorage.getItem(`workflow_${this.projectId}`);
            if (savedState) {
                this.workflowState = JSON.parse(savedState);
            }
        } catch (error) {
            console.error('Error loading workflow state:', error);
        }
    }

    saveState() {
        if (!this.projectId) return;
        try {
            window.localStorage.setItem(`workflow_${this.projectId}`, JSON.stringify(this.workflowState));
        } catch (error) {
            console.error('Error saving workflow state:', error);
        }
    }

    setRequirementsAnalysis(analysisId, reviewId) {
        this.workflowState.requirements.analysisId = analysisId;
        this.workflowState.requirements.reviewId = reviewId;
        this.workflowState.requirements.status = 'pending_review';
        this.saveState();
    }

    getRequirementsAnalysisId() {
        return this.workflowState.requirements.analysisId;
    }

    setProjectPlanning(planningId, reviewId) {
        this.workflowState.planning.planningId = planningId;
        this.workflowState.planning.reviewId = reviewId;
        this.workflowState.planning.status = 'pending_review';
        this.saveState();
    }

    getProjectPlanningId() {
        return this.workflowState.planning.planningId;
    }

    canStartPlanning() {
        return this.workflowState.requirements.status === 'pending_review' ||
            this.workflowState.requirements.status === 'approved';
    }

    setStoryGeneration(generationId, reviewId) {
        this.workflowState.stories.generationId = generationId;
        this.workflowState.stories.reviewId = reviewId;
        this.workflowState.stories.status = 'pending_review';
        this.saveState();
    }

    getStoryGenerationId() {
        return this.workflowState.stories.generationId;
    }

    canGenerateStories() {
        return this.workflowState.planning.status === 'pending_review' ||
            this.workflowState.planning.status === 'approved';
    }

    setCodeGeneration(generationId, reviewId) {
        this.workflowState.code.generationId = generationId;
        this.workflowState.code.reviewId = reviewId;
        this.workflowState.code.status = 'pending_review';
        this.saveState();
    }

    getCodeGenerationId() {
        return this.workflowState.code.generationId;
    }

    canGenerateCode() {
        return this.workflowState.stories.status === 'pending_review' ||
            this.workflowState.stories.status === 'approved';
    }

    updateWorkflowUI() {
        // Mock UI update function
    }
}

// Run tests
describe('Frontend Integration Tests', () => {
    test('should initialize workflow manager correctly', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');

        expect(workflowManager.projectId).toBe('test-project');
        expect(workflowManager.workflowState.requirements.status).toBe('not_started');
    });

    test('should track requirements analysis', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');

        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');

        expect(workflowManager.getRequirementsAnalysisId()).toBe('req-123');
        expect(workflowManager.workflowState.requirements.status).toBe('pending_review');
        expect(workflowManager.canStartPlanning()).toBe(true);
    });

    test('should track project planning', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');

        workflowManager.setProjectPlanning('plan-123', 'rev-456');

        expect(workflowManager.getProjectPlanningId()).toBe('plan-123');
        expect(workflowManager.workflowState.planning.status).toBe('pending_review');
        expect(workflowManager.canGenerateStories()).toBe(true);
    });

    test('should track story generation', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');

        workflowManager.setStoryGeneration('story-123', 'rev-789');

        expect(workflowManager.getStoryGenerationId()).toBe('story-123');
        expect(workflowManager.workflowState.stories.status).toBe('pending_review');
        expect(workflowManager.canGenerateCode()).toBe(true);
    });

    test('should track code generation', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');

        workflowManager.setCodeGeneration('code-123', 'rev-012');

        expect(workflowManager.getCodeGenerationId()).toBe('code-123');
        expect(workflowManager.workflowState.code.status).toBe('pending_review');
    });

    test('should persist workflow state', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');

        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        workflowManager.setProjectPlanning('plan-123', 'rev-456');

        const newWorkflowManager = new WorkflowManager();
        newWorkflowManager.setProjectId('test-project');

        expect(newWorkflowManager.getRequirementsAnalysisId()).toBe('req-123');
        expect(newWorkflowManager.getProjectPlanningId()).toBe('plan-123');
    });

    test('should correctly determine workflow transitions', () => {
        // Clear localStorage to ensure fresh state
        global.window.localStorage.store = {};

        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');

        // Initially nothing should be available
        console.log('Debug - Initial state check:');
        const canStartPlanning = workflowManager.canStartPlanning();
        const canGenerateStories = workflowManager.canGenerateStories();
        const canGenerateCode = workflowManager.canGenerateCode();
        console.log('  canStartPlanning():', canStartPlanning);
        console.log('  canGenerateStories():', canGenerateStories);
        console.log('  canGenerateCode():', canGenerateCode);
        console.log('  requirements.status:', workflowManager.workflowState.requirements.status);
        console.log('  planning.status:', workflowManager.workflowState.planning.status);
        console.log('  stories.status:', workflowManager.workflowState.stories.status);

        expect(canStartPlanning).toBe(false);
        expect(canGenerateStories).toBe(false);
        expect(canGenerateCode).toBe(false);

        // After requirements, planning should be available
        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        expect(workflowManager.canStartPlanning()).toBe(true);
        expect(workflowManager.canGenerateStories()).toBe(false);
        expect(workflowManager.canGenerateCode()).toBe(false);

        // After planning, stories should be available
        workflowManager.setProjectPlanning('plan-123', 'rev-456');
        expect(workflowManager.canStartPlanning()).toBe(true);
        expect(workflowManager.canGenerateStories()).toBe(true);
        expect(workflowManager.canGenerateCode()).toBe(false);

        // After stories, code should be available
        workflowManager.setStoryGeneration('story-123', 'rev-789');

        // Debug: Check the actual values
        console.log('Debug - After story generation:');
        console.log('  canStartPlanning():', workflowManager.canStartPlanning());
        console.log('  canGenerateStories():', workflowManager.canGenerateStories());
        console.log('  canGenerateCode():', workflowManager.canGenerateCode());
        console.log('  requirements.status:', workflowManager.workflowState.requirements.status);
        console.log('  planning.status:', workflowManager.workflowState.planning.status);
        console.log('  stories.status:', workflowManager.workflowState.stories.status);

        expect(workflowManager.canStartPlanning()).toBe(true);
        expect(workflowManager.canGenerateStories()).toBe(true);
        expect(workflowManager.canGenerateCode()).toBe(true);
    });
});

// Report results
console.log(`\n\nTest Results:`);
console.log(`=============`);
console.log(`Total: ${testResults.total}`);
console.log(`Passed: ${testResults.passed}`);
console.log(`Failed: ${testResults.failed}`);

if (testResults.failed > 0) {
    console.log(`\nFailed Tests:`);
    testResults.details.filter(detail => !detail.passed).forEach(detail => {
        console.log(`  - ${detail.description}: ${detail.error}`);
    });
}

console.log(`\nOverall Result: ${testResults.failed === 0 ? 'PASS' : 'FAIL'}`);

process.exit(testResults.failed > 0 ? 1 : 0);
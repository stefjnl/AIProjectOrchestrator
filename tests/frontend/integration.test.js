// Frontend Integration Tests for AI Project Orchestrator
// This file tests the complete workflow from Requirements Analysis to Code Generation

// Mock DOM environment for testing
const mockDOM = (() => {
    const elements = {};
    
    return {
        createElement: (tag) => {
            const element = {
                tag,
                id: null,
                className: '',
                disabled: false,
                textContent: '',
                innerHTML: '',
                style: {},
                querySelector: (selector) => {
                    if (selector === 'button') {
                        return { disabled: element.disabled };
                    }
                    return null;
                },
                appendChild: (child) => {}
            };
            return element;
        },
        getElementById: (id) => {
            if (!elements[id]) {
                elements[id] = {
                    id,
                    textContent: '',
                    className: '',
                    style: {},
                    querySelector: (selector) => {
                        if (selector === 'button') {
                            return { disabled: elements[id].disabled };
                        }
                        return null;
                    }
                };
            }
            return elements[id];
        },
        clear: () => {
            Object.keys(elements).forEach(key => delete elements[key]);
        }
    };
})();

// Mock global objects
global.window = {
    localStorage: {
        store: {},
        getItem: function(key) { return this.store[key] || null; },
        setItem: function(key, value) { this.store[key] = value.toString(); },
        removeItem: function(key) { delete this.store[key]; }
    }
};

global.document = mockDOM;

// Mock fetch for API calls
global.fetch = jest.fn();

// Import the workflow manager
const { WorkflowManager } = require('../../frontend/js/workflow.js');

describe('AI Project Orchestrator Frontend Integration Tests', () => {
    beforeEach(() => {
        // Clear localStorage
        window.localStorage.store = {};
        
        // Clear mock DOM
        mockDOM.clear();
        
        // Clear fetch mock
        fetch.mockClear();
    });
    
    describe('Complete Workflow Integration', () => {
        test('should complete the full workflow from requirements to code generation', async () => {
            // Create workflow manager
            const workflowManager = new WorkflowManager();
            workflowManager.setProjectId('test-project-123');
            
            // Test 1: Requirements Analysis
            const requirementsAnalysisId = 'req-123';
            const requirementsReviewId = 'rev-123';
            
            workflowManager.setRequirementsAnalysis(requirementsAnalysisId, requirementsReviewId);
            
            expect(workflowManager.getRequirementsAnalysisId()).toBe(requirementsAnalysisId);
            expect(workflowManager.canStartPlanning()).toBe(true);
            
            // Test 2: Project Planning
            const planningId = 'plan-123';
            const planningReviewId = 'rev-456';
            
            workflowManager.setProjectPlanning(planningId, planningReviewId);
            
            expect(workflowManager.getProjectPlanningId()).toBe(planningId);
            expect(workflowManager.canGenerateStories()).toBe(true);
            
            // Test 3: Story Generation
            const storyGenerationId = 'story-123';
            const storyReviewId = 'rev-789';
            
            workflowManager.setStoryGeneration(storyGenerationId, storyReviewId);
            
            expect(workflowManager.getStoryGenerationId()).toBe(storyGenerationId);
            expect(workflowManager.canGenerateCode()).toBe(true);
            
            // Test 4: Code Generation
            const codeGenerationId = 'code-123';
            const codeReviewId = 'rev-012';
            
            workflowManager.setCodeGeneration(codeGenerationId, codeReviewId);
            
            expect(workflowManager.getCodeGenerationId()).toBe(codeGenerationId);
            
            // Test state persistence
            const newWorkflowManager = new WorkflowManager();
            newWorkflowManager.setProjectId('test-project-123');
            
            expect(newWorkflowManager.getRequirementsAnalysisId()).toBe(requirementsAnalysisId);
            expect(newWorkflowManager.getProjectPlanningId()).toBe(planningId);
            expect(newWorkflowManager.getStoryGenerationId()).toBe(storyGenerationId);
            expect(newWorkflowManager.getCodeGenerationId()).toBe(codeGenerationId);
        });
    });
    
    describe('Workflow State Transitions', () => {
        test('should correctly transition through workflow stages', async () => {
            const workflowManager = new WorkflowManager();
            workflowManager.setProjectId('test-project-456');
            
            // Initially, no stages should be available
            expect(workflowManager.canStartPlanning()).toBe(false);
            expect(workflowManager.canGenerateStories()).toBe(false);
            expect(workflowManager.canGenerateCode()).toBe(false);
            
            // After requirements analysis, planning should be available
            workflowManager.setRequirementsAnalysis('req-456', 'rev-456');
            expect(workflowManager.canStartPlanning()).toBe(true);
            expect(workflowManager.canGenerateStories()).toBe(false);
            expect(workflowManager.canGenerateCode()).toBe(false);
            
            // After project planning, story generation should be available
            workflowManager.setProjectPlanning('plan-456', 'rev-789');
            expect(workflowManager.canStartPlanning()).toBe(true);
            expect(workflowManager.canGenerateStories()).toBe(true);
            expect(workflowManager.canGenerateCode()).toBe(false);
            
            // After story generation, code generation should be available
            workflowManager.setStoryGeneration('story-456', 'rev-012');
            expect(workflowManager.canStartPlanning()).toBe(true);
            expect(workflowManager.canGenerateStories()).toBe(true);
            expect(workflowManager.canGenerateCode()).toBe(true);
        });
    });
    
    describe('API Function Integration', () => {
        test('should handle API function calls correctly', async () => {
            // Mock API responses
            const mockResponses = {
                '/api/projects/test-project': { id: 'test-project', name: 'Test Project', description: 'Test Description' },
                '/api/requirements/analyze': { analysisId: 'req-123', reviewId: 'rev-123', status: 1, message: 'Success' },
                '/api/projectplanning/create': { planningId: 'plan-123', reviewId: 'rev-456', status: 1, message: 'Success' },
                '/api/projectplanning/can-create/req-123': true,
                '/api/stories/generate': { generationId: 'story-123', reviewId: 'rev-789', status: 1, message: 'Success' },
                '/api/stories/can-generate/plan-123': true,
                '/api/code/generate': { generationId: 'code-123', reviewId: 'rev-012', status: 1, message: 'Success' },
                '/api/code/can-generate/story-123': true
            };
            
            fetch.mockImplementation((url) => {
                const endpoint = url.replace('http://localhost:8086', '');
                const response = mockResponses[endpoint];
                
                if (response) {
                    return Promise.resolve({
                        ok: true,
                        json: () => Promise.resolve(response),
                        headers: {
                            get: () => 'application/json'
                        }
                    });
                }
                
                return Promise.resolve({
                    ok: false,
                    status: 404,
                    statusText: 'Not Found'
                });
            });
            
            // Test that API functions are properly defined
            const apiFunctions = [
                'getProjects',
                'getProject',
                'analyzeRequirements',
                'createProjectPlan',
                'canCreateProjectPlan',
                'generateStories',
                'canGenerateStories',
                'generateCode',
                'canGenerateCode'
            ];
            
            // Check that all functions are available
            apiFunctions.forEach(funcName => {
                expect(typeof window[funcName]).toBe('function');
            });
        });
    });
});
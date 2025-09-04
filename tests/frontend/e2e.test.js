// End-to-End Workflow Test for AI Project Orchestrator
// This test simulates a complete user journey through all workflow stages

// Mock browser environment
const jsdom = require('jsdom');
const { JSDOM } = jsdom;

// Create a mock DOM environment
const dom = new JSDOM(`<!DOCTYPE html>
<html>
<head>
    <title>Workflow Test</title>
</head>
<body>
    <div id="loading" style="display: none;">Loading...</div>
    <div id="error" style="display: none;"></div>
    <div id="project-details" style="display: none;">
        <h2 id="project-name"></h2>
        <p id="project-description"></p>
        <div class="workflow-stages">
            <div class="stage-card" id="requirements-stage">
                <h3>Requirements Analysis</h3>
                <div class="stage-status" id="requirements-status">Not Started</div>
                <button class="btn btn-small">Start Analysis</button>
            </div>
            <div class="stage-card" id="planning-stage">
                <h3>Project Planning</h3>
                <div class="stage-status" id="planning-status">Not Started</div>
                <button class="btn btn-small" disabled>Start Planning</button>
            </div>
            <div class="stage-card" id="stories-stage">
                <h3>Story Generation</h3>
                <div class="stage-status" id="stories-status">Not Started</div>
                <button class="btn btn-small" disabled>Generate Stories</button>
            </div>
            <div class="stage-card" id="code-stage">
                <h3>Code Generation</h3>
                <div class="stage-status" id="code-status">Not Started</div>
                <button class="btn btn-small" disabled>Generate Code</button>
            </div>
        </div>
    </div>
</body>
</html>`);

global.window = dom.window;
global.document = dom.window.document;
global.fetch = jest.fn();

// Mock localStorage
global.window.localStorage = {
    store: {},
    getItem: function(key) { return this.store[key] || null; },
    setItem: function(key, value) { this.store[key] = value.toString(); },
    removeItem: function(key) { delete this.store[key]; }
};

// Import workflow manager
const fs = require('fs');
const path = require('path');

// Read the workflow.js file
const workflowJsPath = path.join(__dirname, '../../frontend/js/workflow.js');
const workflowJsContent = fs.readFileSync(workflowJsPath, 'utf8');

// Evaluate the workflow.js content to make the WorkflowManager available
eval(workflowJsContent);

describe('End-to-End Workflow Test', () => {
    beforeEach(() => {
        // Clear localStorage
        window.localStorage.store = {};
        
        // Reset fetch mock
        fetch.mockClear();
        
        // Reset DOM elements
        document.getElementById('loading').style.display = 'none';
        document.getElementById('error').style.display = 'none';
        document.getElementById('project-details').style.display = 'none';
        document.getElementById('requirements-status').textContent = 'Not Started';
        document.getElementById('planning-status').textContent = 'Not Started';
        document.getElementById('stories-status').textContent = 'Not Started';
        document.getElementById('code-status').textContent = 'Not Started';
        document.getElementById('planning-stage').querySelector('button').disabled = true;
        document.getElementById('stories-stage').querySelector('button').disabled = true;
        document.getElementById('code-stage').querySelector('button').disabled = true;
    });
    
    test('should complete full workflow from requirements to code generation', async () => {
        // Mock API responses
        const mockResponses = {
            '/api/projects/test-project-123': { 
                id: 'test-project-123', 
                name: 'Test Project', 
                description: 'Test project description' 
            },
            '/api/requirements/analyze': { 
                analysisId: 'req-123', 
                reviewId: 'rev-123', 
                status: 1, 
                message: 'Success' 
            },
            '/api/projectplanning/create': { 
                planningId: 'plan-123', 
                reviewId: 'rev-456', 
                status: 1, 
                message: 'Success' 
            },
            '/api/projectplanning/can-create/req-123': true,
            '/api/stories/generate': { 
                generationId: 'story-123', 
                reviewId: 'rev-789', 
                status: 1, 
                message: 'Success' 
            },
            '/api/stories/can-generate/plan-123': true,
            '/api/code/generate': { 
                generationId: 'code-123', 
                reviewId: 'rev-012', 
                status: 1, 
                message: 'Success' 
            },
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
        
        // Initialize workflow manager
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project-123');
        
        // Step 1: Requirements Analysis
        expect(workflowManager.canStartPlanning()).toBe(false);
        
        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        workflowManager.updateWorkflowUI();
        
        expect(workflowManager.canStartPlanning()).toBe(true);
        expect(document.getElementById('requirements-status').textContent).toBe('Pending Review');
        expect(document.getElementById('planning-stage').querySelector('button').disabled).toBe(false);
        
        // Step 2: Project Planning
        workflowManager.setProjectPlanning('plan-123', 'rev-456');
        workflowManager.updateWorkflowUI();
        
        expect(workflowManager.canGenerateStories()).toBe(true);
        expect(document.getElementById('planning-status').textContent).toBe('Pending Review');
        expect(document.getElementById('stories-stage').querySelector('button').disabled).toBe(false);
        
        // Step 3: Story Generation
        workflowManager.setStoryGeneration('story-123', 'rev-789');
        workflowManager.updateWorkflowUI();
        
        expect(workflowManager.canGenerateCode()).toBe(true);
        expect(document.getElementById('stories-status').textContent).toBe('Pending Review');
        expect(document.getElementById('code-stage').querySelector('button').disabled).toBe(false);
        
        // Step 4: Code Generation
        workflowManager.setCodeGeneration('code-123', 'rev-012');
        workflowManager.updateWorkflowUI();
        
        expect(document.getElementById('code-status').textContent).toBe('Pending Review');
        
        // Verify state persistence
        const newWorkflowManager = new WorkflowManager();
        newWorkflowManager.setProjectId('test-project-123');
        
        expect(newWorkflowManager.getRequirementsAnalysisId()).toBe('req-123');
        expect(newWorkflowManager.getProjectPlanningId()).toBe('plan-123');
        expect(newWorkflowManager.getStoryGenerationId()).toBe('story-123');
        expect(newWorkflowManager.getCodeGenerationId()).toBe('code-123');
    });
});
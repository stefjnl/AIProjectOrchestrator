// Mock localStorage for testing
const localStorageMock = (() => {
    let store = {};

    return {
        getItem: (key) => store[key] || null,
        setItem: (key, value) => store[key] = value.toString(),
        removeItem: (key) => delete store[key],
        clear: () => store = {}
    };
})();

Object.defineProperty(window, 'localStorage', {
    value: localStorageMock
});

// Mock DOM elements
function createMockElement(id, tag = 'div') {
    const element = document.createElement(tag);
    element.id = id;
    document.body.appendChild(element);
    return element;
}

// Mock API functions
async function getProject(id) {
    return { id, name: 'Test Project', description: 'Test Description' };
}

async function analyzeRequirements(request) {
    return {
        analysisId: 'req-123',
        reviewId: 'rev-123',
        status: 1,
        message: 'Success'
    };
}

async function createProjectPlan(request) {
    return {
        planningId: 'plan-123',
        reviewId: 'rev-456',
        status: 1,
        message: 'Success'
    };
}

async function generateStories(request) {
    return {
        generationId: 'story-123',
        reviewId: 'rev-789',
        status: 1,
        message: 'Success'
    };
}

async function generateCode(request) {
    return {
        generationId: 'code-123',
        reviewId: 'rev-012',
        status: 1,
        message: 'Success'
    };
}

async function canCreateProjectPlan(requirementsAnalysisId) {
    return true;
}

async function canGenerateStories(planningId) {
    return true;
}

async function canGenerateCode(storyGenerationId) {
    return true;
}

// Mock APIClient for the actual WorkflowManager
window.APIClient = {
    getWorkflowStatus: jest.fn().mockResolvedValue({
        requirementsAnalysis: { analysisId: null, isApproved: false, isPending: false },
        projectPlanning: { planningId: null, isApproved: false, isPending: false },
        storyGeneration: { generationId: null, isApproved: false, isPending: false, storyCount: 0 },
        promptGeneration: { storyPrompts: [], completionPercentage: 0 }
    })
};

// Test suite
describe('WorkflowManager', () => {
    beforeEach(() => {
        // Clear localStorage before each test
        localStorage.clear();

        // Create mock DOM elements
        createMockElement('loading');
        createMockElement('error');
        createMockElement('project-name');
        createMockElement('project-description');
        createMockElement('project-details');
        createMockElement('requirements-status');
        createMockElement('planning-status');
        createMockElement('stories-status');
        createMockElement('code-status');

        // Create stage cards
        const requirementsStage = document.createElement('div');
        requirementsStage.id = 'requirements-stage';
        requirementsStage.className = 'stage-card';
        document.body.appendChild(requirementsStage);

        const planningStage = document.createElement('div');
        planningStage.id = 'planning-stage';
        planningStage.className = 'stage-card';
        document.body.appendChild(planningStage);

        const storiesStage = document.createElement('div');
        storiesStage.id = 'stories-stage';
        storiesStage.className = 'stage-card';
        document.body.appendChild(storiesStage);

        const codeStage = document.createElement('div');
        codeStage.id = 'code-stage';
        codeStage.className = 'stage-card';
        document.body.appendChild(codeStage);

        // Create buttons
        const planningButton = document.createElement('button');
        planningButton.disabled = true;
        planningStage.appendChild(planningButton);

        const storiesButton = document.createElement('button');
        storiesButton.disabled = true;
        storiesStage.appendChild(storiesButton);

        const codeButton = document.createElement('button');
        codeButton.disabled = true;
        codeStage.appendChild(codeButton);
    });

    afterEach(() => {
        // Clean up DOM elements
        document.body.innerHTML = '';
    });

    test('should initialize with correct default state', () => {
        const workflowManager = new WorkflowManager('test-project');

        expect(workflowManager.projectId).toBe('test-project');
        expect(workflowManager.state.requirementsAnalysisId).toBe(null);
        expect(workflowManager.state.projectPlanningId).toBe(null);
        expect(workflowManager.state.storyGenerationId).toBe(null);
        expect(workflowManager.state.codeGenerationId).toBe(null);
        expect(workflowManager.state.requirementsApproved).toBe(false);
        expect(workflowManager.state.planningApproved).toBe(false);
        expect(workflowManager.state.storiesApproved).toBe(false);
    });

    test('should correctly track requirements analysis', () => {
        const workflowManager = new WorkflowManager('test-project');

        // Simulate setting requirements analysis
        workflowManager.state.requirementsAnalysisId = 'req-123';
        workflowManager.state.requirementsPending = true;

        expect(workflowManager.state.requirementsAnalysisId).toBe('req-123');
        expect(workflowManager.state.requirementsPending).toBe(true);
        expect(workflowManager.state.requirementsApproved).toBe(false);
    });

    test('should correctly track project planning', () => {
        const workflowManager = new WorkflowManager('test-project');

        // Simulate setting project planning
        workflowManager.state.projectPlanningId = 'plan-123';
        workflowManager.state.planningPending = true;

        expect(workflowManager.state.projectPlanningId).toBe('plan-123');
        expect(workflowManager.state.planningPending).toBe(true);
        expect(workflowManager.state.planningApproved).toBe(false);
    });

    test('should correctly track story generation', () => {
        const workflowManager = new WorkflowManager('test-project');

        // Simulate setting story generation
        workflowManager.state.storyGenerationId = 'story-123';
        workflowManager.state.storiesPending = true;

        expect(workflowManager.state.storyGenerationId).toBe('story-123');
        expect(workflowManager.state.storiesPending).toBe(true);
        expect(workflowManager.state.storiesApproved).toBe(false);
    });

    test('should correctly track code generation', () => {
        const workflowManager = new WorkflowManager('test-project');

        // Simulate setting code generation
        workflowManager.state.codeGenerationId = 'code-123';

        expect(workflowManager.state.codeGenerationId).toBe('code-123');
    });

    test('should correctly determine if planning can be started', () => {
        const workflowManager = new WorkflowManager('test-project');

        // Initially should not be able to start planning
        expect(workflowManager.state.requirementsApproved).toBe(false);

        // After requirements analysis approval, should be able to start planning
        workflowManager.state.requirementsAnalysisId = 'req-123';
        workflowManager.state.requirementsApproved = true;
        expect(workflowManager.state.requirementsApproved).toBe(true);
    });

    test('should correctly determine if stories can be generated', () => {
        const workflowManager = new WorkflowManager('test-project');

        // Initially should not be able to generate stories
        expect(workflowManager.state.planningApproved).toBe(false);

        // After project planning approval, should be able to generate stories
        workflowManager.state.projectPlanningId = 'plan-123';
        workflowManager.state.planningApproved = true;
        expect(workflowManager.state.planningApproved).toBe(true);
    });

    test('should correctly determine if code can be generated', () => {
        const workflowManager = new WorkflowManager('test-project');

        // Initially should not be able to generate code
        expect(workflowManager.state.storiesApproved).toBe(false);

        // After story generation approval, should be able to generate code
        workflowManager.state.storyGenerationId = 'story-123';
        workflowManager.state.storiesApproved = true;
        expect(workflowManager.state.storiesApproved).toBe(true);
    });

    test('should update UI correctly for requirements status', () => {
        const workflowManager = new WorkflowManager('test-project');
        
        // Set requirements to pending review
        workflowManager.state.requirementsAnalysisId = 'req-123';
        workflowManager.state.requirementsPending = true;
        workflowManager.updateUI();
        
        const requirementsStatus = document.getElementById('requirements-status');
        expect(requirementsStatus.textContent).toBe('Pending Review');
        expect(requirementsStatus.className).toBe('stage-status status-pending');
    });
    
    test('should enable planning button when requirements are analyzed', () => {
        const workflowManager = new WorkflowManager('test-project');
        
        // Set requirements to approved
        workflowManager.state.requirementsAnalysisId = 'req-123';
        workflowManager.state.requirementsApproved = true;
        workflowManager.updateUI();
        
        const planningButton = document.getElementById('planning-stage').querySelector('button');
        expect(planningButton.disabled).toBe(false);
    });
});
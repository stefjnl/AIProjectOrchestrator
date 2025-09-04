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
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        expect(workflowManager.projectId).toBe('test-project');
        expect(workflowManager.workflowState.requirements.status).toBe('not_started');
        expect(workflowManager.workflowState.planning.status).toBe('not_started');
        expect(workflowManager.workflowState.stories.status).toBe('not_started');
        expect(workflowManager.workflowState.code.status).toBe('not_started');
    });
    
    test('should save and load state from localStorage', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        // Set some state
        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        workflowManager.setProjectPlanning('plan-123', 'rev-456');
        
        // Create a new workflow manager to test loading
        const newWorkflowManager = new WorkflowManager();
        newWorkflowManager.setProjectId('test-project');
        
        expect(newWorkflowManager.getRequirementsAnalysisId()).toBe('req-123');
        expect(newWorkflowManager.getProjectPlanningId()).toBe('plan-123');
    });
    
    test('should correctly track requirements analysis', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        
        expect(workflowManager.getRequirementsAnalysisId()).toBe('req-123');
        expect(workflowManager.workflowState.requirements.status).toBe('pending_review');
        expect(workflowManager.workflowState.requirements.reviewId).toBe('rev-123');
    });
    
    test('should correctly track project planning', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        workflowManager.setProjectPlanning('plan-123', 'rev-456');
        
        expect(workflowManager.getProjectPlanningId()).toBe('plan-123');
        expect(workflowManager.workflowState.planning.status).toBe('pending_review');
        expect(workflowManager.workflowState.planning.reviewId).toBe('rev-456');
    });
    
    test('should correctly track story generation', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        workflowManager.setStoryGeneration('story-123', 'rev-789');
        
        expect(workflowManager.getStoryGenerationId()).toBe('story-123');
        expect(workflowManager.workflowState.stories.status).toBe('pending_review');
        expect(workflowManager.workflowState.stories.reviewId).toBe('rev-789');
    });
    
    test('should correctly track code generation', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        workflowManager.setCodeGeneration('code-123', 'rev-012');
        
        expect(workflowManager.getCodeGenerationId()).toBe('code-123');
        expect(workflowManager.workflowState.code.status).toBe('pending_review');
        expect(workflowManager.workflowState.code.reviewId).toBe('rev-012');
    });
    
    test('should correctly determine if planning can be started', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        // Initially should not be able to start planning
        expect(workflowManager.canStartPlanning()).toBe(false);
        
        // After requirements analysis, should be able to start planning
        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        expect(workflowManager.canStartPlanning()).toBe(true);
    });
    
    test('should correctly determine if stories can be generated', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        // Initially should not be able to generate stories
        expect(workflowManager.canGenerateStories()).toBe(false);
        
        // After project planning, should be able to generate stories
        workflowManager.setProjectPlanning('plan-123', 'rev-456');
        expect(workflowManager.canGenerateStories()).toBe(true);
    });
    
    test('should correctly determine if code can be generated', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        // Initially should not be able to generate code
        expect(workflowManager.canGenerateCode()).toBe(false);
        
        // After story generation, should be able to generate code
        workflowManager.setStoryGeneration('story-123', 'rev-789');
        expect(workflowManager.canGenerateCode()).toBe(true);
    });
    
    test('should update UI correctly for requirements status', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        // Set requirements to pending review
        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        workflowManager.updateWorkflowUI();
        
        const requirementsStatus = document.getElementById('requirements-status');
        expect(requirementsStatus.textContent).toBe('Pending Review');
        expect(requirementsStatus.className).toBe('stage-status status-pending');
    });
    
    test('should enable planning button when requirements are analyzed', () => {
        const workflowManager = new WorkflowManager();
        workflowManager.setProjectId('test-project');
        
        // Set requirements to pending review
        workflowManager.setRequirementsAnalysis('req-123', 'rev-123');
        workflowManager.updateWorkflowUI();
        
        const planningButton = document.getElementById('planning-stage').querySelector('button');
        expect(planningButton.disabled).toBe(false);
    });
});
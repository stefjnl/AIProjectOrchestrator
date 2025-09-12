// Clean Workflow Orchestrator - Delegates all functionality to services
class WorkflowManager {
    constructor(projectId, isNewProject = false) {
        this.projectId = projectId;
        this.isNewProject = isNewProject;
        this.stages = ['requirements', 'planning', 'stories', 'prompts', 'review'];
        this.isAutoRefreshing = false;
        this.autoRefreshInterval = null;
        this.workflowState = null; // Add workflowState property

        console.log(`WorkflowManager initialized for project ${projectId}, newProject=${isNewProject}`);

        this.initializeServices().then(() => {
            this.initialize();
        }).catch(error => {
            console.error('Failed to initialize services:', error);
            this.initializeFallbackServices();
            this.initialize();
        });
    }

    async initializeServices() {
        try {
            console.log('Initializing services...');
            console.log('StateManagementService available:', typeof StateManagementService !== 'undefined');
            console.log('WorkflowContentService available:', typeof WorkflowContentService !== 'undefined');
            console.log('EventHandlerService available:', typeof EventHandlerService !== 'undefined');
            console.log('StageInitializationService available:', typeof StageInitializationService !== 'undefined');
            console.log('APIClient available:', typeof APIClient !== 'undefined');

            // Check if APIClient is available and working
            let apiClientAvailable = false;
            if (typeof APIClient !== 'undefined') {
                try {
                    // Test APIClient with a simple health check
                    console.log('Testing APIClient availability...');
                    apiClientAvailable = true;
                } catch (apiError) {
                    console.warn('APIClient test failed:', apiError);
                    apiClientAvailable = false;
                }
            }

            // Initialize services with proper dependencies
            this.stateManager = typeof StateManagementService !== 'undefined' ?
                new StateManagementService(this) : new InlineStateManagementService(this);

            // Use the new modular WorkflowContentServiceBundle
            if (typeof WorkflowContentServiceBundle !== 'undefined' && apiClientAvailable) {
                this.contentService = await this.initializeModularContentService();
            } else {
                this.contentService = new InlineWorkflowContentService(this);
            }

            this.stageInitializer = typeof StageInitializationService !== 'undefined' ?
                new StageInitializationService(this) : new InlineStageInitializationService(this);

            this.eventHandler = typeof EventHandlerService !== 'undefined' ?
                new EventHandlerService(this.stageInitializer, this) : new InlineEventHandlerService(this);

            console.log('Services initialized successfully');
            console.log('StateManager:', this.stateManager.constructor.name);
            console.log('ContentService:', this.contentService.constructor.name);
            console.log('EventHandler:', this.eventHandler.constructor.name);
            console.log('StageInitializer:', this.stageInitializer.constructor.name);
            console.log('APIClient Available:', apiClientAvailable);
        } catch (error) {
            console.warn('Failed to initialize external services, using inline implementations:', error);
            this.initializeFallbackServices();
        }
    }

    /**
     * Initialize the new modular WorkflowContentService
     * @returns {WorkflowContentService} The initialized content service
     */
    async initializeModularContentService() {
        try {
            console.log('🚀 Initializing modular WorkflowContentService...');

            // Create and initialize the service bundle
            const serviceBundle = new WorkflowContentServiceBundle();

            await serviceBundle.initialize({
                workflowManager: this,
                apiClient: APIClient
            });

            // Get the main service from the bundle
            const contentService = serviceBundle.getWorkflowContentService();

            console.log('✅ Modular WorkflowContentService initialized successfully');
            console.log('Service health:', contentService.getHealthStatus());

            return contentService;

        } catch (error) {
            console.error('❌ Failed to initialize modular WorkflowContentService:', error);
            console.log('🔄 Falling back to inline implementation');
            return new InlineWorkflowContentService(this);
        }
    }

    initializeFallbackServices() {
        this.stateManager = new InlineStateManagementService(this);
        this.contentService = new InlineWorkflowContentService(this);
        this.eventHandler = new InlineEventHandlerService(this);
        this.stageInitializer = new InlineStageInitializationService(this);
    }

    async initialize() {
        try {
            console.log('Initializing workflow orchestrator...');

            // Set up state management
            this.stateManager.setNewProjectFlag(this.isNewProject);

            // Set up event listeners
            this.eventHandler.setupEventListeners();

            // Start auto-refresh
            this.startAutoRefresh();

            // Load initial data
            await this.loadInitialData();

            // Update stage indicators after data is loaded
            if (this.stateManager) {
                this.stateManager.updateStageIndicators();
            }

            console.log('Workflow orchestrator initialized successfully');
        } catch (error) {
            console.error('Failed to initialize workflow orchestrator:', error);
            this.showInitializationError(error);
        }
    }

    async loadInitialData() {
        try {
            console.log('Loading initial workflow data...');

            // Load project data with better error handling
            let project = null;
            try {
                project = await APIClient.getProject(this.projectId);
                if (project) {
                    this.stateManager.setProjectData(project);
                    console.log('Project data loaded successfully');
                }
            } catch (apiError) {
                console.warn('Failed to load project data:', apiError);
                // Continue with fallback - don't fail completely
            }

            // Load workflow state (this also sets this.workflowState)
            const workflowState = await this.loadWorkflowState();

            // Determine current stage
            const currentStage = this.stateManager.getCurrentStageFromWorkflow();
            this.stateManager.setCurrentStage(currentStage);

            // Load stage content
            await this.loadStageContent(currentStage);

            // Handle new project scenario
            if (this.isNewProject) {
                this.stateManager.setNewProjectPromptShown(true);
                this.handleNewProjectScenario();
            }

            console.log(`Initial data loaded. Current stage: ${currentStage}`);
        } catch (error) {
            console.error('Failed to load initial data:', error);
            this.showInitializationError(error);
        }
    }

    // Add missing methods that services expect
    async loadProjectData() {
        try {
            console.log(`Loading project data for project ${this.projectId}...`);
            const project = await APIClient.getProject(this.projectId);
            this.stateManager.setProjectData(project);
            console.log('Project data loaded successfully');
            return project;
        } catch (error) {
            console.warn('Could not load project data:', error);
            // Provide more detailed error information
            if (error.message && error.message.includes('NetworkError')) {
                console.error('Network error detected - API may be unavailable');
            }
            return null;
        }
    }

    async loadCurrentStage() {
        try {
            const currentStage = this.stateManager.getCurrentStageFromWorkflow();
            await this.loadStageContent(currentStage);
            return currentStage;
        } catch (error) {
            console.warn('Could not load current stage:', error);
            return 1;
        }
    }

    async loadWorkflowState() {
        try {
            console.log(`Loading workflow state for project ${this.projectId}...`);
            const workflowState = await APIClient.getWorkflowStatus(this.projectId);
            this.workflowState = workflowState; // Store in workflowManager for service access
            this.stateManager.updateWorkflowState(workflowState);
            console.log('Workflow state loaded and synchronized:', workflowState);
            return workflowState;
        } catch (error) {
            console.warn('Could not load workflow state, using defaults:', error);
            // Check if it's a network error
            if (error.message && (error.message.includes('NetworkError') || error.message.includes('Failed to fetch'))) {
                console.error('Network error detected - using fallback workflow state');
            }
            const defaultState = this.stateManager.getDefaultWorkflowState();
            this.workflowState = defaultState;
            return defaultState;
        }
    }

    async loadStageContent(stage) {
        try {
            console.log(`Loading stage ${stage} content...`);

            // Validate stage access
            if (!this.stateManager.canAccessStage(stage)) {
                const accessibleStage = this.stateManager.getHighestAccessibleStage();
                if (accessibleStage !== stage) {
                    return this.loadStageContent(accessibleStage);
                }
            }

            // Update navigation state
            this.stateManager.setCurrentStage(stage);

            // Get stage content from service
            const content = await this.contentService.getStageContent(stage);

            // Update UI
            const stageContentElement = document.getElementById('stage-content');
            if (stageContentElement) {
                stageContentElement.innerHTML = content;
            }

            // Initialize stage functionality
            this.stageInitializer.initializeStage(stage);

            console.log(`Stage ${stage} content loaded successfully`);
        } catch (error) {
            console.error(`Failed to load stage ${stage} content:`, error);
            this.showStageError(stage, error);
        }
    }

    // Navigation methods
    async navigateStage(direction) {
        try {
            const currentStage = this.stateManager.getCurrentStage();
            const newStage = currentStage + direction;

            if (newStage < 1 || newStage > 5) return;

            // Validate progression
            if (direction > 0 && !this.stateManager.canProgressToNextStage()) {
                window.App.showNotification('Complete the current stage before proceeding.', 'warning');
                return;
            }

            await this.loadStageContent(newStage);
        } catch (error) {
            console.error('Navigation error:', error);
            window.App.showNotification('Navigation failed.', 'error');
        }
    }

    async jumpToStage(stage) {
        if (stage >= 1 && stage <= 5) {
            await this.loadStageContent(stage);
        }
    }

    // Action methods - delegate to services
    async analyzeRequirements() {
        return this.contentService.analyzeRequirements();
    }

    async generatePlan() {
        return this.contentService.generatePlan();
    }

    async regeneratePlan() {
        return this.contentService.regeneratePlan();
    }

    async generateStories() {
        return this.contentService.generateStories();
    }

    async regenerateStories() {
        return this.contentService.regenerateStories();
    }

    async generateAllPrompts() {
        return this.contentService.generateAllPrompts();
    }

    async completeProject() {
        return this.contentService.completeProject();
    }

    // Auto-refresh functionality
    startAutoRefresh() {
        if (this.isAutoRefreshing) return;

        this.isAutoRefreshing = true;
        this.autoRefreshInterval = setInterval(() => {
            this.refreshWorkflowStatus();
        }, 10000);

        this.stateManager.setAutoRefreshState(true, this.autoRefreshInterval);
        console.log('Auto-refresh started');
    }

    stopAutoRefresh() {
        if (this.autoRefreshInterval) {
            clearInterval(this.autoRefreshInterval);
            this.autoRefreshInterval = null;
            this.isAutoRefreshing = false;

            this.stateManager.setAutoRefreshState(false);
            console.log('Auto-refresh stopped');
        }
    }

    async refreshWorkflowStatus() {
        try {
            const previousState = this.stateManager.getWorkflowState();
            const currentState = await this.loadWorkflowState();

            // Check for state changes and auto-progress
            await this.checkForAutoProgress(previousState, currentState);

        } catch (error) {
            console.warn('Failed to refresh workflow status:', error);
        }
    }

    async checkForAutoProgress(previousState, currentState) {
        // Auto-progress logic based on state changes
        if (!previousState.requirementsAnalysis?.isApproved && currentState.requirementsAnalysis?.isApproved) {
            await this.loadStageContent(2);
            window.App.showNotification('Requirements approved! Moving to Project Planning.', 'success');
            return;
        }

        if (!previousState.projectPlanning?.isApproved && currentState.projectPlanning?.isApproved) {
            await this.loadStageContent(3);
            window.App.showNotification('Project planning approved! Moving to User Stories.', 'success');
            return;
        }

        if (!previousState.storyGeneration?.isApproved && currentState.storyGeneration?.isApproved) {
            const generationId = currentState.storyGeneration?.generationId;
            if (generationId) {
                window.location.href = `/Stories/Overview?generationId=${generationId}&projectId=${this.projectId}`;
            }
            return;
        }

        const prevCompletion = previousState.promptGeneration?.completionPercentage || 0;
        const currCompletion = currentState.promptGeneration?.completionPercentage || 0;

        if (prevCompletion < 100 && currCompletion >= 100) {
            await this.loadStageContent(5);
            window.App.showNotification('Prompt generation completed! Moving to Final Review.', 'success');
        }
    }

    // New project handling
    handleNewProjectScenario() {
        const state = this.stateManager.getState();

        if (!state.workflow.requirementsAnalysis?.isApproved &&
            state.workflow.requirementsAnalysis?.status === 'NotStarted') {

            setTimeout(() => {
                if (confirm('Welcome to your new project! Would you like to start with requirements analysis?')) {
                    this.analyzeRequirements();
                }
            }, 1500);
        }
    }

    // Error handling
    showInitializationError(error) {
        console.error('Workflow initialization failed:', error);
        window.App.showNotification('Failed to initialize workflow. Please refresh the page.', 'error');

        const stageContent = document.getElementById('stage-content');
        if (stageContent) {
            stageContent.innerHTML = this.getFallbackContent();
        }
    }

    showStageError(stage, error) {
        console.error(`Stage ${stage} loading error:`, error);
        window.App.showNotification(`Failed to load stage ${stage}. Please try again.`, 'error');

        const stageContent = document.getElementById('stage-content');
        if (stageContent) {
            stageContent.innerHTML = this.getStageErrorContent(stage, error);
        }
    }

    getFallbackContent() {
        return `
            <div class="stage-container">
                <h2>Workflow Initialization Failed</h2>
                <div class="empty-stage">
                    <div class="empty-icon">⚠️</div>
                    <h3>Unable to Load Workflow</h3>
                    <p>We encountered an error loading your workflow. Please try refreshing the page.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="location.reload()">
                            🔄 Refresh Page
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    getStageErrorContent(stage, error) {
        return `
            <div class="stage-container">
                <h2>Stage ${stage} Error</h2>
                <div class="empty-stage">
                    <div class="empty-icon">⚠️</div>
                    <h3>Failed to Load Stage Content</h3>
                    <p>Unable to load stage ${stage} content. Please try again.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="workflowManager.loadStageContent(${stage})">
                            🔄 Retry
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    // Utility methods
    viewRequirementsReview() {
        const reviewId = this.stateManager.getWorkflowState().requirementsAnalysis?.reviewId;
        window.location.href = reviewId ? `/Reviews/Queue?reviewId=${reviewId}` : '/Reviews/Queue';
    }

    editRequirements() {
        window.App.showNotification('Edit requirements functionality coming soon', 'info');
    }

    editPlanning() {
        window.App.showNotification('Edit planning functionality coming soon', 'info');
    }

    addCustomStory() {
        window.App.showNotification('Add custom story functionality coming soon', 'info');
    }

    viewStory(storyId) {
        window.App.showNotification(`View story ${storyId} functionality coming soon`, 'info');
    }

    approveStory(storyId) {
        window.App.showNotification(`Approve story ${storyId} functionality coming soon`, 'info');
    }

    rejectStory(storyId) {
        window.App.showNotification(`Reject story ${storyId} functionality coming soon`, 'info');
    }

    viewPrompt(promptId) {
        window.App.showNotification(`View prompt ${promptId} functionality coming soon`, 'info');
    }

    copyPrompt(promptId) {
        window.App.showNotification(`Copy prompt ${promptId} functionality coming soon`, 'info');
    }

    navigateToStoriesOverview() {
        const generationId = this.stateManager.getWorkflowState().storyGeneration?.generationId;
        if (generationId) {
            window.location.href = `/Stories/Overview?generationId=${generationId}&projectId=${this.projectId}`;
        } else {
            window.App.showNotification('No story generation ID found.', 'error');
        }
    }

    navigateToStage5() {
        this.jumpToStage(5);
    }

    exportPrompts() {
        window.App.showNotification('Export prompts functionality coming soon', 'info');
    }

    regeneratePrompts() {
        if (confirm('Are you sure you want to regenerate all prompts?')) {
            this.generateAllPrompts();
        }
    }

    generateReport() {
        window.App.showNotification('Generate report functionality coming soon', 'info');
    }

    exportProject() {
        window.App.showNotification('Export project functionality coming soon', 'info');
    }
}

// Initialize workflow manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    console.log('=== WORKFLOW ORCHESTRATOR INITIALIZATION ===');

    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('projectId');
    const newProject = urlParams.get('newProject') === 'true';

    console.log('Parsed URL parameters:', { projectId, newProject });

    if (projectId) {
        try {
            window.workflowManager = new WorkflowManager(projectId, newProject);
            console.log('Workflow orchestrator created successfully');
        } catch (error) {
            console.error('Failed to create WorkflowManager:', error);
            showFallbackContent();
        }
    } else {
        console.error('No project ID found for workflow initialization');
        window.App.showNotification('No project ID found', 'error');
        showFallbackContent();
    }

    console.log('=== WORKFLOW ORCHESTRATOR INITIALIZATION COMPLETED ===');
});

// Fallback function for initialization failures
function showFallbackContent() {
    console.log('Showing fallback content due to workflow initialization failure');
    const stageContent = document.getElementById('stage-content');
    if (stageContent) {
        stageContent.innerHTML = `
            <div class="stage-container">
                <h2>Workflow Loading Issue</h2>
                <div class="empty-stage">
                    <div class="empty-icon">⚠️</div>
                    <h3>Workflow Loading Issue</h3>
                    <p>There was an issue loading the workflow. Please try refreshing the page.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="location.reload()">
                            🔄 Refresh Page
                        </button>
                    </div>
                </div>
            </div>
        `;
    }
}

// Inline fallback implementations (minimal implementations for basic functionality)
class InlineWorkflowContentService {
    constructor(workflowManager) {
        this.workflowManager = workflowManager;
        console.log('InlineWorkflowContentService initialized');
    }

    async getStageContent(stage) {
        // Basic fallback - just return a simple message
        return `
            <div class="stage-container">
                <h2>Stage ${stage}</h2>
                <div class="empty-stage">
                    <div class="empty-icon">⚠️</div>
                    <h3>Service Unavailable</h3>
                    <p>WorkflowContentService is not available. Please check the console for errors.</p>
                </div>
            </div>
        `;
    }

    async analyzeRequirements() {
        window.App.showNotification('Requirements analysis service unavailable', 'error');
    }

    async generatePlan() {
        window.App.showNotification('Project planning service unavailable', 'error');
    }

    async regeneratePlan() {
        window.App.showNotification('Project planning service unavailable', 'error');
    }

    async generateStories() {
        window.App.showNotification('Story generation service unavailable', 'error');
    }

    async regenerateStories() {
        window.App.showNotification('Story generation service unavailable', 'error');
    }

    async generateAllPrompts() {
        window.App.showNotification('Prompt generation service unavailable', 'error');
    }

    async completeProject() {
        window.App.showNotification('Project completion service unavailable', 'error');
    }
}

class InlineStateManagementService {
    constructor(workflowManager) {
        this.workflowManager = workflowManager;
        this.state = this.getDefaultState();
        console.log('InlineStateManagementService initialized');
    }

    getDefaultState() {
        return {
            workflow: {
                projectId: this.workflowManager.projectId,
                requirementsAnalysis: { status: 'NotStarted', isApproved: false },
                projectPlanning: { status: 'NotStarted', isApproved: false },
                storyGeneration: { status: 'NotStarted', isApproved: false },
                promptGeneration: { status: 'NotStarted', isApproved: false, completionPercentage: 0 }
            },
            navigation: { currentStage: 1, isAutoRefreshing: false },
            project: { data: null },
            ui: { isNewProject: false, hasShownNewProjectPrompt: false, loadingState: 'idle' }
        };
    }

    getState() { return JSON.parse(JSON.stringify(this.state)); }
    getWorkflowState() { return JSON.parse(JSON.stringify(this.state.workflow)); }
    getDefaultWorkflowState() { return this.getDefaultState().workflow; }

    setNewProjectFlag(isNew) { this.state.ui.isNewProject = isNew; }
    setNewProjectPromptShown(shown) { this.state.ui.hasShownNewProjectPrompt = shown; }
    setCurrentStage(stage) { this.state.navigation.currentStage = stage; }
    getCurrentStage() { return this.state.navigation.currentStage; }
    getCurrentStageFromWorkflow() { return 1; }

    updateWorkflowState(updates) { this.state.workflow = { ...this.state.workflow, ...updates }; }
    setProjectData(data) { this.state.project.data = data; }

    canAccessStage(stage) { return stage === 1; }
    canProgressToNextStage() { return false; }
    getHighestAccessibleStage() { return 1; }
    calculateProgress() { return 0; }

    setAutoRefreshState(isRefreshing) { this.state.navigation.isAutoRefreshing = isRefreshing; }
}

class InlineEventHandlerService {
    constructor(workflowManager) {
        this.workflowManager = workflowManager;
        console.log('InlineEventHandlerService initialized');
    }

    setupEventListeners() {
        // Minimal event listeners for basic navigation
        document.getElementById('prev-stage')?.addEventListener('click', () => {
            this.workflowManager.navigateStage(-1);
        });

        document.getElementById('next-stage')?.addEventListener('click', () => {
            this.workflowManager.navigateStage(1);
        });
    }
}

class InlineStageInitializationService {
    constructor(workflowManager) {
        this.workflowManager = workflowManager;
        console.log('InlineStageInitializationService initialized');
    }

    initializeStage(stage) {
        console.log(`Stage ${stage} initialized (minimal implementation)`);
    }
}

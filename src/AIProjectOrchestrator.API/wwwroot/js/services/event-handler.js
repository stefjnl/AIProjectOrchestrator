/**
 * EventHandlerService - Handles all event-related functionality for the workflow manager
 * Following Single Responsibility Principle by separating event handling from business logic
 */
class EventHandlerService {
    /**
     * Initialize the EventHandlerService with dependencies
     * @param {object} stageInitializationService - Service for stage-specific initialization
     * @param {object} workflowManager - Reference to the main workflow manager
     */
    constructor(stageInitializationService, workflowManager) {
        if (!stageInitializationService) {
            throw new Error('StageInitializationService is required');
        }

        this.stageInitializationService = stageInitializationService;
        this.workflowManager = workflowManager;
        this.isInitialized = false;
    }

    /**
     * Initialize the workflow manager and set up all event listeners
     * @param {string} projectId - The project ID
     * @param {boolean} isNewProject - Whether this is a new project
     */
    initialize(projectId, isNewProject = false) {
        try {
            console.log('🔄 EventHandlerService: Initializing workflow manager');
            console.log('EventHandlerService.initialize - projectId:', projectId, 'isNewProject:', isNewProject);

            // Store project information
            this.projectId = projectId;
            this.isNewProject = isNewProject;

            // Ensure workflowManager has the projectId before proceeding
            if (!this.workflowManager.projectId && projectId) {
                this.workflowManager.projectId = projectId;
                console.log('EventHandlerService: Set workflowManager.projectId to', projectId);
            }

            console.log('EventHandlerService: workflowManager.projectId is now:', this.workflowManager.projectId);

            // Set up all event listeners
            this.setupEventListeners();

            // Start auto-refresh
            this.startAutoRefresh();

            // Load initial data
            this.loadInitialData();

            this.isInitialized = true;
            console.log('✅ EventHandlerService: Initialization completed successfully');
        } catch (error) {
            console.error('❌ EventHandlerService: Initialization failed:', error);
            throw error;
        }
    }

    /**
     * Set up all event listeners for the workflow interface
     */
    setupEventListeners() {
        try {
            console.log('🧩 EventHandlerService: Setting up event listeners');

            // Navigation buttons
            document.getElementById('prev-stage')?.addEventListener('click', () => {
                this.workflowManager.navigateStage(-1);
            });

            document.getElementById('next-stage')?.addEventListener('click', () => {
                this.workflowManager.navigateStage(1);
            });

            // Stage indicators
            document.querySelectorAll('.stage-indicator').forEach((indicator, index) => {
                indicator.addEventListener('click', () => {
                    this.workflowManager.jumpToStage(index + 1);
                });
            });

            // Auto-refresh toggle
            const autoRefreshToggle = document.getElementById('auto-refresh-toggle');
            if (autoRefreshToggle) {
                autoRefreshToggle.addEventListener('change', (e) => {
                    if (e.target.checked) {
                        this.workflowManager.startAutoRefresh();
                    } else {
                        this.workflowManager.stopAutoRefresh();
                    }
                });
            }

            // Keyboard navigation
            document.addEventListener('keydown', (e) => {
                if (e.key === 'ArrowLeft') this.workflowManager.navigateStage(-1);
                if (e.key === 'ArrowRight') this.workflowManager.navigateStage(1);
            });

            console.log('✅ EventHandlerService: Event listeners setup completed');
        } catch (error) {
            console.error('❌ EventHandlerService: Failed to setup event listeners:', error);
            throw error;
        }
    }

    /**
     * Start the auto-refresh functionality
     */
    startAutoRefresh() {
        if (this.isAutoRefreshing) return;

        this.isAutoRefreshing = true;
        this.autoRefreshInterval = setInterval(() => {
            this.workflowManager.refreshWorkflowStatus();
        }, 10000); // Refresh every 10 seconds

        console.log('🔄 EventHandlerService: Auto-refresh started');
    }

    /**
     * Load initial data for the workflow
     */
    async loadInitialData() {
        try {
            console.log('📊 EventHandlerService: Loading initial data');

            // Ensure workflowManager has the projectId before loading data
            if (!this.workflowManager.projectId && this.projectId) {
                this.workflowManager.projectId = this.projectId;
                console.log('EventHandlerService: Set workflowManager.projectId to', this.projectId);
            }

            // Load project data
            await this.workflowManager.loadProjectData();

            // Load workflow state
            await this.workflowManager.loadWorkflowState();

            // Load current stage
            await this.workflowManager.loadCurrentStage();

            console.log('✅ EventHandlerService: Initial data loaded successfully');
        } catch (error) {
            console.error('❌ EventHandlerService: Failed to load initial data:', error);
            // Don't throw the error - handle it gracefully
            console.warn('EventHandlerService: Continuing without initial data load');
        }
    }

    /**
     * Clean up event listeners and intervals
     * Useful for page cleanup or when re-initializing
     */
    cleanup() {
        try {
            console.log('🧹 EventHandlerService: Cleaning up resources');

            // Clear auto-refresh interval
            if (this.autoRefreshInterval) {
                clearInterval(this.autoRefreshInterval);
                this.autoRefreshInterval = null;
            }

            // Remove event listeners (if needed for specific scenarios)
            // Note: Most event listeners are on DOM elements that will be garbage collected

            this.isInitialized = false;
            console.log('✅ EventHandlerService: Cleanup completed');
        } catch (error) {
            console.error('❌ EventHandlerService: Cleanup failed:', error);
            throw error;
        }
    }

    /**
     * Check if the service is properly initialized
     * @returns {boolean} True if initialized, false otherwise
     */
    isServiceInitialized() {
        return this.isInitialized && this.stageInitializationService && this.workflowManager;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = EventHandlerService;
} else if (typeof window !== 'undefined') {
    window.EventHandlerService = EventHandlerService;
}
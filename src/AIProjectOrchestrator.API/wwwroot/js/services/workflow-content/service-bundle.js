/**
 * WorkflowContentServiceBundle - Centralized service management and initialization
 * Provides a single entry point for all WorkflowContentService modules
 */
class WorkflowContentServiceBundle {
    constructor() {
        this.isInitialized = false;
        this.services = {};
    }

    /**
     * Initialize all services with proper dependency resolution
     * @param {Object} options - Configuration options
     * @param {Object} options.workflowManager - Workflow manager instance
     * @param {Object} options.apiClient - API client instance
     */
    async initialize(options = {}) {
        if (this.isInitialized) {
            console.warn('WorkflowContentService bundle already initialized');
            return this.services;
        }

        console.log('🚀 Initializing WorkflowContentService Bundle...');

        try {
            const { workflowManager, apiClient } = options;

            if (!workflowManager) {
                throw new Error('WorkflowManager is required');
            }
            if (!apiClient) {
                throw new Error('APIClient is required');
            }

            // Load all required scripts in dependency order
            await this.loadScripts();

            // Initialize the main WorkflowContentService
            this.services.workflowContentService = new WorkflowContentService(workflowManager, apiClient);

            this.isInitialized = true;
            console.log('✅ WorkflowContentService bundle initialization completed');

            return this.services;

        } catch (error) {
            console.error('❌ Failed to initialize WorkflowContentService bundle:', error);
            throw new Error(`WorkflowContentService bundle initialization failed: ${error.message}`);
        }
    }

    /**
     * Load all required script files in dependency order
     * @returns {Promise<void>}
     */
    async loadScripts() {
        console.log('📦 Loading WorkflowContentService scripts...');

        const scripts = [
            // Base class first
            '/js/services/workflow-content/base-content-generator.js',

            // Stage generators
            '/js/services/workflow-content/stage-generators/requirements-generator.js',
            '/js/services/workflow-content/stage-generators/planning-generator.js',
            '/js/services/workflow-content/stage-generators/stories-generator.js',
            '/js/services/workflow-content/stage-generators/prompts-generator.js',
            '/js/services/workflow-content/stage-generators/review-generator.js',

            // Action handlers
            '/js/services/workflow-content/action-handlers/requirements-handler.js',
            '/js/services/workflow-content/action-handlers/planning-handler.js',
            '/js/services/workflow-content/action-handlers/stories-handler.js',
            '/js/services/workflow-content/action-handlers/prompts-handler.js',
            '/js/services/workflow-content/action-handlers/project-handler.js'
        ];

        for (const script of scripts) {
            await this.loadScript(script);
        }

        console.log('✅ All WorkflowContentService scripts loaded');
    }

    /**
     * Load a single script file
     * @param {string} src - Script source URL
     * @returns {Promise<void>}
     */
    async loadScript(src) {
        return new Promise((resolve, reject) => {
            // Check if script is already loaded
            if (document.querySelector(`script[src="${src}"]`)) {
                console.log(`Script already loaded: ${src}`);
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = src;
            script.type = 'text/javascript';
            script.async = false; // Load scripts in order

            script.onload = () => {
                console.log(`✅ Script loaded: ${src}`);
                resolve();
            };

            script.onerror = () => {
                console.error(`❌ Failed to load script: ${src}`);
                reject(new Error(`Failed to load script: ${src}`));
            };

            document.head.appendChild(script);
        });
    }

    /**
     * Get the WorkflowContentService instance
     * @returns {WorkflowContentService} The main service instance
     */
    getWorkflowContentService() {
        if (!this.isInitialized) {
            throw new Error('WorkflowContentService bundle not initialized. Call initialize() first.');
        }
        return this.services.workflowContentService;
    }

    /**
     * Get service health status
     * @returns {Object} Health status information
     */
    getHealthStatus() {
        if (!this.isInitialized) {
            return { status: 'not_initialized', services: {} };
        }

        const health = {
            status: 'healthy',
            services: {
                workflowContentService: this.services.workflowContentService ? 'available' : 'missing'
            },
            isInitialized: this.isInitialized
        };

        // Check if main service is available and healthy
        if (this.services.workflowContentService) {
            try {
                const serviceHealth = this.services.workflowContentService.getHealthStatus();
                health.serviceHealth = serviceHealth;

                if (serviceHealth.status !== 'healthy') {
                    health.status = 'degraded';
                }
            } catch (error) {
                health.services.workflowContentService = 'unhealthy';
                health.status = 'degraded';
            }
        } else {
            health.status = 'unhealthy';
        }

        return health;
    }

    /**
     * Cleanup all services
     */
    async cleanup() {
        console.log('🧹 Cleaning up WorkflowContentService Bundle...');

        if (this.services.workflowContentService) {
            try {
                // Cleanup main service if it has cleanup method
                if (typeof this.services.workflowContentService.cleanup === 'function') {
                    await this.services.workflowContentService.cleanup();
                }
                console.log('✅ WorkflowContentService cleaned up');
            } catch (error) {
                console.error('❌ Error cleaning up WorkflowContentService:', error);
            }
        }

        this.services = {};
        this.isInitialized = false;
        console.log('✅ WorkflowContentService bundle cleanup completed');
    }

    /**
     * Get bundle version and information
     * @returns {Object} Bundle information
     */
    getBundleInfo() {
        return {
            name: 'WorkflowContentService Bundle',
            version: '2.0.0',
            description: 'Modular service bundle for WorkflowContentService functionality',
            modules: [
                'BaseContentGenerator',
                'RequirementsGenerator', 'PlanningGenerator', 'StoriesGenerator', 'PromptsGenerator', 'ReviewGenerator',
                'RequirementsHandler', 'PlanningHandler', 'StoriesHandler', 'PromptsHandler', 'ProjectHandler',
                'WorkflowContentService'
            ],
            isInitialized: this.isInitialized,
            totalEstimatedLines: this.estimateTotalLines()
        };
    }

    /**
     * Estimate total lines of code across all modules
     * @returns {number} Estimated total lines
     */
    estimateTotalLines() {
        // Rough estimates based on module complexity
        const lineEstimates = {
            'BaseContentGenerator': 244,
            'RequirementsGenerator': 184,
            'PlanningGenerator': 207,
            'StoriesGenerator': 244,
            'PromptsGenerator': 165,
            'ReviewGenerator': 118,
            'RequirementsHandler': 118,
            'PlanningHandler': 130,
            'StoriesHandler': 142,
            'PromptsHandler': 118,
            'ProjectHandler': 108,
            'WorkflowContentService': 334
        };

        return Object.values(lineEstimates).reduce((total, lines) => total + lines, 0);
    }

    /**
     * Check if bundle is ready to use
     * @returns {boolean} True if ready
     */
    isReady() {
        return this.isInitialized && this.services.workflowContentService;
    }

    /**
     * Wait for bundle to be ready
     * @param {number} timeout - Timeout in milliseconds
     * @returns {Promise<boolean>} True if ready, false if timeout
     */
    async waitForReady(timeout = 5000) {
        const startTime = Date.now();

        while (Date.now() - startTime < timeout) {
            if (this.isReady()) {
                return true;
            }
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        return false;
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { WorkflowContentServiceBundle };
}

// Make available globally for browser usage
if (typeof window !== 'undefined') {
    window.WorkflowContentServiceBundle = WorkflowContentServiceBundle;
}
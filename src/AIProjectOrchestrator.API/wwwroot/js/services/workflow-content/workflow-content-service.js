/**
 * WorkflowContentService - Handles all stage content generation for the workflow manager
 * Following Single Responsibility Principle by separating content generation from business logic
 * This is the main orchestrator that delegates to specialized generators and handlers
 */
class WorkflowContentService {
    /**
     * Initialize the WorkflowContentService with dependencies
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        if (!workflowManager) {
            throw new Error('WorkflowManager is required');
        }
        if (!apiClient) {
            throw new Error('APIClient is required');
        }

        this.workflowManager = workflowManager;
        this.apiClient = apiClient;
        this.isInitialized = false;

        // Initialize stage generators
        this.initializeGenerators();

        // Initialize action handlers
        this.initializeHandlers();
    }

    /**
     * Initialize stage content generators
     */
    initializeGenerators() {
        try {
            // Requirements Generator (Stage 1)
            if (typeof RequirementsGenerator !== 'undefined') {
                this.requirementsGenerator = new RequirementsGenerator(this.workflowManager, this.apiClient);
            } else {
                console.warn('RequirementsGenerator not available, using fallback');
                this.requirementsGenerator = new InlineRequirementsGenerator(this.workflowManager, this.apiClient);
            }

            // Planning Generator (Stage 2)
            if (typeof PlanningGenerator !== 'undefined') {
                this.planningGenerator = new PlanningGenerator(this.workflowManager, this.apiClient);
            } else {
                console.warn('PlanningGenerator not available, using fallback');
                this.planningGenerator = new InlinePlanningGenerator(this.workflowManager, this.apiClient);
            }

            // Stories Generator (Stage 3)
            if (typeof StoriesGenerator !== 'undefined') {
                this.storiesGenerator = new StoriesGenerator(this.workflowManager, this.apiClient);
            } else {
                console.warn('StoriesGenerator not available, using fallback');
                this.storiesGenerator = new InlineStoriesGenerator(this.workflowManager, this.apiClient);
            }

            // Prompts Generator (Stage 4)
            if (typeof PromptsGenerator !== 'undefined') {
                this.promptsGenerator = new PromptsGenerator(this.workflowManager, this.apiClient);
            } else {
                console.warn('PromptsGenerator not available, using fallback');
                this.promptsGenerator = new InlinePromptsGenerator(this.workflowManager, this.apiClient);
            }

            // Review Generator (Stage 5)
            if (typeof ReviewGenerator !== 'undefined') {
                this.reviewGenerator = new ReviewGenerator(this.workflowManager, this.apiClient);
            } else {
                console.warn('ReviewGenerator not available, using fallback');
                this.reviewGenerator = new InlineReviewGenerator(this.workflowManager, this.apiClient);
            }

            this.isInitialized = true;
            console.log('✅ WorkflowContentService generators initialized successfully');

        } catch (error) {
            console.error('❌ Failed to initialize generators:', error);
            this.initializeFallbackGenerators();
        }
    }

    /**
     * Initialize action handlers
     */
    initializeHandlers() {
        try {
            // Requirements Handler
            if (typeof RequirementsHandler !== 'undefined') {
                this.requirementsHandler = new RequirementsHandler(this.workflowManager, this.apiClient);
            } else {
                console.warn('RequirementsHandler not available, using fallback');
                this.requirementsHandler = new InlineRequirementsHandler(this.workflowManager, this.apiClient);
            }

            // Planning Handler
            if (typeof PlanningHandler !== 'undefined') {
                this.planningHandler = new PlanningHandler(this.workflowManager, this.apiClient);
            } else {
                console.warn('PlanningHandler not available, using fallback');
                this.planningHandler = new InlinePlanningHandler(this.workflowManager, this.apiClient);
            }

            // Stories Handler
            if (typeof StoriesHandler !== 'undefined') {
                this.storiesHandler = new StoriesHandler(this.workflowManager, this.apiClient);
            } else {
                console.warn('StoriesHandler not available, using fallback');
                this.storiesHandler = new InlineStoriesHandler(this.workflowManager, this.apiClient);
            }

            // Prompts Handler
            if (typeof PromptsHandler !== 'undefined') {
                this.promptsHandler = new PromptsHandler(this.workflowManager, this.apiClient);
            } else {
                console.warn('PromptsHandler not available, using fallback');
                this.promptsHandler = new InlinePromptsHandler(this.workflowManager, this.apiClient);
            }

            // Project Handler
            if (typeof ProjectHandler !== 'undefined') {
                this.projectHandler = new ProjectHandler(this.workflowManager, this.apiClient);
            } else {
                console.warn('ProjectHandler not available, using fallback');
                this.projectHandler = new InlineProjectHandler(this.workflowManager, this.apiClient);
            }

            console.log('✅ WorkflowContentService handlers initialized successfully');

        } catch (error) {
            console.error('❌ Failed to initialize handlers:', error);
            this.initializeFallbackHandlers();
        }
    }

    /**
     * Initialize fallback generators (inline implementations)
     */
    initializeFallbackGenerators() {
        console.log('🔄 Initializing fallback generators...');

        // These would be minimal inline implementations
        // For now, we'll log warnings and continue
        console.warn('Fallback generators not implemented yet - service may not function properly');
    }

    /**
     * Initialize fallback handlers (inline implementations)
     */
    initializeFallbackHandlers() {
        console.log('🔄 Initializing fallback handlers...');

        // These would be minimal inline implementations
        // For now, we'll log warnings and continue
        console.warn('Fallback handlers not implemented yet - service may not function properly');
    }

    /**
     * Get content for a specific stage
     * @param {number} stage - Stage number (1-5)
     * @returns {Promise<string>} HTML content for the stage
     */
    async getStageContent(stage) {
        try {
            console.log(`WorkflowContentService: Getting content for stage ${stage}`);

            const templates = {
                1: this.getRequirementsContent.bind(this),
                2: this.getPlanningContent.bind(this),
                3: this.getStoriesContent.bind(this),
                4: this.getPromptsContent.bind(this),
                5: this.getReviewContent.bind(this)
            };

            const content = templates[stage] ? await templates[stage]() : '<p>Stage not found</p>';
            console.log(`WorkflowContentService: Generated content for stage ${stage}, length: ${content.length}`);
            return content;
        } catch (error) {
            console.error(`WorkflowContentService: Error generating content for stage ${stage}:`, error);
            throw new Error(`Failed to generate content for stage ${stage}: ${error.message}`);
        }
    }

    /**
     * Get requirements stage content (Stage 1)
     * @returns {Promise<string>} HTML content
     */
    async getRequirementsContent() {
        if (!this.requirementsGenerator) {
            throw new Error('RequirementsGenerator not available');
        }
        return await this.requirementsGenerator.generateContent();
    }

    /**
     * Get planning stage content (Stage 2)
     * @returns {Promise<string>} HTML content
     */
    async getPlanningContent() {
        if (!this.planningGenerator) {
            throw new Error('PlanningGenerator not available');
        }
        return await this.planningGenerator.generateContent();
    }

    /**
     * Get stories stage content (Stage 3)
     * @returns {Promise<string>} HTML content
     */
    async getStoriesContent() {
        if (!this.storiesGenerator) {
            throw new Error('StoriesGenerator not available');
        }
        return await this.storiesGenerator.generateContent();
    }

    /**
     * Get prompts stage content (Stage 4)
     * @returns {Promise<string>} HTML content
     */
    async getPromptsContent() {
        if (!this.promptsGenerator) {
            throw new Error('PromptsGenerator not available');
        }
        return await this.promptsGenerator.generateContent();
    }

    /**
     * Get review stage content (Stage 5)
     * @returns {Promise<string>} HTML content
     */
    async getReviewContent() {
        if (!this.reviewGenerator) {
            throw new Error('ReviewGenerator not available');
        }
        return await this.reviewGenerator.generateContent();
    }

    /**
     * Check if the service is properly initialized
     * @returns {boolean} True if initialized, false otherwise
     */
    isServiceInitialized() {
        return this.isInitialized && this.workflowManager && this.apiClient;
    }

    // Action methods that the orchestrator will call
    // These delegate to the appropriate handlers

    /**
     * Analyze requirements for the project
     * @returns {Promise<void>}
     */
    async analyzeRequirements() {
        if (!this.requirementsHandler) {
            throw new Error('RequirementsHandler not available');
        }
        return await this.requirementsHandler.analyzeRequirements();
    }

    /**
     * Generate project plan
     * @returns {Promise<void>}
     */
    async generatePlan() {
        if (!this.planningHandler) {
            throw new Error('PlanningHandler not available');
        }
        return await this.planningHandler.generatePlan();
    }

    /**
     * Regenerate project plan
     * @returns {Promise<void>}
     */
    async regeneratePlan() {
        if (!this.planningHandler) {
            throw new Error('PlanningHandler not available');
        }
        return await this.planningHandler.regeneratePlan();
    }

    /**
     * Generate user stories
     * @returns {Promise<void>}
     */
    async generateStories() {
        if (!this.storiesHandler) {
            throw new Error('StoriesHandler not available');
        }
        return await this.storiesHandler.generateStories();
    }

    /**
     * Regenerate user stories
     * @returns {Promise<void>}
     */
    async regenerateStories() {
        if (!this.storiesHandler) {
            throw new Error('StoriesHandler not available');
        }
        return await this.storiesHandler.regenerateStories();
    }

    /**
     * Generate all prompts for approved stories
     * @returns {Promise<void>}
     */
    async generateAllPrompts() {
        if (!this.promptsHandler) {
            throw new Error('PromptsHandler not available');
        }
        return await this.promptsHandler.generateAllPrompts();
    }

    /**
     * Complete the project
     * @returns {Promise<void>}
     */
    async completeProject() {
        if (!this.projectHandler) {
            throw new Error('ProjectHandler not available');
        }
        return await this.projectHandler.completeProject();
    }

    /**
     * Export project results
     * @returns {Promise<void>}
     */
    async exportProject() {
        if (!this.projectHandler) {
            throw new Error('ProjectHandler not available');
        }
        return await this.projectHandler.exportProject();
    }

    /**
     * Generate project report
     * @returns {Promise<void>}
     */
    async generateReport() {
        if (!this.projectHandler) {
            throw new Error('ProjectHandler not available');
        }
        return await this.projectHandler.generateReport();
    }

    /**
     * Get service health status
     * @returns {object} Health status information
     */
    getHealthStatus() {
        const health = {
            status: 'healthy',
            generators: {},
            handlers: {},
            isInitialized: this.isInitialized
        };

        // Check generators
        const generators = ['requirementsGenerator', 'planningGenerator', 'storiesGenerator', 'promptsGenerator', 'reviewGenerator'];
        generators.forEach(generatorName => {
            health.generators[generatorName] = this[generatorName] ? 'available' : 'missing';
        });

        // Check handlers
        const handlers = ['requirementsHandler', 'planningHandler', 'storiesHandler', 'promptsHandler', 'projectHandler'];
        handlers.forEach(handlerName => {
            health.handlers[handlerName] = this[handlerName] ? 'available' : 'missing';
        });

        // Overall health
        const allGeneratorsAvailable = Object.values(health.generators).every(status => status === 'available');
        const allHandlersAvailable = Object.values(health.handlers).every(status => status === 'available');

        if (!allGeneratorsAvailable || !allHandlersAvailable) {
            health.status = 'degraded';
        }

        if (!this.isInitialized) {
            health.status = 'not_initialized';
        }

        return health;
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WorkflowContentService;
} else if (typeof window !== 'undefined') {
    window.WorkflowContentService = WorkflowContentService;
}
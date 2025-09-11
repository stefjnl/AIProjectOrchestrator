/**
 * Service Bundle - Centralized service management and initialization
 * Provides a single entry point for all StoriesOverview services
 */

class StoriesOverviewServiceBundle {
    constructor() {
        this.services = {};
        this.isInitialized = false;
        this.serviceDependencies = {
            // Core services with no dependencies
            'statusUtils': [],
            'exportService': [],

            // UI services that depend on core services
            'storyRenderer': ['statusUtils'],
            'progressRenderer': [],

            // API services
            'storyApiService': [],
            'promptService': ['statusUtils'],

            // Modal services that depend on UI services
            'storyModalService': ['storyRenderer', 'progressRenderer'],
            'promptModalService': ['progressRenderer']
        };
    }

    /**
     * Initialize all services with proper dependency resolution
     * @param {Object} options - Configuration options
     * @param {Object} options.apiClient - API client instance
     * @param {Object} options.logger - Logger instance
     * @param {Object} options.config - Configuration object
     */
    async initialize(options = {}) {
        if (this.isInitialized) {
            console.warn('Service bundle already initialized');
            return this.services;
        }

        console.log('🚀 Initializing StoriesOverview Service Bundle...');

        try {
            // Create services in dependency order
            const creationOrder = this.resolveDependencyOrder();

            for (const serviceName of creationOrder) {
                await this.createService(serviceName, options);
            }

            this.isInitialized = true;
            console.log('✅ Service bundle initialization completed');

            return this.services;

        } catch (error) {
            console.error('❌ Failed to initialize service bundle:', error);
            throw new Error(`Service bundle initialization failed: ${error.message}`);
        }
    }

    /**
     * Resolve dependency order using topological sort
     * @returns {Array} Ordered list of service names
     */
    resolveDependencyOrder() {
        const visited = new Set();
        const visiting = new Set();
        const order = [];

        const visit = (serviceName) => {
            if (visiting.has(serviceName)) {
                throw new Error(`Circular dependency detected for service: ${serviceName}`);
            }
            if (visited.has(serviceName)) {
                return;
            }

            visiting.add(serviceName);

            const dependencies = this.serviceDependencies[serviceName] || [];
            for (const dependency of dependencies) {
                if (!this.serviceDependencies[dependency]) {
                    throw new Error(`Unknown dependency: ${dependency} for service: ${serviceName}`);
                }
                visit(dependency);
            }

            visiting.delete(serviceName);
            visited.add(serviceName);
            order.push(serviceName);
        };

        for (const serviceName of Object.keys(this.serviceDependencies)) {
            visit(serviceName);
        }

        return order;
    }

    /**
     * Create an individual service with its dependencies
     * @param {string} serviceName - Name of the service to create
     * @param {Object} options - Configuration options
     */
    async createService(serviceName, options) {
        console.log(`🔧 Creating service: ${serviceName}`);

        const dependencies = this.getServiceDependencies(serviceName);
        const serviceOptions = { ...options, dependencies };

        let service;

        switch (serviceName) {
            case 'statusUtils':
                service = new StatusUtils();
                break;

            case 'exportService':
                service = new ExportService();
                break;

            case 'storyRenderer':
                service = new StoryRenderer(dependencies.statusUtils);
                break;

            case 'progressRenderer':
                service = new ProgressRenderer();
                break;

            case 'storyApiService':
                service = new StoryApiService(options.apiClient);
                break;

            case 'promptService':
                service = new PromptService(options.apiClient, dependencies.statusUtils);
                break;

            case 'storyModalService':
                service = new StoryModalService(
                    dependencies.storyRenderer,
                    dependencies.progressRenderer
                );
                break;

            case 'promptModalService':
                service = new PromptModalService(dependencies.progressRenderer);
                break;

            default:
                throw new Error(`Unknown service: ${serviceName}`);
        }

        // Initialize service if it has an initialize method
        if (typeof service.initialize === 'function') {
            await service.initialize(serviceOptions);
        }

        this.services[serviceName] = service;
        console.log(`✅ Service created: ${serviceName}`);
    }

    /**
     * Get resolved dependencies for a service
     * @param {string} serviceName - Name of the service
     * @returns {Object} Resolved dependencies
     */
    getServiceDependencies(serviceName) {
        const dependencies = {};
        const serviceDeps = this.serviceDependencies[serviceName] || [];

        for (const depName of serviceDeps) {
            if (!this.services[depName]) {
                throw new Error(`Dependency not found: ${depName} for service: ${serviceName}`);
            }
            dependencies[depName] = this.services[depName];
        }

        return dependencies;
    }

    /**
     * Get a service instance
     * @param {string} serviceName - Name of the service
     * @returns {Object} Service instance
     */
    getService(serviceName) {
        if (!this.isInitialized) {
            throw new Error('Service bundle not initialized. Call initialize() first.');
        }

        const service = this.services[serviceName];
        if (!service) {
            throw new Error(`Service not found: ${serviceName}`);
        }

        return service;
    }

    /**
     * Get all services
     * @returns {Object} All service instances
     */
    getAllServices() {
        if (!this.isInitialized) {
            throw new Error('Service bundle not initialized. Call initialize() first.');
        }

        return { ...this.services };
    }

    /**
     * Check if a service exists
     * @param {string} serviceName - Name of the service
     * @returns {boolean} Whether the service exists
     */
    hasService(serviceName) {
        return this.services.hasOwnProperty(serviceName);
    }

    /**
     * Create a StoriesOverviewManager instance with all services
     * @param {string} generationId - Generation ID
     * @param {string} projectId - Project ID
     * @returns {StoriesOverviewManager} Configured manager instance
     */
    createManager(generationId, projectId) {
        if (!this.isInitialized) {
            throw new Error('Service bundle not initialized. Call initialize() first.');
        }

        console.log(`🏗️ Creating StoriesOverviewManager with generationId=${generationId}, projectId=${projectId}`);

        const manager = new StoriesOverviewManager();

        // Inject all services into the manager
        manager.storyApiService = this.getService('storyApiService');
        manager.statusUtils = this.getService('statusUtils');
        manager.storyRenderer = this.getService('storyRenderer');
        manager.progressRenderer = this.getService('progressRenderer');
        manager.exportService = this.getService('exportService');
        manager.storyModalService = this.getService('storyModalService');
        manager.promptModalService = this.getService('promptModalService');
        manager.promptService = this.getService('promptService');

        // Initialize the manager
        manager.initialize(generationId, projectId);

        console.log('✅ StoriesOverviewManager created successfully');
        return manager;
    }

    /**
     * Get service health status
     * @returns {Object} Health status of all services
     */
    getHealthStatus() {
        if (!this.isInitialized) {
            return { status: 'not_initialized', services: {} };
        }

        const health = {
            status: 'healthy',
            services: {},
            totalServices: Object.keys(this.services).length,
            initializedServices: 0
        };

        for (const [serviceName, service] of Object.entries(this.services)) {
            let serviceHealth = 'healthy';

            // Check if service has health check method
            if (typeof service.getHealthStatus === 'function') {
                serviceHealth = service.getHealthStatus();
            } else if (service) {
                serviceHealth = 'healthy';
            } else {
                serviceHealth = 'unhealthy';
            }

            health.services[serviceName] = serviceHealth;
            if (serviceHealth === 'healthy') {
                health.initializedServices++;
            }
        }

        // Overall health is healthy if all services are healthy
        if (health.initializedServices < health.totalServices) {
            health.status = 'degraded';
        }

        return health;
    }

    /**
     * Cleanup all services
     */
    async cleanup() {
        console.log('🧹 Cleaning up StoriesOverview Service Bundle...');

        for (const [serviceName, service] of Object.entries(this.services)) {
            try {
                if (typeof service.destroy === 'function') {
                    await service.destroy();
                    console.log(`✅ Service cleaned up: ${serviceName}`);
                }
            } catch (error) {
                console.error(`❌ Error cleaning up service ${serviceName}:`, error);
            }
        }

        this.services = {};
        this.isInitialized = false;
        console.log('✅ Service bundle cleanup completed');
    }

    /**
     * Get service bundle version and information
     * @returns {Object} Bundle information
     */
    getBundleInfo() {
        return {
            name: 'StoriesOverview Service Bundle',
            version: '1.0.0',
            description: 'Comprehensive service bundle for StoriesOverview functionality',
            services: Object.keys(this.serviceDependencies),
            isInitialized: this.isInitialized,
            totalLines: this.estimateTotalLines()
        };
    }

    /**
     * Estimate total lines of code across all services
     * @returns {number} Estimated total lines
     */
    estimateTotalLines() {
        // Rough estimates based on service complexity
        const lineEstimates = {
            'statusUtils': 244,
            'exportService': 184,
            'storyRenderer': 267,
            'progressRenderer': 334,
            'storyApiService': 58,
            'promptService': 118,
            'storyModalService': 220,
            'promptModalService': 244
        };

        return Object.keys(this.serviceDependencies)
            .reduce((total, serviceName) => total + (lineEstimates[serviceName] || 0), 0);
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { StoriesOverviewServiceBundle };
}

// Make available globally for browser usage
if (typeof window !== 'undefined') {
    window.StoriesOverviewServiceBundle = StoriesOverviewServiceBundle;
}
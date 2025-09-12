/**
 * StoriesOverviewServiceBundle - Manages service loading and initialization
 * Provides a unified interface for loading all StoriesOverview components
 */
class StoriesOverviewServiceBundle {
    constructor() {
        this.isLoaded = false;
        this.loadError = null;
        this.components = {};
        console.log('StoriesOverviewServiceBundle initialized');
    }

    /**
     * Load all StoriesOverview components in correct dependency order
     * @returns {Promise<boolean>} True if all components loaded successfully
     */
    async loadComponents() {
        console.log('Loading StoriesOverview components...');

        try {
            // Check dependencies first
            this.validateDependencies();

            // Load components in dependency order
            await this.loadBaseManager();
            await this.loadComponentsInOrder();

            this.isLoaded = true;
            console.log('✅ All StoriesOverview components loaded successfully');
            return true;

        } catch (error) {
            this.loadError = error;
            console.error('❌ Failed to load StoriesOverview components:', error);
            return false;
        }
    }

    /**
     * Validate required dependencies
     */
    validateDependencies() {
        const requiredGlobals = ['StatusUtils', 'APIClient', 'App'];
        const missing = requiredGlobals.filter(global => typeof window[global] === 'undefined');

        if (missing.length > 0) {
            throw new Error(`Missing required dependencies: ${missing.join(', ')}`);
        }

        console.log('✅ All dependencies validated');
    }

    /**
     * Load base stories manager
     */
    async loadBaseManager() {
        if (typeof BaseStoriesManager === 'undefined') {
            throw new Error('BaseStoriesManager not loaded');
        }

        this.components.baseManager = BaseStoriesManager;
        console.log('✅ BaseStoriesManager loaded');
    }

    /**
     * Load components in dependency order
     */
    async loadComponentsInOrder() {
        const componentLoaders = [
            { name: 'StoryRenderer', class: StoryRenderer },
            { name: 'StoryActions', class: StoryActions },
            { name: 'PromptGenerator', class: PromptGenerator },
            { name: 'ModalManager', class: ModalManager },
            { name: 'StoryUtils', class: StoryUtils },
            { name: 'StoriesOverviewManager', class: StoriesOverviewManager }
        ];

        for (const { name, class: ComponentClass } of componentLoaders) {
            if (typeof ComponentClass === 'undefined') {
                throw new Error(`${name} not loaded`);
            }

            this.components[name.toLowerCase()] = ComponentClass;
            console.log(`✅ ${name} loaded`);
        }
    }

    /**
     * Check if all components are loaded
     * @returns {boolean} True if all components loaded
     */
    isReady() {
        return this.isLoaded && !this.loadError;
    }

    /**
     * Get loaded components
     * @returns {Object} Loaded components
     */
    getComponents() {
        if (!this.isReady()) {
            throw new Error('Components not ready. Call loadComponents() first.');
        }
        return { ...this.components };
    }

    /**
     * Get StoriesOverviewManager instance
     * @returns {StoriesOverviewManager} StoriesOverviewManager instance
     */
    getManager() {
        if (!this.isReady()) {
            throw new Error('Service bundle not ready. Call loadComponents() first.');
        }

        if (!window.storiesOverviewManager) {
            throw new Error('StoriesOverviewManager not initialized');
        }

        return window.storiesOverviewManager;
    }

    /**
     * Get service status
     * @returns {Object} Service status
     */
    getStatus() {
        return {
            loaded: this.isLoaded,
            error: this.loadError?.message || null,
            components: Object.keys(this.components),
            managerReady: !!window.storiesOverviewManager
        };
    }

    /**
     * Wait for service to be ready
     * @param {number} timeoutMs - Timeout in milliseconds (default: 5000)
     * @returns {Promise<boolean>} True if service became ready
     */
    async waitForReady(timeoutMs = 5000) {
        const startTime = Date.now();

        while (Date.now() - startTime < timeoutMs) {
            if (this.isReady()) {
                return true;
            }
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        return false;
    }

    /**
     * Retry loading components
     * @param {number} maxRetries - Maximum number of retries
     * @returns {Promise<boolean>} True if loading successful
     */
    async retryLoad(maxRetries = 3) {
        this.loadError = null;

        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            console.log(`Retry attempt ${attempt} of ${maxRetries}...`);

            const success = await this.loadComponents();
            if (success) {
                console.log(`✅ Successfully loaded on attempt ${attempt}`);
                return true;
            }

            if (attempt < maxRetries) {
                await new Promise(resolve => setTimeout(resolve, 1000 * attempt)); // Exponential backoff
            }
        }

        console.error(`❌ Failed to load after ${maxRetries} attempts`);
        return false;
    }

    /**
     * Cleanup resources
     */
    destroy() {
        console.log('Destroying StoriesOverviewServiceBundle...');

        if (window.storiesOverviewManager) {
            window.storiesOverviewManager.destroy();
        }

        this.components = {};
        this.isLoaded = false;
        this.loadError = null;

        console.log('StoriesOverviewServiceBundle destroyed');
    }
}

// Initialize service bundle
window.storiesOverviewServiceBundle = new StoriesOverviewServiceBundle();
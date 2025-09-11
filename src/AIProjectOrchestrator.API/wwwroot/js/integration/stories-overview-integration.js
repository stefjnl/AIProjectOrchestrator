/**
 * StoriesOverview Integration Layer
 * Provides easy-to-use integration for the refactored StoriesOverview system
 */

class StoriesOverviewIntegration {
    constructor() {
        this.serviceBundle = null;
        this.manager = null;
        this.isInitialized = false;
        this.config = {
            autoRefresh: true,
            refreshInterval: 30000, // 30 seconds
            enableNotifications: true,
            enableProgressTracking: true,
            mockMode: false
        };
    }

    /**
     * Initialize the StoriesOverview system with service bundle
     * @param {string} generationId - Generation ID
     * @param {string} projectId - Project ID
     * @param {Object} options - Configuration options
     * @returns {Promise<StoriesOverviewManager>} Configured manager instance
     */
    async initialize(generationId, projectId, options = {}) {
        if (this.isInitialized) {
            console.warn('StoriesOverview system already initialized');
            return this.manager;
        }

        try {
            console.log('🚀 Initializing StoriesOverview Integration...');

            // Merge configuration options
            this.config = { ...this.config, ...options };

            // Create and initialize service bundle
            this.serviceBundle = new StoriesOverviewServiceBundle();

            const bundleOptions = {
                apiClient: window.APIClient || this.createMockApiClient(),
                logger: console,
                config: this.config
            };

            await this.serviceBundle.initialize(bundleOptions);
            console.log('✅ Service bundle initialized');

            // Create manager instance
            this.manager = this.serviceBundle.createManager(generationId, projectId);
            console.log('✅ StoriesOverviewManager created');

            // Set up event listeners and auto-refresh
            this.setupEventListeners();

            if (this.config.autoRefresh) {
                this.startAutoRefresh();
            }

            // Set up global error handling
            this.setupErrorHandling();

            this.isInitialized = true;
            console.log('🎉 StoriesOverview Integration completed successfully');

            return this.manager;

        } catch (error) {
            console.error('❌ Failed to initialize StoriesOverview Integration:', error);
            this.showInitializationError(error);
            throw error;
        }
    }

    /**
     * Create mock API client for development/testing
     * @returns {Object} Mock API client
     */
    createMockApiClient() {
        console.log('📡 Using mock API client for development');

        return {
            getStories: async (generationId) => {
                console.log(`📡 Mock: getStories(${generationId})`);
                return this.generateMockStories();
            },
            approveStory: async (storyId) => {
                console.log(`📡 Mock: approveStory(${storyId})`);
                return { success: true, message: 'Story approved successfully' };
            },
            rejectStory: async (storyId, data) => {
                console.log(`📡 Mock: rejectStory(${storyId}, ${JSON.stringify(data)})`);
                return { success: true, message: 'Story rejected successfully' };
            },
            editStory: async (storyId, data) => {
                console.log(`📡 Mock: editStory(${storyId}, ${JSON.stringify(data)})`);
                return { success: true, message: 'Story updated successfully' };
            },
            generatePrompt: async (request) => {
                console.log(`📡 Mock: generatePrompt(${JSON.stringify(request)})`);
                return {
                    success: true,
                    promptId: `mock-prompt-${request.StoryGenerationId}-${Date.now()}`,
                    message: 'Prompt generated successfully'
                };
            },
            getPrompt: async (promptId) => {
                console.log(`📡 Mock: getPrompt(${promptId})`);
                return {
                    promptId: promptId,
                    storyTitle: 'Mock Story Title',
                    generatedPrompt: this.generateMockPrompt(),
                    createdAt: new Date().toISOString(),
                    qualityScore: 85
                };
            }
        };
    }

    /**
     * Generate mock stories for testing
     * @returns {Array} Array of mock story objects
     */
    generateMockStories() {
        return [
            {
                id: 'story-1',
                title: 'Implement User Authentication System',
                description: 'Create a secure authentication system with JWT tokens and refresh mechanisms',
                status: 'pending',
                priority: 'High',
                storyPoints: 8,
                hasPrompt: false,
                acceptanceCriteria: [
                    'Users can register with email and password',
                    'Users can login with credentials',
                    'JWT tokens are properly managed',
                    'Password reset functionality works'
                ]
            },
            {
                id: 'story-2',
                title: 'Design Responsive Dashboard Layout',
                description: 'Create a responsive dashboard that works on mobile, tablet, and desktop',
                status: 'approved',
                priority: 'Medium',
                storyPoints: 5,
                hasPrompt: true,
                promptId: 'prompt-123',
                acceptanceCriteria: [
                    'Layout adapts to screen size',
                    'Navigation is accessible on all devices',
                    'Charts and data visualizations are responsive'
                ]
            },
            {
                id: 'story-3',
                title: 'Setup CI/CD Pipeline',
                description: 'Configure automated testing and deployment pipeline',
                status: 'rejected',
                priority: 'Critical',
                storyPoints: 13,
                hasPrompt: false,
                rejectionFeedback: 'Needs more detailed technical specifications',
                acceptanceCriteria: [
                    'Automated unit tests run on commit',
                    'Integration tests validate API endpoints',
                    'Deployment happens automatically on main branch'
                ]
            }
        ];
    }

    /**
     * Generate mock prompt content
     * @returns {string} Mock prompt content
     */
    generateMockPrompt() {
        return `# Development Prompt

## Context
You are implementing a user authentication system for a web application.

## Requirements
- Implement secure JWT-based authentication
- Handle user registration and login
- Manage password reset functionality
- Ensure proper error handling and validation

## Technical Stack
- Backend: .NET 9 Web API
- Frontend: React with TypeScript
- Database: PostgreSQL with Entity Framework
- Authentication: JWT with refresh tokens

## Acceptance Criteria
✅ Users can register with email and password
✅ Users can login with valid credentials  
✅ JWT tokens are properly managed
✅ Password reset functionality works
✅ Proper error messages for invalid inputs

## Implementation Notes
- Use dependency injection for services
- Implement proper logging
- Follow security best practices
- Write comprehensive unit tests`;
    }

    /**
     * Set up event listeners for user interactions
     */
    setupEventListeners() {
        console.log('🔧 Setting up event listeners...');

        // Global event handlers (backward compatibility)
        window.viewStory = (index) => this.manager.viewStory(index);
        window.approveStory = (storyId) => this.manager.approveStory(storyId);
        window.rejectStory = (storyId) => this.manager.rejectStory(storyId);
        window.generatePromptForStory = (storyId, index) => this.manager.generatePromptForStory(storyId, index);
        window.viewPrompt = (promptId) => this.manager.viewPrompt(promptId);
        window.refreshStories = () => this.manager.refreshStories();
        window.approveAllStories = () => this.manager.approveAllStories();
        window.generatePromptsForApproved = () => this.manager.generatePromptsForApproved();
        window.continueToWorkflow = () => this.manager.continueToWorkflow();
        window.exportStories = () => this.manager.exportStories();

        // Modal close handlers
        document.addEventListener('click', (event) => {
            const storyModal = document.getElementById('story-modal');
            const editModal = document.getElementById('edit-modal');
            const promptModal = document.getElementById('prompt-viewer-modal');

            if (event.target === storyModal) {
                this.manager.closeStoryModal();
            }
            if (event.target === editModal) {
                this.manager.closeEditModal();
            }
            if (event.target === promptModal) {
                this.manager.promptModalService.closeModal();
            }
        });

        // ESC key handlers
        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                this.manager.closeStoryModal();
                this.manager.closeEditModal();
                this.manager.promptModalService.closeModal();
            }
        });

        // Window unload cleanup
        window.addEventListener('beforeunload', () => {
            this.cleanup();
        });

        console.log('✅ Event listeners set up');
    }

    /**
     * Set up global error handling
     */
    setupErrorHandling() {
        console.log('🛡️ Setting up error handling...');

        window.addEventListener('error', (event) => {
            console.error('Global error caught:', event.error);
            this.showError('An unexpected error occurred. Please refresh the page.');
        });

        window.addEventListener('unhandledrejection', (event) => {
            console.error('Unhandled promise rejection:', event.reason);
            this.showError('A network error occurred. Please check your connection and try again.');
        });

        console.log('✅ Error handling set up');
    }

    /**
     * Start auto-refresh functionality
     */
    startAutoRefresh() {
        if (this.autoRefreshInterval) {
            clearInterval(this.autoRefreshInterval);
        }

        this.autoRefreshInterval = setInterval(() => {
            console.log('🔄 Auto-refreshing stories...');
            this.manager.refreshStories();
        }, this.config.refreshInterval);

        console.log(`✅ Auto-refresh started (${this.config.refreshInterval}ms interval)`);
    }

    /**
     * Stop auto-refresh functionality
     */
    stopAutoRefresh() {
        if (this.autoRefreshInterval) {
            clearInterval(this.autoRefreshInterval);
            this.autoRefreshInterval = null;
            console.log('⏹️ Auto-refresh stopped');
        }
    }

    /**
     * Show error notification
     * @param {string} message - Error message
     */
    showError(message) {
        if (this.config.enableNotifications && this.manager?.progressRenderer) {
            this.manager.progressRenderer.showNotification(message, 'error');
        } else {
            console.error('Error:', message);
        }
    }

    /**
     * Show success notification
     * @param {string} message - Success message
     */
    showSuccess(message) {
        if (this.config.enableNotifications && this.manager?.progressRenderer) {
            this.manager.progressRenderer.showNotification(message, 'success');
        } else {
            console.log('Success:', message);
        }
    }

    /**
     * Show initialization error
     * @param {Error} error - Initialization error
     */
    showInitializationError(error) {
        const message = `Failed to initialize StoriesOverview: ${error.message}`;
        this.showError(message);

        // Show user-friendly error in UI
        const container = document.getElementById('stories-container') || document.getElementById('stories-grid');
        if (container) {
            container.innerHTML = `
                <div class="initialization-error">
                    <div class="error-icon">❌</div>
                    <h3>Initialization Error</h3>
                    <p>${message}</p>
                    <button class="btn btn-primary" onclick="location.reload()">
                        🔄 Reload Page
                    </button>
                </div>
            `;
        }
    }

    /**
     * Get system health status
     * @returns {Object} Health status information
     */
    getHealthStatus() {
        if (!this.isInitialized) {
            return { status: 'not_initialized', message: 'System not initialized' };
        }

        const bundleHealth = this.serviceBundle.getHealthStatus();
        const managerHealth = {
            hasStories: this.manager.stories && this.manager.stories.length > 0,
            currentStory: this.manager.currentStory !== null,
            isLoading: this.manager.isLoading
        };

        return {
            status: bundleHealth.status,
            bundleHealth,
            managerHealth,
            config: this.config,
            isInitialized: this.isInitialized
        };
    }

    /**
     * Get system information
     * @returns {Object} System information
     */
    getSystemInfo() {
        const bundleInfo = this.serviceBundle.getBundleInfo();

        return {
            name: 'StoriesOverview Integration System',
            version: '2.0.0',
            bundleInfo,
            isInitialized: this.isInitialized,
            storyCount: this.manager?.stories?.length || 0,
            generationId: this.manager?.generationId || null,
            projectId: this.manager?.projectId || null,
            config: this.config
        };
    }

    /**
     * Update configuration
     * @param {Object} newConfig - New configuration options
     */
    updateConfig(newConfig) {
        const oldAutoRefresh = this.config.autoRefresh;

        this.config = { ...this.config, ...newConfig };

        // Handle auto-refresh changes
        if (oldAutoRefresh !== this.config.autoRefresh) {
            if (this.config.autoRefresh) {
                this.startAutoRefresh();
            } else {
                this.stopAutoRefresh();
            }
        }

        console.log('⚙️ Configuration updated:', this.config);
    }

    /**
     * Refresh stories manually
     */
    async refreshStories() {
        if (!this.isInitialized) {
            console.warn('Cannot refresh - system not initialized');
            return;
        }

        try {
            await this.manager.refreshStories();
            this.showSuccess('Stories refreshed successfully');
        } catch (error) {
            console.error('Failed to refresh stories:', error);
            this.showError('Failed to refresh stories');
        }
    }

    /**
     * Export current stories
     * @param {string} format - Export format ('json', 'csv', 'txt')
     */
    exportStories(format = 'json') {
        if (!this.isInitialized) {
            console.warn('Cannot export - system not initialized');
            return;
        }

        try {
            this.manager.exportStories();
            this.showSuccess(`Stories exported as ${format.toUpperCase()}`);
        } catch (error) {
            console.error('Failed to export stories:', error);
            this.showError('Failed to export stories');
        }
    }

    /**
     * Navigate to workflow
     */
    continueToWorkflow() {
        if (!this.isInitialized) {
            console.warn('Cannot navigate - system not initialized');
            return;
        }

        try {
            this.manager.continueToWorkflow();
        } catch (error) {
            console.error('Failed to navigate to workflow:', error);
            this.showError('Failed to navigate to workflow');
        }
    }

    /**
     * Get current manager instance
     * @returns {StoriesOverviewManager} Current manager instance
     */
    getManager() {
        return this.manager;
    }

    /**
     * Get service bundle
     * @returns {StoriesOverviewServiceBundle} Service bundle instance
     */
    getServiceBundle() {
        return this.serviceBundle;
    }

    /**
     * Check if system is initialized
     * @returns {boolean} Initialization status
     */
    isSystemInitialized() {
        return this.isInitialized;
    }

    /**
     * Cleanup system resources
     */
    cleanup() {
        console.log('🧹 Cleaning up StoriesOverview Integration...');

        try {
            // Stop auto-refresh
            this.stopAutoRefresh();

            // Cleanup manager
            if (this.manager) {
                this.manager.destroy();
            }

            // Cleanup service bundle
            if (this.serviceBundle) {
                this.serviceBundle.cleanup();
            }

            // Remove global references
            if (window.storiesOverviewIntegration === this) {
                window.storiesOverviewIntegration = null;
            }

            this.isInitialized = false;
            console.log('✅ StoriesOverview Integration cleanup completed');

        } catch (error) {
            console.error('❌ Error during cleanup:', error);
        }
    }

    /**
     * Static method to create and initialize the system
     * @param {string} generationId - Generation ID
     * @param {string} projectId - Project ID
     * @param {Object} options - Configuration options
     * @returns {Promise<StoriesOverviewIntegration>} Initialized integration instance
     */
    static async create(generationId, projectId, options = {}) {
        const integration = new StoriesOverviewIntegration();
        await integration.initialize(generationId, projectId, options);
        return integration;
    }
}

// Make available globally
window.StoriesOverviewIntegration = StoriesOverviewIntegration;

// Create global instance for easy access
window.storiesOverviewIntegration = null;

// Auto-initialization helper
window.initializeStoriesOverview = async function (generationId, projectId, options = {}) {
    if (window.storiesOverviewIntegration) {
        console.warn('StoriesOverview already initialized, reinitializing...');
        window.storiesOverviewIntegration.cleanup();
    }

    window.storiesOverviewIntegration = await StoriesOverviewIntegration.create(generationId, projectId, options);
    return window.storiesOverviewIntegration;
};

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { StoriesOverviewIntegration };
}
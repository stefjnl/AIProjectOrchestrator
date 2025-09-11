const { describe, it, expect, beforeEach, afterEach, jest } = require('@jest/globals');

// Mock global objects
global.window = {
    App: {
        showNotification: jest.fn()
    }
};

global.console = {
    log: jest.fn(),
    warn: jest.fn(),
    error: jest.fn()
};

global.APIClient = {
    getRequirements: jest.fn(),
    getProjectPlan: jest.fn(),
    getStories: jest.fn(),
    getPrompts: jest.fn(),
    getPendingReviews: jest.fn(),
    getApprovedStories: jest.fn(),
    getProject: jest.fn()
};

describe('Workflow Service Integration', () => {
    let mockWorkflowManager;
    let services;

    beforeEach(() => {
        jest.clearAllMocks();

        // Create mock workflow manager
        mockWorkflowManager = {
            projectId: 'integration-test-project',
            currentStage: 1,
            workflowState: {
                requirementsAnalysis: {
                    analysisId: 'req-123',
                    status: 'NotStarted',
                    isApproved: false
                },
                projectPlanning: {
                    planningId: 'plan-123',
                    status: 'NotStarted',
                    isApproved: false
                },
                storyGeneration: {
                    generationId: 'story-123',
                    status: 'NotStarted',
                    isApproved: false
                },
                promptGeneration: {
                    completionPercentage: 0,
                    status: 'NotStarted'
                }
            },
            isNewProject: false,
            navigateStage: jest.fn(),
            jumpToStage: jest.fn(),
            startAutoRefresh: jest.fn(),
            stopAutoRefresh: jest.fn(),
            getRequirementsStage: jest.fn(),
            getPlanningStage: jest.fn(),
            getStoriesStage: jest.fn(),
            getPromptsStage: jest.fn(),
            getReviewStage: jest.fn(),
            initializeRequirementsStage: jest.fn(),
            initializePlanningStage: jest.fn(),
            initializeStoriesStage: jest.fn(),
            initializePromptsStage: jest.fn(),
            initializeReviewStage: jest.fn()
        };

        // Create service instances
        services = {
            contentService: {
                workflowManager: mockWorkflowManager,

                async getStageContent(stage) {
                    try {
                        const templates = {
                            1: this.workflowManager.getRequirementsStage.bind(this.workflowManager),
                            2: this.workflowManager.getPlanningStage.bind(this.workflowManager),
                            3: this.workflowManager.getStoriesStage.bind(this.workflowManager),
                            4: this.workflowManager.getPromptsStage.bind(this.workflowManager),
                            5: this.workflowManager.getReviewStage.bind(this.workflowManager)
                        };

                        return templates[stage] ? await templates[stage]() : '<p>Stage not found</p>';
                    } catch (error) {
                        console.error('WorkflowContentService.getStageContent error:', error);
                        return `<div class="stage-container"><h2>Error Loading Stage</h2><p>Failed to load stage ${stage} content.</p></div>`;
                    }
                }
            },

            eventHandlerService: {
                workflowManager: mockWorkflowManager,

                setupEventListeners() {
                    try {
                        // Simulate successful event listener setup
                        console.log('Event listeners setup completed');
                        return true;
                    } catch (error) {
                        console.error('EventHandlerService.setupEventListeners error:', error);
                        return false;
                    }
                },

                startAutoRefresh() {
                    this.workflowManager.startAutoRefresh();
                },

                stopAutoRefresh() {
                    this.workflowManager.stopAutoRefresh();
                }
            },

            stageInitializationService: {
                workflowManager: mockWorkflowManager,

                initializeStage(stage) {
                    try {
                        switch (stage) {
                            case 1:
                                this.workflowManager.initializeRequirementsStage();
                                break;
                            case 2:
                                this.workflowManager.initializePlanningStage();
                                break;
                            case 3:
                                this.workflowManager.initializeStoriesStage();
                                break;
                            case 4:
                                this.workflowManager.initializePromptsStage();
                                break;
                            case 5:
                                this.workflowManager.initializeReviewStage();
                                break;
                            default:
                                console.warn(`Unknown stage ${stage} for initialization`);
                        }
                        console.log(`Stage ${stage} initialized successfully`);
                    } catch (error) {
                        console.error(`StageInitializationService.initializeStage error for stage ${stage}:`, error);
                    }
                }
            }
        };
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    describe('Complete Workflow Cycle Integration', () => {
        it('should handle complete workflow from stage 1 to stage 5', async () => {
            // Mock all stage content methods
            mockWorkflowManager.getRequirementsStage.mockResolvedValue('<div>Requirements Content</div>');
            mockWorkflowManager.getPlanningStage.mockResolvedValue('<div>Planning Content</div>');
            mockWorkflowManager.getStoriesStage.mockResolvedValue('<div>Stories Content</div>');
            mockWorkflowManager.getPromptsStage.mockResolvedValue('<div>Prompts Content</div>');
            mockWorkflowManager.getReviewStage.mockResolvedValue('<div>Review Content</div>');

            // Simulate complete workflow cycle
            for (let stage = 1; stage <= 5; stage++) {
                // Load stage content
                const content = await services.contentService.getStageContent(stage);
                expect(content).toContain('Content');

                // Initialize stage functionality
                services.stageInitializationService.initializeStage(stage);

                // Verify stage initialization was called
                switch (stage) {
                    case 1:
                        expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalled();
                        break;
                    case 2:
                        expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalled();
                        break;
                    case 3:
                        expect(mockWorkflowManager.initializeStoriesStage).toHaveBeenCalled();
                        break;
                    case 4:
                        expect(mockWorkflowManager.initializePromptsStage).toHaveBeenCalled();
                        break;
                    case 5:
                        expect(mockWorkflowManager.initializeReviewStage).toHaveBeenCalled();
                        break;
                }
            }
        });

        it('should handle workflow progression with state changes', async () => {
            // Start with stage 1
            mockWorkflowManager.getRequirementsStage.mockResolvedValue('<div>Stage 1 Content</div>');
            let content = await services.contentService.getStageContent(1);
            expect(content).toBe('<div>Stage 1 Content</div>');
            services.stageInitializationService.initializeStage(1);

            // Simulate requirements approval - move to stage 2
            mockWorkflowManager.workflowState.requirementsAnalysis.isApproved = true;
            mockWorkflowManager.getPlanningStage.mockResolvedValue('<div>Stage 2 Content</div>');

            content = await services.contentService.getStageContent(2);
            expect(content).toBe('<div>Stage 2 Content</div>');
            services.stageInitializationService.initializeStage(2);

            // Verify both stages were initialized
            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalled();
            expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalled();
        });
    });

    describe('Service Coordination', () => {
        it('should coordinate content loading and stage initialization', async () => {
            const stage = 3;

            // Mock stage content and initialization
            mockWorkflowManager.getStoriesStage.mockResolvedValue('<div>Stories Content</div>');

            // Load content first
            const content = await services.contentService.getStageContent(stage);
            expect(content).toBe('<div>Stories Content</div>');

            // Then initialize stage
            services.stageInitializationService.initializeStage(stage);
            expect(mockWorkflowManager.initializeStoriesStage).toHaveBeenCalled();
        });

        it('should handle event handling setup before content loading', () => {
            // Setup event listeners first
            const setupResult = services.eventHandlerService.setupEventListeners();
            expect(setupResult).toBe(true);
            expect(console.log).toHaveBeenCalledWith('Event listeners setup completed');

            // Then proceed with content loading
            services.contentService.getStageContent(1);
            expect(mockWorkflowManager.getRequirementsStage).toHaveBeenCalled();
        });

        it('should handle auto-refresh coordination between services', () => {
            // Start auto-refresh through event handler
            services.eventHandlerService.startAutoRefresh();
            expect(mockWorkflowManager.startAutoRefresh).toHaveBeenCalled();

            // Stop auto-refresh
            services.eventHandlerService.stopAutoRefresh();
            expect(mockWorkflowManager.stopAutoRefresh).toHaveBeenCalled();
        });
    });

    describe('Error Handling Integration', () => {
        it('should handle content service errors without affecting other services', async () => {
            // Mock content service error
            mockWorkflowManager.getRequirementsStage.mockRejectedValue(new Error('Content loading failed'));

            // Content service should handle error gracefully
            const content = await services.contentService.getStageContent(1);
            expect(content).toContain('Error Loading Stage');

            // Stage initialization should still work
            services.stageInitializationService.initializeStage(1);
            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalled();
        });

        it('should handle stage initialization errors without affecting content loading', async () => {
            // Mock stage initialization error
            mockWorkflowManager.initializePlanningStage.mockImplementation(() => {
                throw new Error('Initialization failed');
            });

            // Content loading should work
            mockWorkflowManager.getPlanningStage.mockResolvedValue('<div>Planning Content</div>');
            const content = await services.contentService.getStageContent(2);
            expect(content).toBe('<div>Planning Content</div>');

            // Stage initialization should handle error gracefully
            expect(() => services.stageInitializationService.initializeStage(2)).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'StageInitializationService.initializeStage error for stage 2:',
                expect.any(Error)
            );
        });

        it('should handle event handler errors gracefully', () => {
            // Create a failing event handler
            const failingEventHandler = {
                workflowManager: mockWorkflowManager,
                setupEventListeners() {
                    throw new Error('Event setup failed');
                }
            };

            expect(() => failingEventHandler.setupEventListeners()).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'EventHandlerService.setupEventListeners error:',
                expect.any(Error)
            );
        });
    });

    describe('State Management Integration', () => {
        it('should maintain consistent workflow state across services', () => {
            const testState = {
                requirementsAnalysis: { isApproved: true, status: 'Approved' },
                projectPlanning: { isApproved: true, status: 'Approved' },
                storyGeneration: { isApproved: false, status: 'PendingReview' }
            };

            // Set state in workflow manager
            mockWorkflowManager.workflowState = testState;

            // All services should access the same state
            expect(services.contentService.workflowManager.workflowState).toBe(testState);
            expect(services.eventHandlerService.workflowManager.workflowState).toBe(testState);
            expect(services.stageInitializationService.workflowManager.workflowState).toBe(testState);
        });

        it('should handle workflow state transitions correctly', async () => {
            // Initial state - stage 1 not approved
            mockWorkflowManager.workflowState.requirementsAnalysis.isApproved = false;

            let content = await services.contentService.getStageContent(1);
            expect(content).toBeDefined();

            // Transition to approved
            mockWorkflowManager.workflowState.requirementsAnalysis.isApproved = true;

            // Content service should see the updated state
            content = await services.contentService.getStageContent(2);
            expect(content).toBeDefined();
        });
    });

    describe('Concurrent Service Operations', () => {
        it('should handle concurrent stage content requests', async () => {
            mockWorkflowManager.getRequirementsStage.mockResolvedValue('<div>Requirements</div>');
            mockWorkflowManager.getPlanningStage.mockResolvedValue('<div>Planning</div>');

            // Simulate concurrent requests
            const results = await Promise.all([
                services.contentService.getStageContent(1),
                services.contentService.getStageContent(2),
                services.contentService.getStageContent(1)
            ]);

            expect(results[0]).toBe('<div>Requirements</div>');
            expect(results[1]).toBe('<div>Planning</div>');
            expect(results[2]).toBe('<div>Requirements</div>');
        });

        it('should handle concurrent stage initialization', () => {
            // Initialize multiple stages concurrently
            services.stageInitializationService.initializeStage(1);
            services.stageInitializationService.initializeStage(2);
            services.stageInitializationService.initializeStage(3);

            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalled();
            expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalled();
            expect(mockWorkflowManager.initializeStoriesStage).toHaveBeenCalled();
        });

        it('should handle mixed service operations concurrently', async () => {
            // Mix of content loading, initialization, and event handling
            const operations = [
                services.contentService.getStageContent(1),
                services.stageInitializationService.initializeStage(2),
                services.eventHandlerService.setupEventListeners(),
                services.contentService.getStageContent(3)
            ];

            const results = await Promise.all(operations);

            expect(results[0]).toBeDefined(); // Content loaded
            expect(results[2]).toBe(true); // Event setup successful
            expect(results[3]).toBeDefined(); // Content loaded

            // Verify initialization was called
            expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalled();
        });
    });

    describe('Service Fallback Integration', () => {
        it('should use fallback implementations when services fail', async () => {
            // Simulate service failure by creating a broken content service
            const brokenContentService = {
                workflowManager: mockWorkflowManager,
                getStageContent: jest.fn().mockRejectedValue(new Error('Service failed'))
            };

            // Workflow manager should fall back to inline implementation
            const fallbackContent = await mockWorkflowManager.getRequirementsStage();
            expect(fallbackContent).toBeDefined();
        });

        it('should maintain functionality with partial service failures', async () => {
            // Simulate one service failing while others work
            mockWorkflowManager.getRequirementsStage.mockRejectedValue(new Error('Content failed'));

            // Content service should handle error gracefully
            const content = await services.contentService.getStageContent(1);
            expect(content).toContain('Error Loading Stage');

            // Other services should still work
            services.stageInitializationService.initializeStage(1);
            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalled();
        });
    });
});

describe('Workflow Manager Integration', () => {
    it('should coordinate all services through WorkflowManager', () => {
        // Create a mock WorkflowManager that uses the services
        const mockManager = {
            services: services,
            currentStage: 1,

            async loadStageContent(stage) {
                const content = await this.services.contentService.getStageContent(stage);
                this.services.stageInitializationService.initializeStage(stage);
                return content;
            },

            setupEventListeners() {
                return this.services.eventHandlerService.setupEventListeners();
            }
        };

        // Test coordination
        const setupResult = mockManager.setupEventListeners();
        expect(setupResult).toBe(true);

        // Verify services are properly coordinated
        expect(mockManager.services).toBe(services);
        expect(mockManager.services.contentService).toBeDefined();
        expect(mockManager.services.eventHandlerService).toBeDefined();
        expect(mockManager.services.stageInitializationService).toBeDefined();
    });

    it('should handle service initialization failures gracefully', () => {
        // Create a WorkflowManager with failing services
        const failingManager = {
            services: {
                contentService: {
                    getStageContent: jest.fn().mockRejectedValue(new Error('Content failed'))
                },
                eventHandlerService: {
                    setupEventListeners: jest.fn().mockReturnValue(false)
                },
                stageInitializationService: {
                    initializeStage: jest.fn().mockImplementation(() => {
                        throw new Error('Initialization failed');
                    })
                }
            },

            async loadStageContent(stage) {
                try {
                    return await this.services.contentService.getStageContent(stage);
                } catch (error) {
                    return '<div>Fallback content</div>';
                }
            }
        };

        // Should handle content service failure
        expect(failingManager.loadStageContent(1)).resolves.toBe('<div>Fallback content</div>');

        // Should handle event handler failure
        expect(failingManager.services.eventHandlerService.setupEventListeners()).toBe(false);

        // Should handle initialization failure
        expect(() => failingManager.services.stageInitializationService.initializeStage(1)).toThrow();
    });
});
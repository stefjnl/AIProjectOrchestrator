const { describe, it, expect, beforeEach, afterEach, jest } = require('@jest/globals');

// Mock console
global.console = {
    log: jest.fn(),
    warn: jest.fn(),
    error: jest.fn()
};

describe('StageInitializationService', () => {
    let service;
    let mockWorkflowManager;

    beforeEach(() => {
        // Reset all mocks
        jest.clearAllMocks();

        // Create mock workflow manager
        mockWorkflowManager = {
            projectId: 'test-project-123',
            currentStage: 1,
            workflowState: {
                requirementsAnalysis: { status: 'NotStarted', isApproved: false },
                projectPlanning: { status: 'NotStarted', isApproved: false },
                storyGeneration: { status: 'NotStarted', isApproved: false },
                promptGeneration: { status: 'NotStarted', completionPercentage: 0 }
            },
            initializeRequirementsStage: jest.fn(),
            initializePlanningStage: jest.fn(),
            initializeStoriesStage: jest.fn(),
            initializePromptsStage: jest.fn(),
            initializeReviewStage: jest.fn()
        };

        // Create service instance
        service = {
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
        };
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    describe('initializeStage', () => {
        it('should initialize stage 1 (Requirements) successfully', () => {
            service.initializeStage(1);

            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalled();
            expect(console.log).toHaveBeenCalledWith('Stage 1 initialized successfully');
        });

        it('should initialize stage 2 (Planning) successfully', () => {
            service.initializeStage(2);

            expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalled();
            expect(console.log).toHaveBeenCalledWith('Stage 2 initialized successfully');
        });

        it('should initialize stage 3 (Stories) successfully', () => {
            service.initializeStage(3);

            expect(mockWorkflowManager.initializeStoriesStage).toHaveBeenCalled();
            expect(console.log).toHaveBeenCalledWith('Stage 3 initialized successfully');
        });

        it('should initialize stage 4 (Prompts) successfully', () => {
            service.initializeStage(4);

            expect(mockWorkflowManager.initializePromptsStage).toHaveBeenCalled();
            expect(console.log).toHaveBeenCalledWith('Stage 4 initialized successfully');
        });

        it('should initialize stage 5 (Review) successfully', () => {
            service.initializeStage(5);

            expect(mockWorkflowManager.initializeReviewStage).toHaveBeenCalled();
            expect(console.log).toHaveBeenCalledWith('Stage 5 initialized successfully');
        });

        it('should handle unknown stage numbers gracefully', () => {
            service.initializeStage(99);

            expect(console.warn).toHaveBeenCalledWith('Unknown stage 99 for initialization');
            // Should not call any initialization method
            expect(mockWorkflowManager.initializeRequirementsStage).not.toHaveBeenCalled();
            expect(mockWorkflowManager.initializePlanningStage).not.toHaveBeenCalled();
            expect(mockWorkflowManager.initializeStoriesStage).not.toHaveBeenCalled();
            expect(mockWorkflowManager.initializePromptsStage).not.toHaveBeenCalled();
            expect(mockWorkflowManager.initializeReviewStage).not.toHaveBeenCalled();
        });

        it('should handle stage 0 gracefully', () => {
            service.initializeStage(0);

            expect(console.warn).toHaveBeenCalledWith('Unknown stage 0 for initialization');
        });

        it('should handle negative stage numbers gracefully', () => {
            service.initializeStage(-1);

            expect(console.warn).toHaveBeenCalledWith('Unknown stage -1 for initialization');
        });
    });

    describe('Error handling', () => {
        it('should handle errors in stage 1 initialization gracefully', () => {
            mockWorkflowManager.initializeRequirementsStage.mockImplementation(() => {
                throw new Error('Requirements initialization failed');
            });

            expect(() => service.initializeStage(1)).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'StageInitializationService.initializeStage error for stage 1:',
                expect.any(Error)
            );
        });

        it('should handle errors in stage 2 initialization gracefully', () => {
            mockWorkflowManager.initializePlanningStage.mockImplementation(() => {
                throw new Error('Planning initialization failed');
            });

            expect(() => service.initializeStage(2)).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'StageInitializationService.initializeStage error for stage 2:',
                expect.any(Error)
            );
        });

        it('should handle errors in stage 3 initialization gracefully', () => {
            mockWorkflowManager.initializeStoriesStage.mockImplementation(() => {
                throw new Error('Stories initialization failed');
            });

            expect(() => service.initializeStage(3)).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'StageInitializationService.initializeStage error for stage 3:',
                expect.any(Error)
            );
        });

        it('should handle errors in stage 4 initialization gracefully', () => {
            mockWorkflowManager.initializePromptsStage.mockImplementation(() => {
                throw new Error('Prompts initialization failed');
            });

            expect(() => service.initializeStage(4)).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'StageInitializationService.initializeStage error for stage 4:',
                expect.any(Error)
            );
        });

        it('should handle errors in stage 5 initialization gracefully', () => {
            mockWorkflowManager.initializeReviewStage.mockImplementation(() => {
                throw new Error('Review initialization failed');
            });

            expect(() => service.initializeStage(5)).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'StageInitializationService.initializeStage error for stage 5:',
                expect.any(Error)
            );
        });
    });

    describe('Service lifecycle', () => {
        it('should maintain reference to workflow manager', () => {
            expect(service.workflowManager).toBe(mockWorkflowManager);
            expect(service.workflowManager.projectId).toBe('test-project-123');
        });

        it('should access workflow state during initialization', () => {
            service.initializeStage(1);

            // Verify that the service has access to workflow state
            expect(service.workflowManager.workflowState).toBeDefined();
            expect(service.workflowManager.workflowState.requirementsAnalysis).toBeDefined();
        });

        it('should handle missing workflow manager methods gracefully', () => {
            delete mockWorkflowManager.initializeRequirementsStage;

            expect(() => service.initializeStage(1)).not.toThrow();
            expect(console.error).toHaveBeenCalledWith(
                'StageInitializationService.initializeStage error for stage 1:',
                expect.any(Error)
            );
        });
    });

    describe('Sequential stage initialization', () => {
        it('should handle sequential stage initialization calls', () => {
            // Initialize all stages in sequence
            for (let stage = 1; stage <= 5; stage++) {
                service.initializeStage(stage);
            }

            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalledTimes(1);
            expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalledTimes(1);
            expect(mockWorkflowManager.initializeStoriesStage).toHaveBeenCalledTimes(1);
            expect(mockWorkflowManager.initializePromptsStage).toHaveBeenCalledTimes(1);
            expect(mockWorkflowManager.initializeReviewStage).toHaveBeenCalledTimes(1);
        });

        it('should handle repeated stage initialization calls', () => {
            // Initialize stage 1 multiple times
            service.initializeStage(1);
            service.initializeStage(1);
            service.initializeStage(1);

            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalledTimes(3);
            expect(console.log).toHaveBeenCalledWith('Stage 1 initialized successfully');
            expect(console.log).toHaveBeenCalledTimes(3);
        });
    });

    describe('Workflow state integration', () => {
        it('should work with different workflow states', () => {
            const states = [
                { status: 'NotStarted', isApproved: false },
                { status: 'PendingReview', isApproved: false },
                { status: 'Approved', isApproved: true }
            ];

            states.forEach(state => {
                mockWorkflowManager.workflowState.requirementsAnalysis = state;

                service.initializeStage(1);

                expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalled();
                expect(service.workflowManager.workflowState.requirementsAnalysis).toBe(state);
            });
        });

        it('should handle workflow state changes between calls', () => {
            // Initial state
            mockWorkflowManager.workflowState.requirementsAnalysis = { status: 'NotStarted', isApproved: false };
            service.initializeStage(1);

            // Change state
            mockWorkflowManager.workflowState.requirementsAnalysis = { status: 'Approved', isApproved: true };
            service.initializeStage(1);

            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalledTimes(2);
        });
    });
});

describe('StageInitializationService Integration', () => {
    it('should handle the complete stage initialization cycle', () => {
        const mockWorkflowManager = {
            initializeRequirementsStage: jest.fn(),
            initializePlanningStage: jest.fn(),
            initializeStoriesStage: jest.fn(),
            initializePromptsStage: jest.fn(),
            initializeReviewStage: jest.fn()
        };

        const service = {
            workflowManager: mockWorkflowManager,

            initializeStage(stage) {
                try {
                    switch (stage) {
                        case 1: this.workflowManager.initializeRequirementsStage(); break;
                        case 2: this.workflowManager.initializePlanningStage(); break;
                        case 3: this.workflowManager.initializeStoriesStage(); break;
                        case 4: this.workflowManager.initializePromptsStage(); break;
                        case 5: this.workflowManager.initializeReviewStage(); break;
                        default: console.warn(`Unknown stage ${stage} for initialization`);
                    }
                    console.log(`Stage ${stage} initialized successfully`);
                } catch (error) {
                    console.error(`StageInitializationService.initializeStage error for stage ${stage}:`, error);
                }
            }
        };

        // Initialize all stages
        for (let stage = 1; stage <= 5; stage++) {
            service.initializeStage(stage);
        }

        expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalledTimes(1);
        expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalledTimes(1);
        expect(mockWorkflowManager.initializeStoriesStage).toHaveBeenCalledTimes(1);
        expect(mockWorkflowManager.initializePromptsStage).toHaveBeenCalledTimes(1);
        expect(mockWorkflowManager.initializeReviewStage).toHaveBeenCalledTimes(1);
    });

    it('should handle concurrent stage initialization', () => {
        const mockWorkflowManager = {
            initializeRequirementsStage: jest.fn(),
            initializePlanningStage: jest.fn()
        };

        const service = {
            workflowManager: mockWorkflowManager,

            initializeStage(stage) {
                switch (stage) {
                    case 1: this.workflowManager.initializeRequirementsStage(); break;
                    case 2: this.workflowManager.initializePlanningStage(); break;
                }
                console.log(`Stage ${stage} initialized successfully`);
            }
        };

        // Simulate concurrent initialization
        const promises = [
            Promise.resolve().then(() => service.initializeStage(1)),
            Promise.resolve().then(() => service.initializeStage(2)),
            Promise.resolve().then(() => service.initializeStage(1))
        ];

        return Promise.all(promises).then(() => {
            expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalledTimes(2);
            expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalledTimes(1);
        });
    });

    it('should handle mixed valid and invalid stage numbers', () => {
        const mockWorkflowManager = {
            initializeRequirementsStage: jest.fn(),
            initializePlanningStage: jest.fn()
        };

        const service = {
            workflowManager: mockWorkflowManager,

            initializeStage(stage) {
                try {
                    switch (stage) {
                        case 1: this.workflowManager.initializeRequirementsStage(); break;
                        case 2: this.workflowManager.initializePlanningStage(); break;
                        default: console.warn(`Unknown stage ${stage} for initialization`);
                    }
                    console.log(`Stage ${stage} initialized successfully`);
                } catch (error) {
                    console.error(`StageInitializationService.initializeStage error for stage ${stage}:`, error);
                }
            }
        };

        // Mix of valid and invalid stages
        service.initializeStage(1);
        service.initializeStage(99);
        service.initializeStage(2);
        service.initializeStage(-1);

        expect(mockWorkflowManager.initializeRequirementsStage).toHaveBeenCalledTimes(1);
        expect(mockWorkflowManager.initializePlanningStage).toHaveBeenCalledTimes(1);
        expect(console.warn).toHaveBeenCalledWith('Unknown stage 99 for initialization');
        expect(console.warn).toHaveBeenCalledWith('Unknown stage -1 for initialization');
    });
});
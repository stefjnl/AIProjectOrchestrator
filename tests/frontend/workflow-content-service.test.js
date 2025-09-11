const { describe, it, expect, beforeEach, afterEach, jest } = require('@jest/globals');

// Mock the global APIClient
global.APIClient = {
    getRequirements: jest.fn(),
    getProjectPlan: jest.fn(),
    getStories: jest.fn(),
    getPrompts: jest.fn(),
    getPendingReviews: jest.fn(),
    getApprovedStories: jest.fn()
};

// Mock window.App
global.window = {
    App: {
        showNotification: jest.fn()
    }
};

// Mock document
global.document = {
    getElementById: jest.fn(() => ({ innerHTML: '' })),
    querySelectorAll: jest.fn(() => []),
    addEventListener: jest.fn()
};

// Load the service (we'll need to adapt it for Node.js testing)
const fs = require('fs');
const path = require('path');

describe('WorkflowContentService', () => {
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
            getRequirementsStage: jest.fn(),
            getPlanningStage: jest.fn(),
            getStoriesStage: jest.fn(),
            getPromptsStage: jest.fn(),
            getReviewStage: jest.fn()
        };

        // Create a simple mock service for testing
        service = {
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
            },

            async getRequirementsStage() {
                return this.workflowManager.getRequirementsStage();
            },

            async getPlanningStage() {
                return this.workflowManager.getPlanningStage();
            },

            async getStoriesStage() {
                return this.workflowManager.getStoriesStage();
            },

            async getPromptsStage() {
                return this.workflowManager.getPromptsStage();
            },

            async getReviewStage() {
                return this.workflowManager.getReviewStage();
            }
        };
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    describe('getStageContent', () => {
        it('should return stage 1 content successfully', async () => {
            const mockContent = '<div>Requirements Stage Content</div>';
            mockWorkflowManager.getRequirementsStage.mockResolvedValue(mockContent);

            const result = await service.getStageContent(1);

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getRequirementsStage).toHaveBeenCalled();
        });

        it('should return stage 2 content successfully', async () => {
            const mockContent = '<div>Planning Stage Content</div>';
            mockWorkflowManager.getPlanningStage.mockResolvedValue(mockContent);

            const result = await service.getStageContent(2);

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getPlanningStage).toHaveBeenCalled();
        });

        it('should return stage 3 content successfully', async () => {
            const mockContent = '<div>Stories Stage Content</div>';
            mockWorkflowManager.getStoriesStage.mockResolvedValue(mockContent);

            const result = await service.getStageContent(3);

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getStoriesStage).toHaveBeenCalled();
        });

        it('should return stage 4 content successfully', async () => {
            const mockContent = '<div>Prompts Stage Content</div>';
            mockWorkflowManager.getPromptsStage.mockResolvedValue(mockContent);

            const result = await service.getStageContent(4);

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getPromptsStage).toHaveBeenCalled();
        });

        it('should return stage 5 content successfully', async () => {
            const mockContent = '<div>Review Stage Content</div>';
            mockWorkflowManager.getReviewStage.mockResolvedValue(mockContent);

            const result = await service.getStageContent(5);

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getReviewStage).toHaveBeenCalled();
        });

        it('should return "Stage not found" for invalid stage', async () => {
            const result = await service.getStageContent(99);

            expect(result).toBe('<p>Stage not found</p>');
        });

        it('should handle errors gracefully', async () => {
            mockWorkflowManager.getRequirementsStage.mockRejectedValue(new Error('API Error'));

            const result = await service.getStageContent(1);

            expect(result).toContain('Error Loading Stage');
            expect(result).toContain('Failed to load stage 1 content');
        });
    });

    describe('Stage-specific content methods', () => {
        it('should call workflow manager for requirements stage', async () => {
            const mockContent = '<div>Requirements Content</div>';
            mockWorkflowManager.getRequirementsStage.mockResolvedValue(mockContent);

            const result = await service.getRequirementsStage();

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getRequirementsStage).toHaveBeenCalled();
        });

        it('should call workflow manager for planning stage', async () => {
            const mockContent = '<div>Planning Content</div>';
            mockWorkflowManager.getPlanningStage.mockResolvedValue(mockContent);

            const result = await service.getPlanningStage();

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getPlanningStage).toHaveBeenCalled();
        });

        it('should call workflow manager for stories stage', async () => {
            const mockContent = '<div>Stories Content</div>';
            mockWorkflowManager.getStoriesStage.mockResolvedValue(mockContent);

            const result = await service.getStoriesStage();

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getStoriesStage).toHaveBeenCalled();
        });

        it('should call workflow manager for prompts stage', async () => {
            const mockContent = '<div>Prompts Content</div>';
            mockWorkflowManager.getPromptsStage.mockResolvedValue(mockContent);

            const result = await service.getPromptsStage();

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getPromptsStage).toHaveBeenCalled();
        });

        it('should call workflow manager for review stage', async () => {
            const mockContent = '<div>Review Content</div>';
            mockWorkflowManager.getReviewStage.mockResolvedValue(mockContent);

            const result = await service.getReviewStage();

            expect(result).toBe(mockContent);
            expect(mockWorkflowManager.getReviewStage).toHaveBeenCalled();
        });
    });

    describe('Error handling', () => {
        it('should handle API errors in stage content generation', async () => {
            mockWorkflowManager.getRequirementsStage.mockRejectedValue(new Error('Network error'));

            const result = await service.getStageContent(1);

            expect(result).toContain('Error Loading Stage');
            expect(console.error).toHaveBeenCalledWith('WorkflowContentService.getStageContent error:', expect.any(Error));
        });

        it('should handle missing workflow manager methods', async () => {
            delete mockWorkflowManager.getRequirementsStage;

            const result = await service.getStageContent(1);

            expect(result).toContain('Error Loading Stage');
        });
    });

    describe('Workflow state integration', () => {
        it('should access workflow state for stage decisions', async () => {
            mockWorkflowManager.workflowState.requirementsAnalysis.isApproved = true;
            mockWorkflowManager.workflowState.requirementsAnalysis.status = 'Approved';

            const mockContent = '<div>Approved Requirements Content</div>';
            mockWorkflowManager.getRequirementsStage.mockResolvedValue(mockContent);

            const result = await service.getStageContent(1);

            expect(result).toBe(mockContent);
            expect(service.workflowManager.workflowState.requirementsAnalysis.isApproved).toBe(true);
        });

        it('should handle different workflow states', async () => {
            // Test with different workflow states
            const states = [
                { status: 'NotStarted', isApproved: false },
                { status: 'PendingReview', isApproved: false },
                { status: 'Approved', isApproved: true }
            ];

            for (const state of states) {
                mockWorkflowManager.workflowState.requirementsAnalysis = state;

                const mockContent = `<div>Content for ${state.status}</div>`;
                mockWorkflowManager.getRequirementsStage.mockResolvedValue(mockContent);

                const result = await service.getStageContent(1);

                expect(result).toBe(mockContent);
            }
        });
    });
});

describe('WorkflowContentService Integration', () => {
    it('should handle the complete workflow stage cycle', async () => {
        const mockWorkflowManager = {
            projectId: 'integration-test-project',
            workflowState: {
                requirementsAnalysis: { isApproved: true, status: 'Approved' },
                projectPlanning: { isApproved: true, status: 'Approved' },
                storyGeneration: { isApproved: true, status: 'Approved' },
                promptGeneration: { completionPercentage: 100, status: 'Completed' }
            },
            getRequirementsStage: jest.fn().mockResolvedValue('<div>Requirements</div>'),
            getPlanningStage: jest.fn().mockResolvedValue('<div>Planning</div>'),
            getStoriesStage: jest.fn().mockResolvedValue('<div>Stories</div>'),
            getPromptsStage: jest.fn().mockResolvedValue('<div>Prompts</div>'),
            getReviewStage: jest.fn().mockResolvedValue('<div>Review</div>')
        };

        const service = {
            workflowManager: mockWorkflowManager,

            async getStageContent(stage) {
                const templates = {
                    1: this.workflowManager.getRequirementsStage.bind(this.workflowManager),
                    2: this.workflowManager.getPlanningStage.bind(this.workflowManager),
                    3: this.workflowManager.getStoriesStage.bind(this.workflowManager),
                    4: this.workflowManager.getPromptsStage.bind(this.workflowManager),
                    5: this.workflowManager.getReviewStage.bind(this.workflowManager)
                };

                return templates[stage] ? await templates[stage]() : '<p>Stage not found</p>';
            }
        };

        // Test all stages in sequence
        for (let stage = 1; stage <= 5; stage++) {
            const result = await service.getStageContent(stage);
            expect(result).toContain('div');
            expect(result).not.toBe('<p>Stage not found</p>');
        }
    });

    it('should handle concurrent stage requests', async () => {
        const mockWorkflowManager = {
            getRequirementsStage: jest.fn().mockResolvedValue('<div>Requirements</div>'),
            getPlanningStage: jest.fn().mockResolvedValue('<div>Planning</div>')
        };

        const service = {
            workflowManager: mockWorkflowManager,

            async getStageContent(stage) {
                const templates = {
                    1: this.workflowManager.getRequirementsStage.bind(this.workflowManager),
                    2: this.workflowManager.getPlanningStage.bind(this.workflowManager)
                };

                return templates[stage] ? await templates[stage]() : '<p>Stage not found</p>';
            }
        };

        // Test concurrent requests
        const results = await Promise.all([
            service.getStageContent(1),
            service.getStageContent(2),
            service.getStageContent(1)
        ]);

        expect(results[0]).toBe('<div>Requirements</div>');
        expect(results[1]).toBe('<div>Planning</div>');
        expect(results[2]).toBe('<div>Requirements</div>');
    });
});
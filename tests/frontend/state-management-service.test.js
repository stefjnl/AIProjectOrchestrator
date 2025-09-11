/**
 * StateManagementService Test Suite
 * 
 * Comprehensive tests for the StateManagementService including:
 * - State initialization and validation
 * - State updates and subscriptions
 * - Stage navigation and progression logic
 * - Project data management
 * - UI state management
 * - Error handling and edge cases
 * - Browser and Node.js compatibility
 */

// Mock APIClient for testing
const mockAPIClient = {
    saveWorkflowState: jest.fn().mockResolvedValue(true),
    getWorkflowState: jest.fn().mockResolvedValue(null)
};

// Mock global objects
global.APIClient = mockAPIClient;
global.localStorage = {
    getItem: jest.fn(),
    setItem: jest.fn(),
    removeItem: jest.fn()
};
global.console = {
    log: jest.fn(),
    warn: jest.fn(),
    error: jest.fn()
};

describe('StateManagementService', () => {
    let stateManager;
    let workflowManager;

    beforeEach(() => {
        // Reset mocks
        jest.clearAllMocks();

        // Create mock workflow manager
        workflowManager = {
            projectId: 'test-project-123',
            showNotification: jest.fn()
        };

        // Create fresh instance for each test
        stateManager = new StateManagementService(workflowManager);
    });

    afterEach(() => {
        if (stateManager) {
            stateManager.dispose();
        }
    });

    describe('Initialization', () => {
        test('should initialize with default state', () => {
            const state = stateManager.getState();

            expect(state).toBeDefined();
            expect(state.workflow.projectId).toBe('test-project-123');
            expect(state.navigation.currentStage).toBe(1);
            expect(state.ui.isNewProject).toBe(false);
            expect(state.project.data).toBeNull();
        });

        test('should initialize with correct default workflow state', () => {
            const workflow = stateManager.getWorkflowState();

            expect(workflow.requirementsAnalysis.status).toBe('NotStarted');
            expect(workflow.requirementsAnalysis.isApproved).toBe(false);
            expect(workflow.projectPlanning.status).toBe('NotStarted');
            expect(workflow.storyGeneration.status).toBe('NotStarted');
            expect(workflow.promptGeneration.status).toBe('NotStarted');
        });

        test('should initialize with empty subscribers', () => {
            expect(stateManager.getSubscriberCount()).toBe(0);
        });

        test('should initialize with empty history', () => {
            expect(stateManager.canUndo()).toBe(false);
            expect(stateManager.canRedo()).toBe(false);
        });
    });

    describe('State Management', () => {
        test('should get immutable state copy', () => {
            const state1 = stateManager.getState();
            const state2 = stateManager.getState();

            expect(state1).toEqual(state2);
            expect(state1).not.toBe(state2); // Should be different objects
        });

        test('should get specific state sections', () => {
            const workflow = stateManager.getWorkflowState();
            const navigation = stateManager.getNavigationState();
            const project = stateManager.getProjectState();
            const ui = stateManager.getUIState();

            expect(workflow).toBeDefined();
            expect(navigation).toBeDefined();
            expect(project).toBeDefined();
            expect(ui).toBeDefined();

            // Should be immutable copies
            expect(workflow).not.toBe(stateManager.state.workflow);
            expect(navigation).not.toBe(stateManager.state.navigation);
            expect(project).not.toBe(stateManager.state.project);
            expect(ui).not.toBe(stateManager.state.ui);
        });

        test('should set complete state with validation', () => {
            const newState = {
                workflow: {
                    projectId: 'test-project-123',
                    requirementsAnalysis: { status: 'Completed', isApproved: true },
                    projectPlanning: { status: 'Completed', isApproved: true },
                    storyGeneration: { status: 'Completed', isApproved: true },
                    promptGeneration: { status: 'Completed', isApproved: true }
                },
                navigation: {
                    currentStage: 3,
                    stages: ['requirements', 'planning', 'stories', 'prompts', 'review'],
                    isAutoRefreshing: false,
                    autoRefreshInterval: null
                },
                project: {
                    data: { name: 'Test Project' },
                    name: 'Test Project',
                    status: 'Active'
                },
                ui: {
                    isNewProject: false,
                    hasShownNewProjectPrompt: true,
                    loadingState: 'idle',
                    errorMessage: null,
                    notifications: []
                },
                cache: {
                    lastUpdated: new Date().toISOString(),
                    etag: 'test-etag',
                    isStale: false
                }
            };

            const result = stateManager.setState(newState);

            expect(result).toBe(true);
            const currentState = stateManager.getState();
            expect(currentState.workflow.requirementsAnalysis.isApproved).toBe(true);
            expect(currentState.navigation.currentStage).toBe(3);
            expect(currentState.project.name).toBe('Test Project');
        });

        test('should update specific state sections', () => {
            // Update workflow state
            const workflowUpdate = {
                requirementsAnalysis: { status: 'PendingReview', isApproved: false }
            };
            const workflowResult = stateManager.updateWorkflowState(workflowUpdate);
            expect(workflowResult).toBe(true);

            const workflow = stateManager.getWorkflowState();
            expect(workflow.requirementsAnalysis.status).toBe('PendingReview');

            // Update navigation state
            const navResult = stateManager.updateNavigationState({ currentStage: 2 });
            expect(navResult).toBe(true);

            const navigation = stateManager.getNavigationState();
            expect(navigation.currentStage).toBe(2);

            // Update project state
            const projectResult = stateManager.updateProjectState({ name: 'Updated Project' });
            expect(projectResult).toBe(true);

            const project = stateManager.getProjectState();
            expect(project.name).toBe('Updated Project');

            // Update UI state
            const uiResult = stateManager.updateUIState({ isNewProject: true });
            expect(uiResult).toBe(true);

            const ui = stateManager.getUIState();
            expect(ui.isNewProject).toBe(true);
        });

        test('should handle invalid state sections gracefully', () => {
            const result = stateManager.updateStateSection('invalid', { test: true });
            expect(result).toBe(false);
        });

        test('should validate state on updates', () => {
            const invalidWorkflow = {
                requirementsAnalysis: { status: 'InvalidStatus', isApproved: 'not-boolean' }
            };

            const result = stateManager.updateWorkflowState(invalidWorkflow);
            expect(result).toBe(false);

            // State should remain unchanged
            const workflow = stateManager.getWorkflowState();
            expect(workflow.requirementsAnalysis.isApproved).toBe(false);
        });
    });

    describe('Stage Navigation Logic', () => {
        beforeEach(() => {
            // Set up a workflow with some approvals
            stateManager.updateWorkflowState({
                requirementsAnalysis: { status: 'Completed', isApproved: true },
                projectPlanning: { status: 'Completed', isApproved: true },
                storyGeneration: { status: 'NotStarted', isApproved: false },
                promptGeneration: { status: 'NotStarted', isApproved: false }
            });
        });

        test('should get current stage', () => {
            expect(stateManager.getCurrentStage()).toBe(1);

            stateManager.setCurrentStage(3);
            expect(stateManager.getCurrentStage()).toBe(3);
        });

        test('should validate stage access', () => {
            expect(stateManager.canAccessStage(1)).toBe(true);
            expect(stateManager.canAccessStage(2)).toBe(true);
            expect(stateManager.canAccessStage(3)).toBe(true);
            expect(stateManager.canAccessStage(4)).toBe(false); // Stories not approved
            expect(stateManager.canAccessStage(5)).toBe(false); // Stories not approved
        });

        test('should validate stage progression', () => {
            stateManager.setCurrentStage(1);
            expect(stateManager.canProgressToNextStage()).toBe(true); // Requirements approved

            stateManager.setCurrentStage(2);
            expect(stateManager.canProgressToNextStage()).toBe(true); // Planning approved

            stateManager.setCurrentStage(3);
            expect(stateManager.canProgressToNextStage()).toBe(false); // Stories not approved

            stateManager.setCurrentStage(4);
            expect(stateManager.canProgressToNextStage()).toBe(false); // Prompts not completed

            stateManager.setCurrentStage(5);
            expect(stateManager.canProgressToNextStage()).toBe(true); // Always true for final stage
        });

        test('should get highest accessible stage', () => {
            expect(stateManager.getHighestAccessibleStage()).toBe(3); // Stories stage

            // Approve stories
            stateManager.updateWorkflowState({
                storyGeneration: { status: 'Completed', isApproved: true }
            });

            expect(stateManager.getHighestAccessibleStage()).toBe(4); // Prompts stage

            // Complete prompts
            stateManager.updateWorkflowState({
                promptGeneration: { status: 'Completed', completionPercentage: 100 }
            });

            expect(stateManager.getHighestAccessibleStage()).toBe(5); // Review stage
        });

        test('should determine current stage from workflow', () => {
            expect(stateManager.getCurrentStageFromWorkflow()).toBe(3); // First incomplete stage

            // Test new project scenario
            stateManager.setNewProjectFlag(true);
            expect(stateManager.getCurrentStageFromWorkflow()).toBe(1); // Force stage 1 for new projects
        });

        test('should handle new project scenario in stage determination', () => {
            stateManager.setNewProjectFlag(true);

            // Even with approvals, should return stage 1 for new projects
            expect(stateManager.getCurrentStageFromWorkflow()).toBe(1);
        });
    });

    describe('Progress Calculation', () => {
        test('should calculate 0% progress with no approvals', () => {
            expect(stateManager.calculateProgress()).toBe(0);
        });

        test('should calculate 20% progress with requirements approved', () => {
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });
            expect(stateManager.calculateProgress()).toBe(20);
        });

        test('should calculate 40% progress with requirements and planning approved', () => {
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true },
                projectPlanning: { isApproved: true }
            });
            expect(stateManager.calculateProgress()).toBe(40);
        });

        test('should calculate 60% progress with first 3 stages approved', () => {
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true },
                projectPlanning: { isApproved: true },
                storyGeneration: { isApproved: true }
            });
            expect(stateManager.calculateProgress()).toBe(60);
        });

        test('should calculate 80% progress with prompts completed', () => {
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true },
                projectPlanning: { isApproved: true },
                storyGeneration: { isApproved: true },
                promptGeneration: { completionPercentage: 100 }
            });
            expect(stateManager.calculateProgress()).toBe(80);
        });

        test('should calculate 100% progress with all stages complete', () => {
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true },
                projectPlanning: { isApproved: true },
                storyGeneration: { isApproved: true },
                promptGeneration: { completionPercentage: 100 }
            });
            expect(stateManager.calculateProgress()).toBe(100);
        });
    });

    describe('Project Data Management', () => {
        test('should set and get project data', () => {
            const mockProject = {
                id: 'test-project-123',
                name: 'Test Project',
                status: 'Active',
                description: 'Test description'
            };

            const result = stateManager.setProjectData(mockProject);
            expect(result).toBe(true);

            const retrievedData = stateManager.getProjectData();
            expect(retrievedData).toEqual(mockProject);
        });

        test('should update project overview', () => {
            const project = {
                name: 'Updated Project',
                status: 'In Progress',
                createdAt: '2023-01-01T00:00:00Z',
                description: 'Updated description',
                techStack: 'React, Node.js',
                timeline: '6 months'
            };

            const result = stateManager.updateProjectOverview(project);
            expect(result).toBe(true);

            const projectState = stateManager.getProjectState();
            expect(projectState.name).toBe('Updated Project');
            expect(projectState.status).toBe('In Progress');
            expect(projectState.description).toBe('Updated description');
            expect(projectState.techStack).toBe('React, Node.js');
            expect(projectState.timeline).toBe('6 months');
        });

        test('should handle null/undefined project data gracefully', () => {
            const result = stateManager.setProjectData(null);
            expect(result).toBe(true);

            const retrievedData = stateManager.getProjectData();
            expect(retrievedData).toBeNull();
        });
    });

    describe('UI State Management', () => {
        test('should set loading state', () => {
            const result = stateManager.setLoadingState('loading', 'Loading project data...');
            expect(result).toBe(true);

            const ui = stateManager.getUIState();
            expect(ui.loadingState).toBe('loading');
            expect(ui.errorMessage).toBeNull(); // No error message for loading state
        });

        test('should set error state', () => {
            const errorMessage = 'Failed to load project data';
            const result = stateManager.setError(errorMessage);
            expect(result).toBe(true);

            const ui = stateManager.getUIState();
            expect(ui.loadingState).toBe('error');
            expect(ui.errorMessage).toBe(errorMessage);
        });

        test('should clear error', () => {
            // First set an error
            stateManager.setError('Test error');

            // Then clear it
            const result = stateManager.clearError();
            expect(result).toBe(true);

            const ui = stateManager.getUIState();
            expect(ui.loadingState).toBe('idle');
            expect(ui.errorMessage).toBeNull();
        });

        test('should set new project flags', () => {
            const result1 = stateManager.setNewProjectFlag(true);
            expect(result1).toBe(true);

            let ui = stateManager.getUIState();
            expect(ui.isNewProject).toBe(true);

            const result2 = stateManager.setNewProjectPromptShown(true);
            expect(result2).toBe(true);

            ui = stateManager.getUIState();
            expect(ui.hasShownNewProjectPrompt).toBe(true);
        });
    });

    describe('Auto-refresh State Management', () => {
        test('should manage auto-refresh state', () => {
            const result = stateManager.setAutoRefreshState(true, 'interval-123');
            expect(result).toBe(true);

            expect(stateManager.isAutoRefreshing()).toBe(true);

            const navigation = stateManager.getNavigationState();
            expect(navigation.isAutoRefreshing).toBe(true);
            expect(navigation.autoRefreshInterval).toBe('interval-123');
        });

        test('should stop auto-refresh', () => {
            stateManager.setAutoRefreshState(true, 'interval-123');

            const result = stateManager.setAutoRefreshState(false, null);
            expect(result).toBe(true);

            expect(stateManager.isAutoRefreshing()).toBe(false);

            const navigation = stateManager.getNavigationState();
            expect(navigation.isAutoRefreshing).toBe(false);
            expect(navigation.autoRefreshInterval).toBeNull();
        });
    });

    describe('Observer Pattern (Subscriptions)', () => {
        test('should subscribe to state changes', () => {
            const callback = jest.fn();
            const unsubscribe = stateManager.subscribe(callback);

            expect(stateManager.getSubscriberCount()).toBe(1);
            expect(typeof unsubscribe).toBe('function');
        });

        test('should notify subscribers on state changes', () => {
            const callback = jest.fn();
            stateManager.subscribe(callback);

            stateManager.updateWorkflowState({
                requirementsAnalysis: { status: 'PendingReview' }
            });

            expect(callback).toHaveBeenCalledTimes(1);
            expect(callback).toHaveBeenCalledWith(expect.objectContaining({
                workflow: expect.objectContaining({
                    requirementsAnalysis: expect.objectContaining({
                        status: 'PendingReview'
                    })
                })
            }));
        });

        test('should notify subscribers for specific section changes', () => {
            const workflowCallback = jest.fn();
            const navCallback = jest.fn();

            stateManager.subscribe(workflowCallback, 'workflowChanged');
            stateManager.subscribe(navCallback, 'navigationChanged');

            // Update workflow - should trigger workflow callback
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });

            expect(workflowCallback).toHaveBeenCalledTimes(1);
            expect(navCallback).toHaveBeenCalledTimes(0);

            // Update navigation - should trigger navigation callback
            stateManager.updateNavigationState({ currentStage: 2 });

            expect(workflowCallback).toHaveBeenCalledTimes(1);
            expect(navCallback).toHaveBeenCalledTimes(1);
        });

        test('should unsubscribe from events', () => {
            const callback = jest.fn();
            const unsubscribe = stateManager.subscribe(callback);

            // Update state - should notify
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });
            expect(callback).toHaveBeenCalledTimes(1);

            // Unsubscribe
            unsubscribe();
            expect(stateManager.getSubscriberCount()).toBe(0);

            // Update state again - should not notify
            stateManager.updateWorkflowState({
                projectPlanning: { isApproved: true }
            });
            expect(callback).toHaveBeenCalledTimes(1); // Still 1, no additional calls
        });

        test('should handle subscriber errors gracefully', () => {
            const errorCallback = jest.fn(() => {
                throw new Error('Subscriber error');
            });
            const goodCallback = jest.fn();

            stateManager.subscribe(errorCallback);
            stateManager.subscribe(goodCallback);

            // Should not throw, but should log error
            expect(() => {
                stateManager.updateWorkflowState({
                    requirementsAnalysis: { isApproved: true }
                });
            }).not.toThrow();

            // Good callback should still be called
            expect(goodCallback).toHaveBeenCalledTimes(1);

            // Error should be logged
            expect(console.error).toHaveBeenCalled();
        });
    });

    describe('State History (Undo/Redo)', () => {
        test('should save state to history on changes', () => {
            expect(stateManager.canUndo()).toBe(false);

            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });

            expect(stateManager.canUndo()).toBe(true);
            expect(stateManager.canRedo()).toBe(false);
        });

        test('should support undo operation', () => {
            // Make a change
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });

            // Undo should work
            expect(stateManager.canUndo()).toBe(true);
            const undoResult = stateManager.undo();
            expect(undoResult).toBe(true);

            // After undo, should be able to redo
            expect(stateManager.canUndo()).toBe(false);
            expect(stateManager.canRedo()).toBe(true);
        });

        test('should support redo operation', () => {
            // Make a change
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });

            // Undo
            stateManager.undo();

            // Redo should work
            expect(stateManager.canRedo()).toBe(true);
            const redoResult = stateManager.redo();
            expect(redoResult).toBe(true);

            // After redo, should not be able to redo again
            expect(stateManager.canRedo()).toBe(false);
            expect(stateManager.canUndo()).toBe(true);
        });

        test('should limit history size', () => {
            // Make many changes to exceed max history size
            for (let i = 0; i < 60; i++) {
                stateManager.updateUIState({
                    notifications: [`Notification ${i}`]
                });
            }

            // Should have limited history
            expect(stateManager.stateHistory.length).toBeLessThanOrEqual(50);
        });
    });

    describe('State Persistence', () => {
        beforeEach(() => {
            localStorage.getItem.mockClear();
            localStorage.setItem.mockClear();
            mockAPIClient.saveWorkflowState.mockClear();
            mockAPIClient.getWorkflowState.mockClear();
        });

        test('should enable and disable auto-save', () => {
            stateManager.enableAutoSave(true);
            expect(stateManager.autoSaveEnabled).toBe(true);

            stateManager.enableAutoSave(false);
            expect(stateManager.autoSaveEnabled).toBe(false);
        });

        test('should save state to localStorage when API not available', async () => {
            // Make APIClient unavailable
            global.APIClient = undefined;

            const result = await stateManager.saveState();
            expect(result).toBe(true);

            expect(localStorage.setItem).toHaveBeenCalledWith(
                'workflow_state_test-project-123',
                expect.stringContaining('test-project-123')
            );

            // Restore APIClient
            global.APIClient = mockAPIClient;
        });

        test('should save state to API when available', async () => {
            const result = await stateManager.saveState();
            expect(result).toBe(true);

            expect(mockAPIClient.saveWorkflowState).toHaveBeenCalledWith(
                'test-project-123',
                expect.objectContaining({
                    workflow: expect.any(Object),
                    navigation: expect.any(Object),
                    project: expect.any(Object),
                    ui: expect.any(Object),
                    cache: expect.any(Object)
                })
            );
        });

        test('should load state from localStorage when API not available', async () => {
            // Make APIClient unavailable
            global.APIClient = undefined;

            const mockSavedState = JSON.stringify({
                workflow: { projectId: 'test-project-123' },
                navigation: { currentStage: 2 },
                project: { name: 'Loaded Project' },
                ui: { isNewProject: false },
                cache: { lastUpdated: new Date().toISOString() }
            });

            localStorage.getItem.mockReturnValue(mockSavedState);

            const result = await stateManager.loadState();
            expect(result).toBe(true);

            expect(localStorage.getItem).toHaveBeenCalledWith('workflow_state_test-project-123');

            const state = stateManager.getState();
            expect(state.navigation.currentStage).toBe(2);
            expect(state.project.name).toBe('Loaded Project');

            // Restore APIClient
            global.APIClient = mockAPIClient;
        });

        test('should load state from API when available', async () => {
            const mockSavedState = {
                workflow: { projectId: 'test-project-123' },
                navigation: { currentStage: 3 },
                project: { name: 'API Loaded Project' },
                ui: { isNewProject: false },
                cache: { lastUpdated: new Date().toISOString() }
            };

            mockAPIClient.getWorkflowState.mockResolvedValue(mockSavedState);

            const result = await stateManager.loadState();
            expect(result).toBe(true);

            expect(mockAPIClient.getWorkflowState).toHaveBeenCalledWith('test-project-123');

            const state = stateManager.getState();
            expect(state.navigation.currentStage).toBe(3);
            expect(state.project.name).toBe('API Loaded Project');
        });
    });

    describe('State Validation', () => {
        test('should validate complete state structure', () => {
            const validState = stateManager.getState();
            expect(() => stateManager.validateState(validState)).not.toThrow();
        });

        test('should reject invalid state structure', () => {
            expect(() => stateManager.validateState(null)).toThrow('State must be an object');
            expect(() => stateManager.validateState('invalid')).toThrow('State must be an object');
            expect(() => stateManager.validateState(123)).toThrow('State must be an object');
        });

        test('should reject missing required sections', () => {
            const invalidState = { workflow: {}, navigation: {}, project: {} }; // Missing ui and cache
            expect(() => stateManager.validateState(invalidState)).toThrow('Missing required state section: ui');
        });

        test('should validate workflow state', () => {
            const invalidWorkflow = { projectId: 'test' }; // Missing required fields
            expect(() => stateManager.validateWorkflowState(invalidWorkflow)).toThrow();
        });

        test('should validate navigation state', () => {
            const invalidNavigation = { currentStage: 6 }; // Invalid stage number
            expect(() => stateManager.validateNavigationState(invalidNavigation)).toThrow('Navigation.currentStage must be a number between 1 and 5');
        });

        test('should validate project state', () => {
            const invalidProject = { name: 123 }; // Name should be string
            expect(() => stateManager.validateProjectState(invalidProject)).toThrow('Project.name must be a string or null');
        });

        test('should validate UI state', () => {
            const invalidUI = { isNewProject: 'not-boolean' }; // Should be boolean
            expect(() => stateManager.validateUIState(invalidUI)).toThrow('UI.isNewProject must be a boolean');
        });
    });

    describe('Utility Methods', () => {
        test('should reset state to defaults', () => {
            // Make some changes
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });
            stateManager.setCurrentStage(3);

            // Reset
            const result = stateManager.resetState();
            expect(result).toBe(true);

            const state = stateManager.getState();
            expect(state.workflow.requirementsAnalysis.isApproved).toBe(false);
            expect(state.navigation.currentStage).toBe(1);
            expect(state.projectId).toBe('test-project-123'); // Preserved
        });

        test('should provide state summary', () => {
            stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: true }
            });
            stateManager.setCurrentStage(2);
            stateManager.setNewProjectFlag(true);

            const summary = stateManager.getStateSummary();

            expect(summary).toHaveProperty('currentStage', 2);
            expect(summary).toHaveProperty('isNewProject', true);
            expect(summary).toHaveProperty('workflowProgress', 20);
            expect(summary).toHaveProperty('subscriberCount', 0);
            expect(summary).toHaveProperty('lastUpdated');
        });

        test('should count subscribers correctly', () => {
            expect(stateManager.getSubscriberCount()).toBe(0);

            const unsub1 = stateManager.subscribe(() => { });
            expect(stateManager.getSubscriberCount()).toBe(1);

            const unsub2 = stateManager.subscribe(() => { }, 'workflowChanged');
            expect(stateManager.getSubscriberCount()).toBe(2);

            unsub1();
            expect(stateManager.getSubscriberCount()).toBe(1);

            unsub2();
            expect(stateManager.getSubscriberCount()).toBe(0);
        });

        test('should perform deep merge correctly', () => {
            const target = {
                workflow: { requirementsAnalysis: { status: 'Old' } },
                navigation: { currentStage: 1 }
            };

            const source = {
                workflow: { requirementsAnalysis: { status: 'New' } }
            };

            const result = stateManager.deepMerge(target, source);

            expect(result.workflow.requirementsAnalysis.status).toBe('New');
            expect(result.navigation.currentStage).toBe(1); // Preserved
        });
    });

    describe('Error Handling', () => {
        test('should handle invalid stage numbers gracefully', () => {
            expect(() => stateManager.setCurrentStage(0)).toThrow('Invalid stage: 0');
            expect(() => stateManager.setCurrentStage(6)).toThrow('Invalid stage: 6');
        });

        test('should handle state update errors gracefully', () => {
            const result = stateManager.updateWorkflowState({
                requirementsAnalysis: { isApproved: 'not-boolean' } // Invalid type
            });

            expect(result).toBe(false);
            expect(console.error).toHaveBeenCalled();
        });

        test('should handle API errors gracefully', async () => {
            mockAPIClient.saveWorkflowState.mockRejectedValue(new Error('API Error'));

            const result = await stateManager.saveState();
            expect(result).toBe(false);
            expect(console.error).toHaveBeenCalled();
        });

        test('should handle subscriber errors gracefully', () => {
            const errorCallback = jest.fn(() => {
                throw new Error('Subscriber error');
            });

            stateManager.subscribe(errorCallback);

            // Should not throw
            expect(() => {
                stateManager.updateWorkflowState({
                    requirementsAnalysis: { isApproved: true }
                });
            }).not.toThrow();

            expect(console.error).toHaveBeenCalled();
        });
    });

    describe('Browser and Node.js Compatibility', () => {
        test('should work without localStorage', () => {
            const originalLocalStorage = global.localStorage;
            global.localStorage = undefined;

            // Should not throw
            expect(() => {
                stateManager.saveState();
                stateManager.loadState();
            }).not.toThrow();

            global.localStorage = originalLocalStorage;
        });

        test('should work without APIClient', () => {
            const originalAPIClient = global.APIClient;
            global.APIClient = undefined;

            // Should not throw
            expect(() => {
                stateManager.saveState();
                stateManager.loadState();
            }).not.toThrow();

            global.APIClient = originalAPIClient;
        });

        test('should export correctly for different environments', () => {
            // Test module exports
            expect(typeof StateManagementService).toBe('function');

            // In browser environment, should be available on window
            if (typeof window !== 'undefined') {
                expect(window.StateManagementService).toBe(StateManagementService);
            }
        });
    });

    describe('Performance and Edge Cases', () => {
        test('should handle rapid state updates efficiently', () => {
            const startTime = Date.now();

            // Perform many rapid updates
            for (let i = 0; i < 100; i++) {
                stateManager.updateUIState({
                    notifications: [`Notification ${i}`]
                });
            }

            const endTime = Date.now();
            const duration = endTime - startTime;

            // Should complete in reasonable time (less than 1 second)
            expect(duration).toBeLessThan(1000);

            const state = stateManager.getState();
            expect(state.ui.notifications).toHaveLength(1);
            expect(state.ui.notifications[0]).toBe('Notification 99');
        });

        test('should handle large state objects', () => {
            const largeProjectData = {
                name: 'Large Project',
                description: 'A'.repeat(10000), // Large description
                components: Array.from({ length: 1000 }, (_, i) => ({
                    id: i,
                    name: `Component ${i}`,
                    description: `Description for component ${i}`
                }))
            };

            const result = stateManager.setProjectData(largeProjectData);
            expect(result).toBe(true);

            const retrievedData = stateManager.getProjectData();
            expect(retrievedData.name).toBe('Large Project');
            expect(retrievedData.description).toHaveLength(10000);
            expect(retrievedData.components).toHaveLength(1000);
        });

        test('should handle circular references in state validation', () => {
            const circularState = stateManager.getState();
            circularState.workflow.circular = circularState; // Create circular reference

            // Should handle gracefully (JSON.stringify will throw, but validation should catch it)
            expect(() => {
                stateManager.validateState(circularState);
            }).toThrow(); // Will throw due to JSON.stringify in deep merge
        });

        test('should handle concurrent state updates', async () => {
            const promises = [];
            const results = [];

            // Simulate concurrent updates
            for (let i = 0; i < 10; i++) {
                promises.push(
                    stateManager.updateUIState({
                        notifications: [`Concurrent ${i}`]
                    }).then(result => results.push(result))
                );
            }

            await Promise.all(promises);

            // All updates should succeed
            expect(results.every(result => result === true)).toBe(true);
        });
    });

    describe('Cleanup and Disposal', () => {
        test('should clean up resources on disposal', () => {
            const callback = jest.fn();
            stateManager.subscribe(callback);

            expect(stateManager.getSubscriberCount()).toBe(1);

            stateManager.dispose();

            expect(stateManager.getSubscriberCount()).toBe(0);
            expect(stateManager.subscribers.size).toBe(0);
            expect(stateManager.stateHistory).toHaveLength(0);
            expect(stateManager.historyIndex).toBe(-1);
        });

        test('should cancel auto-save on disposal', () => {
            stateManager.enableAutoSave(true);

            const cancelAutoSaveSpy = jest.spyOn(stateManager, 'cancelAutoSave');

            stateManager.dispose();

            expect(cancelAutoSaveSpy).toHaveBeenCalled();
        });
    });
});
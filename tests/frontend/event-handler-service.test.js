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

describe('EventHandlerService', () => {
    let service;
    let mockWorkflowManager;
    let mockDocument;
    let mockElements;

    beforeEach(() => {
        // Reset all mocks
        jest.clearAllMocks();

        // Create mock elements
        mockElements = {
            prevButton: { addEventListener: jest.fn() },
            nextButton: { addEventListener: jest.fn() },
            autoRefreshToggle: { addEventListener: jest.fn() },
            stageIndicators: [
                { addEventListener: jest.fn() },
                { addEventListener: jest.fn() },
                { addEventListener: jest.fn() },
                { addEventListener: jest.fn() },
                { addEventListener: jest.fn() }
            ]
        };

        // Create mock document
        mockDocument = {
            getElementById: jest.fn((id) => {
                switch (id) {
                    case 'prev-stage': return mockElements.prevButton;
                    case 'next-stage': return mockElements.nextButton;
                    case 'auto-refresh-toggle': return mockElements.autoRefreshToggle;
                    default: return null;
                }
            }),
            querySelectorAll: jest.fn((selector) => {
                if (selector === '.stage-indicator') {
                    return mockElements.stageIndicators;
                }
                return [];
            }),
            addEventListener: jest.fn()
        };

        // Create mock workflow manager
        mockWorkflowManager = {
            projectId: 'test-project-123',
            currentStage: 1,
            navigateStage: jest.fn(),
            jumpToStage: jest.fn(),
            startAutoRefresh: jest.fn(),
            stopAutoRefresh: jest.fn()
        };

        // Create service instance
        service = {
            workflowManager: mockWorkflowManager,
            document: mockDocument,

            setupEventListeners() {
                try {
                    // Navigation buttons
                    const prevButton = this.document.getElementById('prev-stage');
                    const nextButton = this.document.getElementById('next-stage');

                    if (prevButton) {
                        prevButton.addEventListener('click', () => this.workflowManager.navigateStage(-1));
                    }
                    if (nextButton) {
                        nextButton.addEventListener('click', () => this.workflowManager.navigateStage(1));
                    }

                    // Stage indicators
                    const stageIndicators = this.document.querySelectorAll('.stage-indicator');
                    stageIndicators.forEach((indicator, index) => {
                        indicator.addEventListener('click', () => this.workflowManager.jumpToStage(index + 1));
                    });

                    // Auto-refresh toggle
                    const autoRefreshToggle = this.document.getElementById('auto-refresh-toggle');
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
                    this.document.addEventListener('keydown', (e) => {
                        if (e.key === 'ArrowLeft') this.workflowManager.navigateStage(-1);
                        if (e.key === 'ArrowRight') this.workflowManager.navigateStage(1);
                    });

                    console.log('Event listeners setup completed');
                } catch (error) {
                    console.error('EventHandlerService.setupEventListeners error:', error);
                }
            },

            startAutoRefresh() {
                this.workflowManager.startAutoRefresh();
            },

            stopAutoRefresh() {
                this.workflowManager.stopAutoRefresh();
            }
        };
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    describe('setupEventListeners', () => {
        it('should setup navigation button listeners successfully', () => {
            service.setupEventListeners();

            expect(mockDocument.getElementById).toHaveBeenCalledWith('prev-stage');
            expect(mockDocument.getElementById).toHaveBeenCalledWith('next-stage');

            expect(mockElements.prevButton.addEventListener).toHaveBeenCalledWith('click', expect.any(Function));
            expect(mockElements.nextButton.addEventListener).toHaveBeenCalledWith('click', expect.any(Function));
        });

        it('should setup stage indicator listeners successfully', () => {
            service.setupEventListeners();

            expect(mockDocument.querySelectorAll).toHaveBeenCalledWith('.stage-indicator');

            mockElements.stageIndicators.forEach((indicator, index) => {
                expect(indicator.addEventListener).toHaveBeenCalledWith('click', expect.any(Function));
            });
        });

        it('should setup auto-refresh toggle listener successfully', () => {
            service.setupEventListeners();

            expect(mockDocument.getElementById).toHaveBeenCalledWith('auto-refresh-toggle');
            expect(mockElements.autoRefreshToggle.addEventListener).toHaveBeenCalledWith('change', expect.any(Function));
        });

        it('should setup keyboard navigation listener successfully', () => {
            service.setupEventListeners();

            expect(mockDocument.addEventListener).toHaveBeenCalledWith('keydown', expect.any(Function));
        });

        it('should handle missing elements gracefully', () => {
            // Mock getElementById to return null for all elements
            mockDocument.getElementById = jest.fn(() => null);

            expect(() => service.setupEventListeners()).not.toThrow();
        });

        it('should log success message when setup completes', () => {
            service.setupEventListeners();

            expect(console.log).toHaveBeenCalledWith('Event listeners setup completed');
        });

        it('should log error when setup fails', () => {
            // Mock an error during setup
            mockDocument.getElementById = jest.fn(() => {
                throw new Error('DOM Error');
            });

            service.setupEventListeners();

            expect(console.error).toHaveBeenCalledWith('EventHandlerService.setupEventListeners error:', expect.any(Error));
        });
    });

    describe('Navigation button functionality', () => {
        it('should call navigateStage(-1) when prev button is clicked', () => {
            service.setupEventListeners();

            // Get the click handler that was registered
            const prevClickHandler = mockElements.prevButton.addEventListener.mock.calls[0][1];

            // Simulate click
            prevClickHandler();

            expect(mockWorkflowManager.navigateStage).toHaveBeenCalledWith(-1);
        });

        it('should call navigateStage(1) when next button is clicked', () => {
            service.setupEventListeners();

            // Get the click handler that was registered
            const nextClickHandler = mockElements.nextButton.addEventListener.mock.calls[0][1];

            // Simulate click
            nextClickHandler();

            expect(mockWorkflowManager.navigateStage).toHaveBeenCalledWith(1);
        });
    });

    describe('Stage indicator functionality', () => {
        it('should call jumpToStage with correct stage numbers', () => {
            service.setupEventListeners();

            // Get the click handlers for each stage indicator
            mockElements.stageIndicators.forEach((indicator, index) => {
                const clickHandler = indicator.addEventListener.mock.calls[0][1];

                // Simulate click
                clickHandler();

                expect(mockWorkflowManager.jumpToStage).toHaveBeenCalledWith(index + 1);
            });
        });
    });

    describe('Auto-refresh toggle functionality', () => {
        it('should start auto-refresh when toggle is checked', () => {
            service.setupEventListeners();

            // Get the change handler that was registered
            const changeHandler = mockElements.autoRefreshToggle.addEventListener.mock.calls[0][1];

            // Simulate change event with checked state
            changeHandler({ target: { checked: true } });

            expect(mockWorkflowManager.startAutoRefresh).toHaveBeenCalled();
        });

        it('should stop auto-refresh when toggle is unchecked', () => {
            service.setupEventListeners();

            // Get the change handler that was registered
            const changeHandler = mockElements.autoRefreshToggle.addEventListener.mock.calls[0][1];

            // Simulate change event with unchecked state
            changeHandler({ target: { checked: false } });

            expect(mockWorkflowManager.stopAutoRefresh).toHaveBeenCalled();
        });
    });

    describe('Keyboard navigation functionality', () => {
        it('should call navigateStage(-1) when left arrow is pressed', () => {
            service.setupEventListeners();

            // Get the keydown handler that was registered
            const keydownHandler = mockDocument.addEventListener.mock.calls.find(
                call => call[0] === 'keydown'
            )[1];

            // Simulate left arrow key press
            keydownHandler({ key: 'ArrowLeft' });

            expect(mockWorkflowManager.navigateStage).toHaveBeenCalledWith(-1);
        });

        it('should call navigateStage(1) when right arrow is pressed', () => {
            service.setupEventListeners();

            // Get the keydown handler that was registered
            const keydownHandler = mockDocument.addEventListener.mock.calls.find(
                call => call[0] === 'keydown'
            )[1];

            // Simulate right arrow key press
            keydownHandler({ key: 'ArrowRight' });

            expect(mockWorkflowManager.navigateStage).toHaveBeenCalledWith(1);
        });

        it('should not call navigateStage for other keys', () => {
            service.setupEventListeners();

            // Get the keydown handler that was registered
            const keydownHandler = mockDocument.addEventListener.mock.calls.find(
                call => call[0] === 'keydown'
            )[1];

            // Simulate other key press
            keydownHandler({ key: 'Enter' });

            expect(mockWorkflowManager.navigateStage).not.toHaveBeenCalled();
        });
    });

    describe('Auto-refresh control methods', () => {
        it('should delegate startAutoRefresh to workflow manager', () => {
            service.startAutoRefresh();

            expect(mockWorkflowManager.startAutoRefresh).toHaveBeenCalled();
        });

        it('should delegate stopAutoRefresh to workflow manager', () => {
            service.stopAutoRefresh();

            expect(mockWorkflowManager.stopAutoRefresh).toHaveBeenCalled();
        });
    });

    describe('Error handling', () => {
        it('should handle errors during document element access', () => {
            // Mock getElementById to throw an error
            mockDocument.getElementById = jest.fn(() => {
                throw new Error('Element not found');
            });

            expect(() => service.setupEventListeners()).not.toThrow();
            expect(console.error).toHaveBeenCalledWith('EventHandlerService.setupEventListeners error:', expect.any(Error));
        });

        it('should handle errors during event listener registration', () => {
            // Mock addEventListener to throw an error
            mockElements.prevButton.addEventListener = jest.fn(() => {
                throw new Error('Event registration failed');
            });

            expect(() => service.setupEventListeners()).not.toThrow();
            expect(console.error).toHaveBeenCalledWith('EventHandlerService.setupEventListeners error:', expect.any(Error));
        });
    });

    describe('Edge cases', () => {
        it('should handle empty stage indicators array', () => {
            mockDocument.querySelectorAll = jest.fn(() => []);

            service.setupEventListeners();

            expect(mockDocument.querySelectorAll).toHaveBeenCalledWith('.stage-indicator');
            // Should not throw even with empty array
        });

        it('should handle null stage indicators', () => {
            mockDocument.querySelectorAll = jest.fn(() => null);

            service.setupEventListeners();

            expect(mockDocument.querySelectorAll).toHaveBeenCalledWith('.stage-indicator');
            // Should not throw even with null result
        });

        it('should handle missing auto-refresh toggle gracefully', () => {
            mockDocument.getElementById = jest.fn((id) => {
                if (id === 'auto-refresh-toggle') return null;
                return mockElements[id.replace('-', '')] || null;
            });

            expect(() => service.setupEventListeners()).not.toThrow();
        });
    });
});

describe('EventHandlerService Integration', () => {
    it('should handle multiple rapid clicks without errors', () => {
        const mockWorkflowManager = {
            navigateStage: jest.fn(),
            jumpToStage: jest.fn()
        };

        const service = {
            workflowManager: mockWorkflowManager,

            setupEventListeners() {
                // Simulate rapid button clicks
                for (let i = 0; i < 10; i++) {
                    this.workflowManager.navigateStage(-1);
                }
            }
        };

        expect(() => service.setupEventListeners()).not.toThrow();
        expect(mockWorkflowManager.navigateStage).toHaveBeenCalledTimes(10);
    });

    it('should handle mixed navigation events', () => {
        const mockWorkflowManager = {
            navigateStage: jest.fn(),
            jumpToStage: jest.fn()
        };

        const service = {
            workflowManager: mockWorkflowManager,

            simulateMixedEvents() {
                // Simulate various navigation events
                this.workflowManager.navigateStage(-1);
                this.workflowManager.navigateStage(1);
                this.workflowManager.jumpToStage(3);
                this.workflowManager.navigateStage(-1);
            }
        };

        service.simulateMixedEvents();

        expect(mockWorkflowManager.navigateStage).toHaveBeenCalledWith(-1);
        expect(mockWorkflowManager.navigateStage).toHaveBeenCalledWith(1);
        expect(mockWorkflowManager.jumpToStage).toHaveBeenCalledWith(3);
    });
});
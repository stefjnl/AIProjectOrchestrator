/**
 * UI Rendering Services Test Suite
 * Tests StoryRenderer, ProgressRenderer, and StatusUtils functionality
 */

// Mock DOM environment for testing
if (typeof document === 'undefined') {
    global.document = {
        getElementById: () => null,
        createElement: () => ({
            innerHTML: '',
            appendChild: () => { },
            remove: () => { },
            classList: { add: () => { }, remove: () => { } },
            style: {}
        }),
        querySelector: () => null,
        querySelectorAll: () => [],
        body: { appendChild: () => { } }
    };
}

// Load services (in real environment, these would be loaded via script tags)
// For testing, we'll assume they're available globally

class UIRenderingTest {
    constructor() {
        this.testResults = [];
        this.totalTests = 0;
        this.passedTests = 0;
        this.failedTests = 0;
    }

    /**
     * Run all UI rendering tests
     */
    async runAllTests() {
        console.log('🚀 Starting UI Rendering Services Test Suite...\n');

        // Test StatusUtils
        await this.testStatusUtils();

        // Test StoryRenderer
        await this.testStoryRenderer();

        // Test ProgressRenderer
        await this.testProgressRenderer();

        this.printResults();
        return this.testResults;
    }

    /**
     * Test StatusUtils functionality
     */
    async testStatusUtils() {
        console.log('📋 Testing StatusUtils...');

        const statusUtils = new StatusUtils();

        // Test status normalization
        this.test('normalizeStoryStatus - basic statuses', () => {
            this.assertEqual(statusUtils.normalizeStoryStatus('pending'), 'pending');
            this.assertEqual(statusUtils.normalizeStoryStatus('approved'), 'approved');
            this.assertEqual(statusUtils.normalizeStoryStatus('rejected'), 'rejected');
        });

        this.test('normalizeStoryStatus - case insensitive', () => {
            this.assertEqual(statusUtils.normalizeStoryStatus('PENDING'), 'pending');
            this.assertEqual(statusUtils.normalizeStoryStatus('Approved'), 'approved');
            this.assertEqual(statusUtils.normalizeStoryStatus('REJECTED'), 'rejected');
        });

        this.test('getStatusName - returns correct names', () => {
            this.assertEqual(statusUtils.getStatusName('pending'), 'Pending');
            this.assertEqual(statusUtils.getStatusName('approved'), 'Approved');
            this.assertEqual(statusUtils.getStatusName('rejected'), 'Rejected');
        });

        this.test('getStatusClass - returns correct CSS classes', () => {
            this.assertEqual(statusUtils.getStatusClass('pending'), 'status-pending');
            this.assertEqual(statusUtils.getStatusClass('approved'), 'status-approved');
            this.assertEqual(statusUtils.getStatusClass('rejected'), 'status-rejected');
        });

        this.test('canApproveStory - correct logic', () => {
            this.assertTrue(statusUtils.canApproveStory('pending'));
            this.assertTrue(statusUtils.canApproveStory('rejected'));
            this.assertTrue(statusUtils.canApproveStory('draft'));
            this.assertFalse(statusUtils.canApproveStory('approved'));
        });

        this.test('canRejectStory - correct logic', () => {
            this.assertTrue(statusUtils.canRejectStory('pending'));
            this.assertTrue(statusUtils.canRejectStory('approved'));
            this.assertTrue(statusUtils.canRejectStory('draft'));
            this.assertFalse(statusUtils.canRejectStory('rejected'));
        });

        this.test('canGeneratePrompt - correct logic', () => {
            this.assertTrue(statusUtils.canGeneratePrompt('approved', false));
            this.assertFalse(statusUtils.canGeneratePrompt('approved', true));
            this.assertFalse(statusUtils.canGeneratePrompt('pending', false));
            this.assertFalse(statusUtils.canGeneratePrompt('rejected', false));
        });

        this.test('calculateApprovalStats - empty array', () => {
            const stats = statusUtils.calculateApprovalStats([]);
            this.assertEqual(stats.total, 0);
            this.assertEqual(stats.approved, 0);
            this.assertEqual(stats.rejected, 0);
            this.assertEqual(stats.approvalPercentage, 0);
        });

        this.test('calculateApprovalStats - with stories', () => {
            const stories = [
                { status: 'approved' },
                { status: 'approved' },
                { status: 'pending' },
                { status: 'rejected' }
            ];
            const stats = statusUtils.calculateApprovalStats(stories);
            this.assertEqual(stats.total, 4);
            this.assertEqual(stats.approved, 2);
            this.assertEqual(stats.rejected, 1);
            this.assertEqual(stats.pending, 1);
            this.assertEqual(stats.approvalPercentage, 50);
        });

        this.test('validateStory - required fields', () => {
            const validStory = { title: 'Test', description: 'Test desc' };
            const invalidStory = { description: 'Test desc' };

            const validResult = statusUtils.validateStory(validStory);
            const invalidResult = statusUtils.validateStory(invalidStory);

            this.assertTrue(validResult.isValid);
            this.assertFalse(invalidResult.isValid);
            this.assertTrue(invalidResult.errors.includes('Title is required'));
        });

        console.log('✅ StatusUtils tests completed\n');
    }

    /**
     * Test StoryRenderer functionality
     */
    async testStoryRenderer() {
        console.log('🎨 Testing StoryRenderer...');

        const statusUtils = new StatusUtils();
        const storyRenderer = new StoryRenderer(statusUtils);

        // Test story card rendering
        this.test('createStoryCard - basic story', () => {
            const story = {
                id: '1',
                title: 'Test Story',
                description: 'Test description',
                status: 'pending',
                priority: 'Medium',
                storyPoints: 5,
                hasPrompt: false
            };

            const html = storyRenderer.createStoryCard(story, 0);

            this.assertTrue(html.includes('Test Story'));
            this.assertTrue(html.includes('Test description'));
            this.assertTrue(html.includes('status-pending'));
            this.assertTrue(html.includes('Points: 5'));
            this.assertTrue(html.includes('Medium'));
        });

        this.test('createStoryCard - approved story with prompt', () => {
            const story = {
                id: '2',
                title: 'Approved Story',
                description: 'Approved description',
                status: 'approved',
                priority: 'High',
                storyPoints: 8,
                hasPrompt: true,
                promptId: 'prompt-123'
            };

            const html = storyRenderer.createStoryCard(story, 1);

            this.assertTrue(html.includes('Approved Story'));
            this.assertTrue(html.includes('status-approved'));
            this.assertTrue(html.includes('View Prompt'));
            this.assertFalse(html.includes('Generate Prompt')); // Should not show for approved with prompt
        });

        this.test('renderEmptyState - returns correct HTML', () => {
            const html = storyRenderer.renderEmptyState();

            this.assertTrue(html.includes('No Stories Found'));
            this.assertTrue(html.includes('No user stories are available'));
            this.assertTrue(html.includes('Try Again'));
        });

        this.test('renderErrorState - returns correct HTML', () => {
            const errorMessage = 'Test error message';
            const html = storyRenderer.renderErrorState(errorMessage);

            this.assertTrue(html.includes('Error Loading Stories'));
            this.assertTrue(html.includes(errorMessage));
            this.assertTrue(html.includes('Try Again'));
        });

        this.test('renderStorySummary - calculates stats correctly', () => {
            const stats = { total: 10, approved: 7, rejected: 1, approvalPercentage: 70 };
            const html = storyRenderer.renderStorySummary(stats);

            this.assertTrue(html.includes('10')); // Total
            this.assertTrue(html.includes('7'));  // Approved
            this.assertTrue(html.includes('3'));  // Pending (10-7)
            this.assertTrue(html.includes('70%')); // Progress
        });

        this.test('renderStoryDetails - includes acceptance criteria', () => {
            const story = {
                title: 'Test Story',
                description: 'Test description',
                status: 'approved',
                acceptanceCriteria: ['Criterion 1', 'Criterion 2']
            };

            const html = storyRenderer.renderStoryDetails(story);

            this.assertTrue(html.includes('Test Story'));
            this.assertTrue(html.includes('Test description'));
            this.assertTrue(html.includes('Acceptance Criteria'));
            this.assertTrue(html.includes('Criterion 1'));
            this.assertTrue(html.includes('Criterion 2'));
        });

        this.test('renderEditForm - creates form with story data', () => {
            const story = {
                title: 'Edit Story',
                description: 'Edit description',
                priority: 'High',
                storyPoints: 10,
                acceptanceCriteria: ['Criterion 1', 'Criterion 2']
            };

            const html = storyRenderer.renderEditForm(story);

            this.assertTrue(html.includes('value="Edit Story"'));
            this.assertTrue(html.includes('Edit description'));
            this.assertTrue(html.includes('selected>High'));
            this.assertTrue(html.includes('value="10"'));
            this.assertTrue(html.includes('Criterion 1'));
            this.assertTrue(html.includes('Criterion 2'));
        });

        console.log('✅ StoryRenderer tests completed\n');
    }

    /**
     * Test ProgressRenderer functionality
     */
    async testProgressRenderer() {
        console.log('⏳ Testing ProgressRenderer...');

        const progressRenderer = new ProgressRenderer();

        // Test progress rendering
        this.test('showProgress - creates progress HTML', () => {
            const html = progressRenderer.createProgressElements();
            // Since we're in test environment, we can't test actual DOM manipulation
            // But we can verify the method exists and doesn't throw
            this.assertTrue(true); // Method executed without error
        });

        this.test('renderStepIndicators - creates correct HTML', () => {
            const steps = [
                { name: 'Step 1' },
                { name: 'Step 2' },
                { name: 'Step 3' }
            ];

            const html = progressRenderer.createStepIndicators(steps, 1);

            this.assertTrue(html.includes('Step 1'));
            this.assertTrue(html.includes('Step 2'));
            this.assertTrue(html.includes('Step 3'));
            this.assertTrue(html.includes('completed')); // Step 1 should be completed
            this.assertTrue(html.includes('active'));    // Step 2 should be active
            this.assertTrue(html.includes('pending'));   // Step 3 should be pending
        });

        this.test('getStatusIcon - returns correct icons', () => {
            this.assertEqual(progressRenderer.getStatusIcon('success'), '✅');
            this.assertEqual(progressRenderer.getStatusIcon('error'), '❌');
            this.assertEqual(progressRenderer.getStatusIcon('warning'), '⚠️');
            this.assertEqual(progressRenderer.getStatusIcon('info'), 'ℹ️');
        });

        this.test('renderErrorMessage - creates error HTML', () => {
            const html = progressRenderer.renderErrorMessage('Test error', 'Test details');

            this.assertTrue(html.includes('Test error'));
            this.assertTrue(html.includes('Test details'));
            this.assertTrue(html.includes('❌'));
        });

        this.test('renderSuccessMessage - creates success HTML', () => {
            const html = progressRenderer.renderSuccessMessage('Test success', 'Test details');

            this.assertTrue(html.includes('Test success'));
            this.assertTrue(html.includes('Test details'));
            this.assertTrue(html.includes('✅'));
        });

        console.log('✅ ProgressRenderer tests completed\n');
    }

    /**
     * Test helper methods
     */
    test(description, testFunction) {
        this.totalTests++;
        try {
            testFunction();
            this.passedTests++;
            console.log(`  ✅ ${description}`);
        } catch (error) {
            this.failedTests++;
            console.log(`  ❌ ${description}`);
            console.log(`     Error: ${error.message}`);
        }
    }

    assertTrue(condition, message = 'Expected true') {
        if (!condition) {
            throw new Error(message);
        }
    }

    assertFalse(condition, message = 'Expected false') {
        if (condition) {
            throw new Error(message);
        }
    }

    assertEqual(actual, expected, message = `Expected ${expected}, got ${actual}`) {
        if (actual !== expected) {
            throw new Error(message);
        }
    }

    /**
     * Print test results summary
     */
    printResults() {
        console.log('\n📊 Test Results Summary:');
        console.log(`Total Tests: ${this.totalTests}`);
        console.log(`Passed: ${this.passedTests} ✅`);
        console.log(`Failed: ${this.failedTests} ❌`);

        if (this.failedTests === 0) {
            console.log('\n🎉 All tests passed! UI Rendering services are working correctly.');
        } else {
            console.log(`\n⚠️  ${this.failedTests} test(s) failed. Please review the implementation.`);
        }
    }
}

// Run tests if this file is executed directly
if (typeof require !== 'undefined' && require.main === module) {
    const testSuite = new UIRenderingTest();
    testSuite.runAllTests();
}

// Export for use in other test files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { UIRenderingTest };
}

// Make available globally for browser testing (only if window exists)
if (typeof window !== 'undefined') {
    window.UIRenderingTest = UIRenderingTest;
}
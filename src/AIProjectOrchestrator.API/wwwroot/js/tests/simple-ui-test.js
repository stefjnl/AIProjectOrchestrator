/**
 * Simple UI Rendering Test
 * Loads service files and tests basic functionality
 */

const fs = require('fs');
const path = require('path');

// Load service files
console.log('📂 Loading UI rendering services...');

try {
    // Load StatusUtils
    const statusUtilsContent = fs.readFileSync('src/AIProjectOrchestrator.API/wwwroot/js/services/status-utils.js', 'utf8');
    eval(statusUtilsContent.replace(/if \(typeof module.*?\{[\s\S]*?\}/g, '')); // Remove module exports for Node.js

    // Load StoryRenderer
    const storyRendererContent = fs.readFileSync('src/AIProjectOrchestrator.API/wwwroot/js/services/story-renderer.js', 'utf8');
    eval(storyRendererContent.replace(/if \(typeof module.*?\{[\s\S]*?\}/g, ''));

    // Load ProgressRenderer
    const progressRendererContent = fs.readFileSync('src/AIProjectOrchestrator.API/wwwroot/js/services/progress-renderer.js', 'utf8');
    eval(progressRendererContent.replace(/if \(typeof module.*?\{[\s\S]*?\}/g, ''));

    console.log('✅ All services loaded successfully\n');
} catch (error) {
    console.error('❌ Error loading services:', error.message);
    process.exit(1);
}

// Mock DOM for testing
global.document = {
    getElementById: (id) => {
        console.log(`📄 Getting element by ID: ${id}`);
        return {
            style: {},
            innerHTML: '',
            appendChild: () => { },
            remove: () => { },
            classList: { add: () => { }, remove: () => { } },
            textContent: ''
        };
    },
    createElement: (tag) => ({
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

global.window = {
    setTimeout: setTimeout,
    clearTimeout: clearTimeout
};

// Test suite
class SimpleUITest {
    constructor() {
        this.passed = 0;
        this.failed = 0;
        this.tests = [];
    }

    test(name, fn) {
        try {
            console.log(`🧪 Testing: ${name}`);
            fn();
            this.passed++;
            console.log(`   ✅ PASSED\n`);
        } catch (error) {
            this.failed++;
            console.log(`   ❌ FAILED: ${error.message}\n`);
        }
    }

    assert(condition, message) {
        if (!condition) {
            throw new Error(message || 'Assertion failed');
        }
    }

    assertEqual(actual, expected, message) {
        if (actual !== expected) {
            throw new Error(message || `Expected ${expected}, got ${actual}`);
        }
    }

    runTests() {
        console.log('🚀 Starting Simple UI Rendering Tests...\n');

        // Test StatusUtils
        console.log('📋 Testing StatusUtils...');
        const statusUtils = new StatusUtils();

        this.test('StatusUtils - normalizeStoryStatus', () => {
            this.assertEqual(statusUtils.normalizeStoryStatus('pending'), 'pending');
            this.assertEqual(statusUtils.normalizeStoryStatus('PENDING'), 'pending');
            this.assertEqual(statusUtils.normalizeStoryStatus('approved'), 'approved');
        });

        this.test('StatusUtils - getStatusName', () => {
            this.assertEqual(statusUtils.getStatusName('pending'), 'Pending');
            this.assertEqual(statusUtils.getStatusName('approved'), 'Approved');
            this.assertEqual(statusUtils.getStatusName('rejected'), 'Rejected');
        });

        this.test('StatusUtils - getStatusClass', () => {
            this.assertEqual(statusUtils.getStatusClass('pending'), 'status-pending');
            this.assertEqual(statusUtils.getStatusClass('approved'), 'status-approved');
            this.assertEqual(statusUtils.getStatusClass('rejected'), 'status-rejected');
        });

        this.test('StatusUtils - canApproveStory', () => {
            this.assert(statusUtils.canApproveStory('pending'), 'Should be able to approve pending');
            this.assert(statusUtils.canApproveStory('rejected'), 'Should be able to approve rejected');
            this.assert(!statusUtils.canApproveStory('approved'), 'Should not be able to approve approved');
        });

        this.test('StatusUtils - calculateApprovalStats', () => {
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
            this.assertEqual(stats.approvalPercentage, 50);
        });

        // Test StoryRenderer
        console.log('🎨 Testing StoryRenderer...');
        const storyRenderer = new StoryRenderer(statusUtils);

        this.test('StoryRenderer - createStoryCard basic', () => {
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

            this.assert(html.includes('Test Story'), 'Should include title');
            this.assert(html.includes('Test description'), 'Should include description');
            this.assert(html.includes('status-pending'), 'Should include status class');
            this.assert(html.includes('Points: 5'), 'Should include story points');
        });

        this.test('StoryRenderer - renderEmptyState', () => {
            const html = storyRenderer.renderEmptyState();

            this.assert(html.includes('No Stories Found'), 'Should include empty state title');
            this.assert(html.includes('No user stories are available'), 'Should include empty state message');
            this.assert(html.includes('Try Again'), 'Should include retry button');
        });

        this.test('StoryRenderer - renderStorySummary', () => {
            const stats = { total: 10, approved: 7, rejected: 1, approvalPercentage: 70 };
            const html = storyRenderer.renderStorySummary(stats);

            this.assert(html.includes('10'), 'Should include total');
            this.assert(html.includes('7'), 'Should include approved');
            this.assert(html.includes('70%'), 'Should include percentage');
        });

        // Test ProgressRenderer
        console.log('⏳ Testing ProgressRenderer...');
        const progressRenderer = new ProgressRenderer();

        this.test('ProgressRenderer - createStepIndicators', () => {
            const steps = [
                { name: 'Step 1' },
                { name: 'Step 2' },
                { name: 'Step 3' }
            ];

            const html = progressRenderer.createStepIndicators(steps, 1);

            this.assert(html.includes('Step 1'), 'Should include step 1 name');
            this.assert(html.includes('Step 2'), 'Should include step 2 name');
            this.assert(html.includes('Step 3'), 'Should include step 3 name');
            this.assert(html.includes('completed'), 'Should mark completed steps');
            this.assert(html.includes('active'), 'Should mark active step');
        });

        this.test('ProgressRenderer - getStatusIcon', () => {
            this.assertEqual(progressRenderer.getStatusIcon('success'), '✅');
            this.assertEqual(progressRenderer.getStatusIcon('error'), '❌');
            this.assertEqual(progressRenderer.getStatusIcon('warning'), '⚠️');
            this.assertEqual(progressRenderer.getStatusIcon('info'), 'ℹ️');
        });

        this.test('ProgressRenderer - renderErrorMessage', () => {
            const html = progressRenderer.renderErrorMessage('Test error', 'Test details');

            this.assert(html.includes('Test error'), 'Should include error message');
            this.assert(html.includes('Test details'), 'Should include error details');
            this.assert(html.includes('❌'), 'Should include error icon');
        });

        // Print results
        console.log('\n📊 Test Results:');
        console.log(`Total Tests: ${this.passed + this.failed}`);
        console.log(`Passed: ${this.passed} ✅`);
        console.log(`Failed: ${this.failed} ❌`);

        if (this.failed === 0) {
            console.log('\n🎉 All UI rendering tests passed!');
        } else {
            console.log(`\n⚠️  ${this.failed} test(s) failed.`);
        }
    }
}

// Run tests
const testSuite = new SimpleUITest();
testSuite.runTests();
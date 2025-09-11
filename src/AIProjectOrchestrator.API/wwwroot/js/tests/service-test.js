/**
 * Service Test Script
 * Tests the newly created services to ensure they work correctly
 */

// Mock dependencies
const mockWindow = {
    App: {
        showNotification: (message, type) => {
            console.log(`[${type.toUpperCase()}] ${message}`);
        }
    }
};

const mockDocument = {
    createElement: (tagName) => ({
        href: '',
        download: '',
        style: { display: '' },
        click: () => console.log('File download simulated'),
        appendChild: () => { },
        removeChild: () => { }
    }),
    body: {
        appendChild: () => { },
        removeChild: () => { }
    }
};

// Set up global mocks
global.window = mockWindow;
global.document = mockDocument;
global.URL = {
    createObjectURL: () => 'blob:test-url',
    revokeObjectURL: () => { }
};
global.Blob = class {
    constructor(content, options) {
        this.content = content;
        this.type = options.type;
    }
};

// Load services
const fs = require('fs');

// Load each service and ensure they're available globally
const storyServiceContent = fs.readFileSync('src/AIProjectOrchestrator.API/wwwroot/js/services/story-api-service.js', 'utf8');
const promptServiceContent = fs.readFileSync('src/AIProjectOrchestrator.API/wwwroot/js/services/prompt-service.js', 'utf8');
const exportServiceContent = fs.readFileSync('src/AIProjectOrchestrator.API/wwwroot/js/services/export-service.js', 'utf8');

// Execute the service files to define the classes globally
eval(storyServiceContent);
eval(promptServiceContent);
eval(exportServiceContent);

// Mock API client
const mockApiClient = {
    getStories: (generationId) => {
        console.log(`Mock: getStories called with generationId: ${generationId}`);
        return Promise.resolve([
            { id: '1', title: 'Test Story 1', status: 'pending' },
            { id: '2', title: 'Test Story 2', status: 'approved' }
        ]);
    },
    approveStory: (storyId) => {
        console.log(`Mock: approveStory called with storyId: ${storyId}`);
        return Promise.resolve({ success: true });
    },
    rejectStory: (storyId, data) => {
        console.log(`Mock: rejectStory called with storyId: ${storyId}, feedback: ${data.feedback}`);
        return Promise.resolve({ success: true });
    },
    editStory: (storyId, updatedStory) => {
        console.log(`Mock: editStory called with storyId: ${storyId}`, updatedStory);
        return Promise.resolve({ success: true });
    },
    generatePrompt: (request) => {
        console.log(`Mock: generatePrompt called with request:`, request);
        // Simulate different response formats
        const responses = [
            { promptId: 'prompt-123' },
            { PromptId: 'prompt-456' },
            { id: 'prompt-789' },
            { Id: 'prompt-abc' }
        ];
        return Promise.resolve(responses[Math.floor(Math.random() * responses.length)]);
    },
    getPrompt: (promptId) => {
        console.log(`Mock: getPrompt called with promptId: ${promptId}`);
        return Promise.resolve({
            promptId: promptId,
            storyTitle: 'Test Story',
            generatedPrompt: 'This is a test prompt with Context, Requirements, Testing, and Code implementation details.',
            createdAt: new Date().toISOString()
        });
    }
};

// Test functions
async function testStoryApiService() {
    console.log('\n🧪 Testing StoryApiService...');

    const service = new StoryApiService(mockApiClient);

    try {
        // Test loadStories
        const stories = await service.loadStories('test-generation-123');
        console.log('✅ loadStories:', stories.length, 'stories loaded');

        // Test approveStory
        await service.approveStory('story-1');
        console.log('✅ approveStory: Story approved successfully');

        // Test rejectStory
        await service.rejectStory('story-2', 'Needs more detail');
        console.log('✅ rejectStory: Story rejected with feedback');

        // Test editStory
        await service.editStory('story-1', { title: 'Updated Title' });
        console.log('✅ editStory: Story edited successfully');

        // Test approveAllStories
        await service.approveAllStories(['story-1', 'story-2']);
        console.log('✅ approveAllStories: All stories approved');

        console.log('✅ StoryApiService: All tests passed');
        return true;
    } catch (error) {
        console.log('❌ StoryApiService test failed:', error.message);
        return false;
    }
}

async function testPromptService() {
    console.log('\n🧪 Testing PromptService...');

    const service = new PromptService(mockApiClient);

    try {
        // Test validatePromptRequest
        const validRequest = {
            StoryGenerationId: '12345678-1234-1234-1234-123456789012',
            StoryIndex: 0,
            TechnicalPreferences: {},
            PromptStyle: null
        };

        const invalidRequest = {
            StoryGenerationId: 'invalid-guid',
            StoryIndex: -1
        };

        console.log('✅ Valid request:', service.validatePromptRequest(validRequest));
        console.log('❌ Invalid request:', service.validatePromptRequest(invalidRequest));
        console.log('❌ Empty request:', service.validatePromptRequest({}));

        // Test generatePrompt with valid request
        const result = await service.generatePrompt(validRequest);
        console.log('✅ generatePrompt:', result);

        // Test generatePrompt with different response formats
        const results = await Promise.all([
            service.generatePrompt(validRequest),
            service.generatePrompt(validRequest),
            service.generatePrompt(validRequest),
            service.generatePrompt(validRequest)
        ]);
        console.log('✅ Multiple generatePrompt calls with different formats');

        // Test getPrompt
        const promptData = await service.getPrompt('test-prompt-123');
        console.log('✅ getPrompt:', promptData.storyTitle);

        // Test calculateQualityScore
        const qualityScore = service.calculateQualityScore('This prompt includes Context, Requirements, Testing, and Code implementation details that make it longer than 1000 characters. ' + 'x'.repeat(500));
        console.log('✅ calculateQualityScore:', qualityScore);

        // Test generateMockPrompt
        const mockStory = {
            title: 'Test Story',
            description: 'Test description',
            priority: 'High',
            storyPoints: 5,
            acceptanceCriteria: ['Criterion 1', 'Criterion 2']
        };
        const mockPrompt = service.generateMockPrompt(mockStory);
        console.log('✅ generateMockPrompt:', mockPrompt.substring(0, 100) + '...');

        console.log('✅ PromptService: All tests passed');
        return true;
    } catch (error) {
        console.log('❌ PromptService test failed:', error.message);
        return false;
    }
}

function testExportService() {
    console.log('\n🧪 Testing ExportService...');

    const service = new ExportService();

    try {
        // Test data
        const testStories = [
            {
                id: '1',
                title: 'Test Story 1',
                description: 'This is a test story with, commas and "quotes"',
                status: 'approved',
                priority: 'High',
                storyPoints: 5,
                hasPrompt: true,
                promptId: 'prompt-123',
                acceptanceCriteria: ['Criterion 1', 'Criterion 2']
            },
            {
                id: '2',
                title: 'Test Story 2',
                description: 'Another test story',
                status: 'pending',
                priority: 'Medium',
                storyPoints: 3,
                hasPrompt: false,
                promptId: null
            }
        ];

        const testPrompt = {
            promptId: 'prompt-123',
            storyTitle: 'Test Story',
            generatedPrompt: 'This is a test prompt with Context, Requirements, Testing, and Code implementation details.',
            createdAt: new Date().toISOString()
        };

        // Test generateStoriesSummary
        const summary = service.generateStoriesSummary(testStories);
        console.log('✅ generateStoriesSummary:', summary);

        // Test exportStories (simulated)
        service.exportStories(testStories, 'gen-123', 'proj-456', 'test-export');
        console.log('✅ exportStories: JSON export simulated');

        // Test exportStoriesAsCSV (simulated)
        service.exportStoriesAsCSV(testStories, 'test-stories.csv');
        console.log('✅ exportStoriesAsCSV: CSV export simulated');

        // Test exportPrompt (simulated)
        service.exportPrompt(testPrompt, 'test-prompt');
        console.log('✅ exportPrompt: JSON export simulated');

        // Test exportPromptsAsText (simulated)
        service.exportPromptsAsText([testPrompt], 'test-prompts.txt');
        console.log('✅ exportPromptsAsText: Text export simulated');

        // Test escapeCSV
        const escapedText = service.escapeCSV('Text with, commas and "quotes"');
        console.log('✅ escapeCSV:', escapedText);

        console.log('✅ ExportService: All tests passed');
        return true;
    } catch (error) {
        console.log('❌ ExportService test failed:', error.message);
        return false;
    }
}

// Run all tests
async function runAllTests() {
    console.log('🚀 Starting Service Tests...');

    const results = await Promise.all([
        testStoryApiService(),
        testPromptService(),
        testExportService()
    ]);

    const passed = results.filter(r => r).length;
    const total = results.length;

    console.log(`\n📊 Test Results: ${passed}/${total} services passed`);

    if (passed === total) {
        console.log('🎉 All service tests passed! Services are ready for integration.');
        process.exit(0);
    } else {
        console.log('❌ Some tests failed. Please review the issues above.');
        process.exit(1);
    }
}

// Run tests
runAllTests().catch(error => {
    console.error('Test runner failed:', error);
    process.exit(1);
});
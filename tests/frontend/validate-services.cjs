// Simple validation script for the refactored services
const fs = require('fs');
const path = require('path');

console.log('=== SERVICE VALIDATION STARTED ===\n');

// Check if all service files exist
const services = [
    '../../src/AIProjectOrchestrator.API/wwwroot/js/services/stage-initialization.js',
    '../../src/AIProjectOrchestrator.API/wwwroot/js/services/event-handler.js',
    '../../src/AIProjectOrchestrator.API/wwwroot/js/services/workflow-content.js'
];

console.log('Checking service file existence:');
services.forEach(service => {
    const exists = fs.existsSync(service);
    console.log(`  ${service}: ${exists ? '✓' : '✗'}`);
});

// Check if test files exist
const testFiles = [
    'workflow-content-service.test.js',
    'event-handler-service.test.js',
    'stage-initialization-service.test.js',
    'workflow-integration.test.js'
];

console.log('\nChecking test file existence:');
testFiles.forEach(testFile => {
    const exists = fs.existsSync(testFile);
    console.log(`  ${testFile}: ${exists ? '✓' : '✗'}`);
});

// Validate service file structure
console.log('\n=== VALIDATING SERVICE STRUCTURE ===\n');

function validateService(filePath, serviceName) {
    console.log(`Validating ${serviceName}:`);

    if (!fs.existsSync(filePath)) {
        console.log(`  ✗ File not found: ${filePath}`);
        return false;
    }

    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');

    console.log(`  Lines of code: ${lines.length}`);
    console.log(`  Size: ${Math.round(content.length / 1024)}KB`);

    // Check for key patterns
    const hasModuleExport = content.includes('module.exports') || content.includes('export');
    const hasErrorHandling = content.includes('try') && content.includes('catch');
    const hasLogging = content.includes('console.log') || content.includes('console.error');
    const hasJSDoc = content.includes('/**') || content.includes('*/');

    console.log(`  Module exports: ${hasModuleExport ? '✓' : '✗'}`);
    console.log(`  Error handling: ${hasErrorHandling ? '✓' : '✗'}`);
    console.log(`  Logging: ${hasLogging ? '✓' : '✗'}`);
    console.log(`  Documentation: ${hasJSDoc ? '✓' : '✗'}`);

    // Check for specific service patterns
    if (serviceName.includes('Content')) {
        const hasStageContent = content.includes('getStageContent');
        const hasRequirements = content.includes('getRequirementsStage');
        console.log(`  Stage content method: ${hasStageContent ? '✓' : '✗'}`);
        console.log(`  Requirements method: ${hasRequirements ? '✓' : '✗'}`);
    } else if (serviceName.includes('Event')) {
        const hasEventListeners = content.includes('setupEventListeners');
        const hasNavigation = content.includes('navigateStage');
        console.log(`  Event listeners setup: ${hasEventListeners ? '✓' : '✗'}`);
        console.log(`  Navigation handling: ${hasNavigation ? '✓' : '✗'}`);
    } else if (serviceName.includes('Stage')) {
        const hasInitializeStage = content.includes('initializeStage');
        const hasStageSwitch = content.includes('switch (stage)');
        console.log(`  Initialize stage method: ${hasInitializeStage ? '✓' : '✗'}`);
        console.log(`  Stage switch logic: ${hasStageSwitch ? '✓' : '✗'}`);
    }

    console.log('');
    return true;
}

// Validate each service
validateService('../../src/AIProjectOrchestrator.API/wwwroot/js/services/workflow-content.js', 'WorkflowContentService');
validateService('../../src/AIProjectOrchestrator.API/wwwroot/js/services/event-handler.js', 'EventHandlerService');
validateService('../../src/AIProjectOrchestrator.API/wwwroot/js/services/stage-initialization.js', 'StageInitializationService');

// Validate main workflow file
console.log('Validating main workflow.js:');
const workflowContent = fs.readFileSync('../../src/AIProjectOrchestrator.API/wwwroot/js/workflow.js', 'utf8');
const workflowLines = workflowContent.split('\n');
console.log(`  Lines of code: ${workflowLines.length}`);
console.log(`  Size: ${Math.round(workflowContent.length / 1024)}KB`);

const hasServiceIntegration = workflowContent.includes('initializeServices');
const hasFallbackMechanism = workflowContent.includes('InlineWorkflowContentService');
const hasContentService = workflowContent.includes('contentService');
const hasEventHandler = workflowContent.includes('eventHandler');
const hasStageInitializer = workflowContent.includes('stageInitializer');

console.log(`  Service integration: ${hasServiceIntegration ? '✓' : '✗'}`);
console.log(`  Fallback mechanism: ${hasFallbackMechanism ? '✓' : '✗'}`);
console.log(`  Content service: ${hasContentService ? '✓' : '✗'}`);
console.log(`  Event handler: ${hasEventHandler ? '✓' : '✗'}`);
console.log(`  Stage initializer: ${hasStageInitializer ? '✓' : '✗'}`);

// Validate test files
console.log('\n=== VALIDATING TEST FILES ===\n');

function validateTestFile(filePath, testName) {
    console.log(`Validating ${testName}:`);

    if (!fs.existsSync(filePath)) {
        console.log(`  ✗ File not found: ${filePath}`);
        return false;
    }

    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');

    console.log(`  Lines of code: ${lines.length}`);
    console.log(`  Size: ${Math.round(content.length / 1024)}KB`);

    const hasDescribe = content.includes('describe(');
    const hasIt = content.includes('it(') || content.includes('test(');
    const hasExpect = content.includes('expect(');
    const hasBeforeEach = content.includes('beforeEach');
    const hasAfterEach = content.includes('afterEach');

    console.log(`  Test suites: ${hasDescribe ? '✓' : '✗'}`);
    console.log(`  Test cases: ${hasIt ? '✓' : '✗'}`);
    console.log(`  Assertions: ${hasExpect ? '✓' : '✗'}`);
    console.log(`  Setup/Teardown: ${hasBeforeEach && hasAfterEach ? '✓' : '✗'}`);

    console.log('');
    return true;
}

validateTestFile('workflow-content-service.test.js', 'WorkflowContentService Tests');
validateTestFile('event-handler-service.test.js', 'EventHandlerService Tests');
validateTestFile('stage-initialization-service.test.js', 'StageInitializationService Tests');
validateTestFile('workflow-integration.test.js', 'Integration Tests');

// Summary
console.log('=== VALIDATION SUMMARY ===\n');

const totalServiceLines = [
    '../../src/AIProjectOrchestrator.API/wwwroot/js/services/workflow-content.js',
    '../../src/AIProjectOrchestrator.API/wwwroot/js/services/event-handler.js',
    '../../src/AIProjectOrchestrator.API/wwwroot/js/services/stage-initialization.js'
].reduce((total, file) => {
    if (fs.existsSync(file)) {
        const content = fs.readFileSync(file, 'utf8');
        return total + content.split('\n').length;
    }
    return total;
}, 0);

const workflowLinesCount = fs.readFileSync('../../src/AIProjectOrchestrator.API/wwwroot/js/workflow.js', 'utf8').split('\n').length;

console.log(`Original workflow.js lines: ~2,114`);
console.log(`Refactored workflow.js lines: ${workflowLinesCount}`);
console.log(`New service files total lines: ${totalServiceLines}`);
console.log(`Total lines across all files: ${workflowLines + totalServiceLines}`);
console.log(`Code reduction in main file: ${Math.round((1 - workflowLines / 2114) * 100)}%`);

console.log('\n=== VALIDATION COMPLETED ===');
# Prompt Generation Phase 4: API & Testing Completion Implementation

## Project Context
You are implementing the final phase of the AI Project Orchestrator's prompt generation feature. This .NET 9 Clean Architecture application transforms individual user stories into comprehensive coding prompts for AI assistants. The core business logic is complete - you need to expose it through REST APIs and validate with comprehensive testing.

## Current State
- ✅ Domain models and service interfaces complete
- ✅ Core AI orchestration logic implemented (`PromptGenerationService`)
- ✅ Context assembly and optimization working (`PromptContextAssembler`, `ContextOptimizer`)
- ✅ Unit tests passing for all new components
- 🔄 **Missing: REST API endpoints and integration testing**

## Implementation Requirements

### 1. API Controller Implementation

**File**: `src/AIProjectOrchestrator.API/Controllers/PromptGenerationController.cs`

**Required Endpoints**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class PromptGenerationController : ControllerBase
{
    // POST /api/prompts/generate
    [HttpPost("generate")]
    public async Task<ActionResult<PromptGenerationResponse>> GeneratePrompt(
        [FromBody] PromptGenerationRequest request, 
        CancellationToken cancellationToken)
    
    // GET /api/prompts/{promptId}/status  
    [HttpGet("{promptId}/status")]
    public async Task<ActionResult<PromptGenerationStatus>> GetPromptStatus(
        string promptId, 
        CancellationToken cancellationToken)
    
    // GET /api/prompts/can-generate/{storyGenerationId}/{storyIndex}
    [HttpGet("can-generate/{storyGenerationId}/{storyIndex}")]
    public async Task<ActionResult<bool>> CanGeneratePrompt(
        string storyGenerationId, 
        int storyIndex, 
        CancellationToken cancellationToken)
    
    // GET /api/prompts/{promptId}
    [HttpGet("{promptId}")]
    public async Task<ActionResult<PromptGenerationResponse>> GetPrompt(
        string promptId, 
        CancellationToken cancellationToken)
}
```

**Error Handling Requirements**:
- 400 Bad Request for validation failures with detailed error messages
- 404 Not Found for missing prompts/stories
- 409 Conflict for prerequisite validation failures (unapproved stories)
- 500 Internal Server Error for service failures

**Follow Existing Patterns**: Use identical structure to `RequirementsController`, `ProjectPlanningController`, `StoryGenerationController` including:
- Constructor dependency injection with ILogger
- Try-catch error handling with structured logging
- Proper HTTP status code mapping
- Model validation attributes usage

### 2. Integration Testing

**File**: `tests/AIProjectOrchestrator.IntegrationTests/PromptGenerationIntegrationTests.cs`

**Required Test Coverage**:
```csharp
public class PromptGenerationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GeneratePrompt_ValidRequest_ReturnsSuccess()
    
    [Fact]
    public async Task GeneratePrompt_InvalidStoryGenerationId_ReturnsBadRequest()
    
    [Fact]
    public async Task GeneratePrompt_UnapprovedStories_ReturnsConflict()
    
    [Fact]
    public async Task GetPromptStatus_ValidId_ReturnsStatus()
    
    [Fact]
    public async Task GetPromptStatus_InvalidId_ReturnsNotFound()
    
    [Fact]
    public async Task CanGeneratePrompt_ValidPrerequisites_ReturnsTrue()
    
    [Fact]
    public async Task CanGeneratePrompt_InvalidPrerequisites_ReturnsFalse()
    
    [Fact]
    public async Task GetPrompt_ValidId_ReturnsPromptDetails()
}
```

**Testing Requirements**:
- Use WebApplicationFactory for full HTTP integration testing
- Mock external dependencies (AI providers) using existing test infrastructure
- Validate request/response serialization
- Test error scenarios with proper HTTP status codes
- Follow existing integration test patterns from other controllers

### 3. End-to-End Workflow Testing

**File**: `tests/AIProjectOrchestrator.IntegrationTests/CompleteWorkflowIntegrationTests.cs`

**Required Scenario**:
```csharp
[Fact]
public async Task CompleteWorkflow_ProjectToPrompts_Success()
{
    // 1. Create project
    // 2. Submit requirements analysis  
    // 3. Approve requirements
    // 4. Submit project planning
    // 5. Approve planning
    // 6. Submit story generation
    // 7. Approve stories
    // 8. Generate prompt for first story
    // 9. Verify prompt contains expected sections
}
```

### 4. Service Registration Verification

**File**: `src/AIProjectOrchestrator.API/Program.cs`

**Ensure Registration**:
```csharp
// Verify these services are registered
builder.Services.AddScoped<IPromptGenerationService, PromptGenerationService>();
builder.Services.AddScoped<PromptContextAssembler>();
builder.Services.AddScoped<ContextOptimizer>();
```

### 5. Model Validation Enhancement

**Files**: Update domain models if needed

**Add Validation Attributes**:
- `PromptGenerationRequest`: Required fields, range validation for StoryIndex
- Ensure proper error messages for API validation failures

## Technical Constraints

### Architecture Compliance
- **Clean Architecture**: Controller only orchestrates, no business logic
- **Dependency Injection**: Use constructor injection with proper lifetimes
- **Async Patterns**: All methods async with CancellationToken support
- **Error Handling**: Domain exceptions translated to appropriate HTTP status codes

### Existing Infrastructure Usage
- **IPromptGenerationService**: Use existing implementation
- **ILogger**: Structured logging for operations and errors
- **Model Validation**: Use built-in ModelState validation
- **Exception Handling**: Follow patterns from existing controllers

### Performance Considerations
- **Context Size Monitoring**: Log context size in prompt generation
- **Timeout Handling**: Proper cancellation token usage for AI operations
- **Memory Management**: Dispose resources appropriately

## Expected Deliverables

1. **PromptGenerationController.cs** - Complete REST API implementation
2. **PromptGenerationIntegrationTests.cs** - Comprehensive API testing
3. **CompleteWorkflowIntegrationTests.cs** - End-to-end validation
4. **Updated Program.cs** - Service registration verification
5. **All tests passing** - Both unit and integration test suites

## Success Criteria

- All API endpoints functional and properly routed
- Comprehensive error handling with meaningful HTTP responses
- Integration tests covering happy path and error scenarios
- End-to-end workflow test validating complete pipeline
- Consistent patterns with existing controllers
- No breaking changes to existing functionality

## Implementation Notes

- **Follow Existing Patterns**: Copy structure from `StoryGenerationController` as template
- **Error Translation**: Map domain exceptions to HTTP status codes consistently
- **Logging Strategy**: Use structured logging for debugging and monitoring
- **Test Data**: Use realistic test scenarios with proper user story content
- **Documentation**: Ensure Swagger documentation generates properly for new endpoints

The prompt generation feature represents sophisticated AI orchestration - your API implementation should reflect this enterprise-grade quality through proper error handling, comprehensive testing, and architectural consistency.
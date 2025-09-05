## Implementation Overview

You're implementing **Phase 4: Prompt Generation** - a service that transforms individual approved user stories into comprehensive coding prompts for AI assistants. This creates an enterprise-grade prompt engineering platform within your AI Project Orchestrator.

**Architecture**: Following your existing pattern (Requirements → Planning → Stories → **Prompts**), this service receives individual user stories and generates detailed, technical prompts that coding assistants can use to implement the story.

**4-Phase Implementation Strategy**:
1. **Foundation & Data Models** ← Starting here
2. Story Retrieval Integration  
3. Prompt Generation Logic
4. API & Testing Completion

---

## Phase 1 Implementation Prompt

```
CONTEXT: .NET 9 AI Project Orchestrator implementing Phase 4 (Prompt Generation) - Phase 1 Foundation. You're creating the foundational structure for a service that generates comprehensive coding prompts from individual user stories.

PROJECT ARCHITECTURE: Clean Architecture (.NET 9) with Domain/Application/Infrastructure/API layers. Follow exact patterns from existing RequirementsAnalysisService, ProjectPlanningService, and StoryGenerationService.

PHASE 1 OBJECTIVE: Establish foundational contracts, data models, and service skeleton without complex business logic.

REQUIREMENTS:

1. DOMAIN LAYER (AIProjectOrchestrator.Domain):

   Create Models/PromptGeneration.cs:
   ```csharp
   // PromptGenerationRequest model
   // Properties: StoryId (Guid), TechnicalPreferences (Dictionary<string,string>), PromptStyle (string, optional)
   
   // PromptGenerationResponse model  
   // Properties: PromptId (Guid), GeneratedPrompt (string), ReviewId (Guid), Status (PromptGenerationStatus), CreatedAt (DateTime)
   
   // PromptGenerationStatus enum
   // Values: Processing, PendingReview, Approved, Rejected, Failed
   ```

   Create Interfaces/IPromptGenerationService.cs:
   ```csharp
   // Method: GeneratePromptAsync(PromptGenerationRequest, CancellationToken) → Task<PromptGenerationResponse>
   // Method: GetPromptStatusAsync(Guid promptId, CancellationToken) → Task<PromptGenerationStatus>
   // Method: CanGeneratePromptAsync(Guid storyId, CancellationToken) → Task<bool>
   ```

2. APPLICATION LAYER (AIProjectOrchestrator.Application):

   Create Services/PromptGenerationService.cs:
   ```csharp
   // Implement IPromptGenerationService
   // Constructor dependencies: ILogger<PromptGenerationService>
   // Use ConcurrentDictionary for in-memory storage (same pattern as other services)
   // All methods should be async with proper cancellation token support
   
   // GeneratePromptAsync: Return placeholder response for now
   // GetPromptStatusAsync: Return status from in-memory storage
   // CanGeneratePromptAsync: Return true for now (will implement validation in Phase 2)
   ```

3. SERVICE REGISTRATION:

   Update Program.cs:
   ```csharp
   // Add: builder.Services.AddScoped<IPromptGenerationService, PromptGenerationService>();
   // Follow exact pattern used for other services
   ```

4. UNIT TESTS (AIProjectOrchestrator.UnitTests):

   Create Services/PromptGenerationServiceTests.cs:
   ```csharp
   // Test class with proper setup/teardown
   // Test: GeneratePromptAsync_WithValidRequest_ReturnsResponse
   // Test: GetPromptStatusAsync_WithValidId_ReturnsStatus  
   // Test: CanGeneratePromptAsync_WithValidStoryId_ReturnsTrue
   // Test: GeneratePromptAsync_WithCancellation_ThrowsOperationCanceledException
   
   // Use same testing patterns as RequirementsAnalysisServiceTests
   // Mock dependencies properly, verify async behavior
   ```

TECHNICAL REQUIREMENTS:
- Follow exact coding patterns from existing services
- Use same namespace conventions: AIProjectOrchestrator.Domain.Models, etc.
- Include proper XML documentation comments
- Use consistent error handling patterns
- Implement proper async/await with ConfigureAwait(false)
- Include CancellationToken support throughout

VALIDATION CRITERIA:
- Solution builds without errors
- All unit tests pass
- Service properly registered in DI container
- Models follow existing conventions
- Interface contract is complete and usable

IMPLEMENTATION NOTES:
- This is foundation only - no AI integration, instruction loading, or complex logic yet
- Focus on establishing clean contracts and data structures
- GeneratePromptAsync should return a placeholder response with generated GUID
- Use simple in-memory storage with ConcurrentDictionary<Guid, object>
- Follow the exact dependency injection and logging patterns from other services

DELIVERABLES:
1. Complete domain models with proper validation attributes
2. Service interface with comprehensive method signatures
3. Basic service implementation with placeholder logic
4. Service registration in Program.cs
5. Comprehensive unit tests covering all public methods
6. Verify solution builds and all tests pass (including existing tests)

Do not implement: AI integration, instruction loading, story retrieval, API endpoints, integration tests. These are handled in subsequent phases.

Focus on creating a solid foundation that establishes the contracts and patterns for the complete implementation.
```

**Key Success Metrics for Phase 1**:
- Solution compiles without warnings
- All existing tests continue passing  
- New unit tests achieve >90% coverage on implemented methods
- Service follows exact patterns from your existing services
- Ready to extend with business logic in Phase 2

This foundation approach ensures you have working, testable contracts before tackling the complex prompt generation logic.
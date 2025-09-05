## Phase 2 Implementation Prompt

```
CONTEXT: .NET 9 AI Project Orchestrator implementing Phase 4 (Prompt Generation) - Phase 2 Story Retrieval Integration. You're building on the Phase 1 foundation to add story access and prerequisite validation.

PROJECT STATUS: Phase 1 complete with PromptGenerationService foundation, contracts, and basic unit tests. Phase 2 adds the ability to retrieve individual user stories and validate prerequisites.

PHASE 2 OBJECTIVE: Enable PromptGenerationService to access individual user stories and validate that stories are approved before prompt generation.

REQUIREMENTS:

1. EXTEND STORY GENERATION SERVICE:

   Update Domain/Interfaces/IStoryGenerationService.cs:
   ```csharp
   // Add new method: GetIndividualStoryAsync(Guid storyGenerationId, int storyIndex, CancellationToken) → Task<UserStory>
   // Add new method: GetAllStoriesAsync(Guid storyGenerationId, CancellationToken) → Task<List<UserStory>>
   // Add new method: GetStoryCountAsync(Guid storyGenerationId, CancellationToken) → Task<int>
   
   // UserStory model should include:
   // - Id (Guid), Title (string), Description (string), AcceptanceCriteria (List<string>)
   // - Priority (string), StoryPoints (int?), Tags (List<string>)
   ```

   Update Application/Services/StoryGenerationService.cs:
   ```csharp
   // Implement the new methods above
   // Parse stored story generation results to extract individual stories
   // Store stories in structured format during generation (modify existing logic)
   // Return individual stories by index or return all stories as list
   ```

2. UPDATE PROMPT GENERATION SERVICE:

   Update Application/Services/PromptGenerationService.cs:
   ```csharp
   // Add constructor dependencies:
   // - IStoryGenerationService _storyGenerationService
   // - IInstructionService _instructionService (for future use)
   
   // Implement CanGeneratePromptAsync:
   // 1. Check if storyGenerationId exists and is approved
   // 2. Validate that the specific story index exists
   // 3. Return true only if all prerequisites are met
   
   // Update GeneratePromptAsync:
   // 1. Call CanGeneratePromptAsync first
   // 2. If validation fails, throw meaningful exception
   // 3. Retrieve the specific story using IStoryGenerationService
   // 4. Store story data for future prompt generation (Phase 3)
   // 5. Return response with story title in placeholder prompt content
   ```

3. ADD STORY ID RESOLUTION:

   Update Domain/Models/PromptGeneration.cs:
   ```csharp
   // Modify PromptGenerationRequest:
   // - Change StoryId to StoryGenerationId (Guid) 
   // - Add StoryIndex (int) - which story from the generated set
   // OR
   // - Add StoryIdentifier class with GenerationId + Index properties
   
   // Add validation attributes for required fields
   ```

4. PREREQUISITE VALIDATION LOGIC:

   Create Application/Services/PromptPrerequisiteValidator.cs:
   ```csharp
   // Static class or service for validation logic
   // Method: ValidateStoryApprovalAsync(Guid storyGenerationId) → Task<bool>
   // Method: ValidateStoryExistsAsync(Guid storyGenerationId, int storyIndex) → Task<bool>
   // Use existing services to check approval status
   ```

5. ENHANCED ERROR HANDLING:

   Create Domain/Exceptions/PromptGenerationExceptions.cs:
   ```csharp
   // StoryNotApprovedException: When stories aren't approved yet
   // StoryNotFoundException: When story index doesn't exist
   // InvalidStoryGenerationException: When generation ID is invalid
   // Follow existing exception patterns from other services
   ```

6. UNIT TESTS EXPANSION:

   Update Tests/Services/PromptGenerationServiceTests.cs:
   ```csharp
   // Add mock for IStoryGenerationService
   // Test: CanGeneratePromptAsync_WithApprovedStories_ReturnsTrue
   // Test: CanGeneratePromptAsync_WithUnapprovedStories_ReturnsFalse
   // Test: CanGeneratePromptAsync_WithInvalidStoryId_ReturnsFalse
   // Test: GeneratePromptAsync_WithInvalidPrerequisites_ThrowsException
   // Test: GeneratePromptAsync_WithValidStory_IncludesStoryTitle
   
   // Mock story service to return test data
   // Verify prerequisite validation is called correctly
   ```

   Create Tests/Services/StoryGenerationServiceTests.cs (if not exists):
   ```csharp
   // Test: GetIndividualStoryAsync_WithValidIndex_ReturnsStory
   // Test: GetAllStoriesAsync_WithValidId_ReturnsAllStories
   // Test: GetStoryCountAsync_WithValidId_ReturnsCorrectCount
   // Test: GetIndividualStoryAsync_WithInvalidIndex_ThrowsException
   ```

TECHNICAL REQUIREMENTS:
- Maintain exact coding patterns from existing services
- Use proper async/await with CancellationToken throughout
- Follow existing error handling and logging patterns
- Ensure story parsing logic handles realistic story generation output
- Add comprehensive input validation with meaningful error messages

INTEGRATION POINTS:
- Must work with existing StoryGenerationService results
- Should validate against existing ReviewService approval status
- Prepare for AI integration in Phase 3 (context assembly)

TESTING STRATEGY:
- Mock all external dependencies (IStoryGenerationService, etc.)
- Create realistic test data for user stories
- Test edge cases: empty stories, invalid indices, missing approvals
- Verify prerequisite validation prevents unauthorized prompt generation

VALIDATION CRITERIA:
- All existing tests continue to pass
- New functionality works with realistic story data
- Prerequisite validation correctly blocks invalid requests
- Service can retrieve individual stories from story generation results
- Error handling provides clear, actionable error messages

DELIVERABLES:
1. Extended IStoryGenerationService with individual story access
2. Updated PromptGenerationService with prerequisite validation
3. UserStory model with complete story data structure
4. Custom exceptions for prompt generation failures
5. Comprehensive unit tests covering all new functionality
6. Integration with existing review/approval workflow

IMPORTANT: This phase should enable the PromptGenerationService to access and validate story data, but still return placeholder prompts. The actual AI-powered prompt generation happens in Phase 3.

Focus on creating robust story access patterns and bulletproof prerequisite validation that prevents invalid prompt generation requests.
```

**Key Success Metrics for Phase 2**:
- Can retrieve individual stories from approved story generation results
- Prerequisite validation correctly blocks unauthorized requests  
- Service integration follows existing patterns
- Comprehensive error handling with meaningful exceptions
- Ready for AI-powered prompt generation in Phase 3

This phase establishes the data access foundation needed for intelligent prompt generation.
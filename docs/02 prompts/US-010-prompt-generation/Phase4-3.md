## Implementation Overview

You're implementing **Phase 4: Prompt Generation** - a service that transforms individual approved user stories into comprehensive coding prompts for AI assistants. This is the final stage of my AI Project Orchestrator's 4-stage workflow (Requirements → Planning → Stories → **Prompts**).

**Current Status**: 
- Phase 1: Foundation & data models ✅
- Phase 2: Story retrieval & prerequisite validation ✅  
- Phase 3: AI-powered prompt generation ← **Starting here**
- Phase 4: API & testing completion

**Architecture Achievement**: We've successfully created a service composition that validates prerequisites, retrieves individual stories, and is ready for AI orchestration to generate enterprise-grade coding prompts.

---

## Phase 3 Implementation Prompt

```
CONTEXT: .NET 9 AI Project Orchestrator implementing Phase 4 (Prompt Generation) - Phase 3 AI-Powered Prompt Generation. You're implementing the core business logic that uses AI to transform individual user stories into comprehensive coding prompts.

PROJECT STATUS: Phases 1-2 complete with working story retrieval, prerequisite validation, and solid service foundation. Phase 3 adds the AI orchestration that generates actual enterprise-grade coding prompts.

PHASE 3 OBJECTIVE: Implement AI-powered prompt generation that combines individual user stories with project context to create comprehensive coding prompts for AI assistants.

REQUIREMENTS:

1. CREATE INSTRUCTION FILE:

   Create Instructions/PromptGenerator.md:
   ```markdown
   # AI Prompt Engineering Specialist

   ## Role
   You are an enterprise prompt engineering specialist who creates comprehensive, technical prompts for AI coding assistants to implement user stories.

   ## Task
   Transform individual user stories into detailed, actionable coding prompts that include all context needed for implementation.

   ## Input Context
   - Individual user story with acceptance criteria
   - Project architecture and technical decisions from approved planning
   - Related stories and integration points
   - Technical preferences and coding standards

   ## Output Format
   Generate a structured prompt with these sections:

   ### IMPLEMENTATION TASK
   [Clear, specific task title derived from user story]

   ### BUSINESS REQUIREMENTS  
   [User story acceptance criteria translated to technical requirements]
   [Any business rules or constraints that affect implementation]

   ### TECHNICAL CONTEXT
   [Relevant architecture decisions from project planning]
   [Technology stack and frameworks to use]
   [Integration points with other components]

   ### IMPLEMENTATION REQUIREMENTS
   - File structure and naming conventions
   - Required classes, interfaces, and methods
   - Data models and validation requirements
   - Error handling and logging specifications
   - Performance and security considerations

   ### TESTING REQUIREMENTS
   - Unit test specifications with expected coverage
   - Integration test requirements
   - Acceptance criteria validation steps
   - Mock/stub requirements for dependencies

   ### CODE QUALITY STANDARDS
   - .NET coding conventions and best practices
   - Clean Architecture compliance requirements
   - SOLID principles application
   - Documentation and commenting standards

   ### INTEGRATION SPECIFICATIONS
   - Dependencies on other components
   - API endpoints or interfaces to implement
   - Database schema requirements
   - Configuration settings needed

   ### DELIVERABLES CHECKLIST
   - [ ] Implementation files with proper structure
   - [ ] Unit test files with comprehensive coverage
   - [ ] Integration test updates where needed
   - [ ] Documentation updates (README, API docs)
   - [ ] Configuration updates if required

   ## Constraints
   - Generate self-contained prompts that don't require additional context
   - Include specific technical details, not vague instructions
   - Ensure prompts are implementable by senior developers
   - Maintain consistency with project architecture decisions
   - Focus on production-ready, enterprise-grade implementation

   ## Examples
   [Include 1-2 sample prompts for reference - create realistic examples]
   ```

2. IMPLEMENT CONTEXT ASSEMBLY:

   Create Application/Services/PromptContextAssembler.cs:
   ```csharp
   public class PromptContextAssembler
   {
       // Dependencies: IProjectPlanningService, IStoryGenerationService, ILogger
       
       // Method: AssembleContextAsync(Guid storyGenerationId, int storyIndex) → Task<PromptContext>
       // - Retrieve individual story
       // - Get approved project planning context
       // - Get related stories for integration context
       // - Combine into structured context object
       
       // Method: GetProjectArchitectureAsync(Guid storyGenerationId) → Task<string>
       // - Extract architecture decisions from project planning
       // - Format for prompt consumption
       
       // Method: GetRelatedStoriesAsync(Guid storyGenerationId, int currentIndex) → Task<List<UserStory>>
       // - Get stories that might integrate with current story
       // - Limit to essential context to manage prompt size
   }
   
   public class PromptContext
   {
       public UserStory TargetStory { get; set; }
       public string ProjectArchitecture { get; set; }
       public List<UserStory> RelatedStories { get; set; }
       public Dictionary<string, string> TechnicalPreferences { get; set; }
       public string IntegrationGuidance { get; set; }
   }
   ```

3. COMPLETE PROMPT GENERATION SERVICE:

   Update Application/Services/PromptGenerationService.cs:
   ```csharp
   // Add dependencies:
   // - IInstructionService _instructionService
   // - IAIClientFactory _aiClientFactory
   // - PromptContextAssembler _contextAssembler
   
   // Complete GeneratePromptAsync implementation:
   public async Task<PromptGenerationResponse> GeneratePromptAsync(
       PromptGenerationRequest request, CancellationToken cancellationToken)
   {
       // 1. Validate prerequisites (existing logic)
       // 2. Assemble comprehensive context using PromptContextAssembler
       // 3. Load PromptGenerator instructions
       // 4. Create AI request combining instructions + context
       // 5. Call Claude API for prompt generation
       // 6. Parse and validate generated prompt
       // 7. Submit for review via IReviewService
       // 8. Return response with generated prompt and review ID
   }
   
   // Add helper methods:
   // - ValidateGeneratedPrompt(string prompt) → bool
   // - FormatPromptForStorage(string prompt) → string
   // - CreateAIRequest(instructions, context) → AIRequest
   ```

4. CONTEXT SIZE MANAGEMENT:

   Create Application/Services/ContextOptimizer.cs:
   ```csharp
   public class ContextOptimizer
   {
       // Method: OptimizeContext(PromptContext context) → PromptContext
       // - Summarize lengthy project planning content
       // - Limit related stories to most relevant ones
       // - Compress architecture details while preserving key information
       // - Target ~40% of AI context window (estimate 8000-10000 characters)
       
       // Method: EstimateTokenCount(string content) → int
       // - Rough token estimation for context management
       
       // Method: PrioritizeRelatedStories(List<UserStory> stories, UserStory target) → List<UserStory>
       // - Select most relevant stories based on tags, titles, dependencies
   }
   ```

5. AI REQUEST ORCHESTRATION:

   Update the GeneratePromptAsync method:
   ```csharp
   // Create comprehensive AI request:
   var aiRequest = new AIRequest
   {
       Instructions = await _instructionService.GetInstructionAsync("PromptGenerator", cancellationToken),
       Content = $@"
   TARGET USER STORY:
   {context.TargetStory.Title}
   {context.TargetStory.Description}
   
   ACCEPTANCE CRITERIA:
   {string.Join("\n", context.TargetStory.AcceptanceCriteria)}
   
   PROJECT ARCHITECTURE:
   {context.ProjectArchitecture}
   
   RELATED STORIES FOR INTEGRATION:
   {FormatRelatedStories(context.RelatedStories)}
   
   TECHNICAL PREFERENCES:
   {FormatTechnicalPreferences(context.TechnicalPreferences)}
   
   Generate a comprehensive coding prompt following the specified format.",
       ModelPreferences = new ModelPreferences { PreferredProvider = "Claude" }
   };
   ```

6. PROMPT VALIDATION AND STORAGE:

   Add validation logic:
   ```csharp
   private bool ValidateGeneratedPrompt(string prompt)
   {
       // Check for required sections: IMPLEMENTATION TASK, BUSINESS REQUIREMENTS, etc.
       // Verify minimum length and completeness
       // Ensure technical specifications are present
       // Validate that acceptance criteria are addressed
       return hasAllRequiredSections && hasMinimumLength && hasTechnicalDetails;
   }
   
   // Store generated prompts with metadata:
   private void StoreGeneratedPrompt(Guid promptId, string prompt, PromptContext context)
   {
       var promptData = new
       {
           Prompt = prompt,
           SourceStory = context.TargetStory,
           GeneratedAt = DateTime.UtcNow,
           ContextSize = EstimateContextSize(context)
       };
       _promptStorage[promptId] = promptData;
   }
   ```

7. ENHANCED UNIT TESTS:

   Update Tests/Services/PromptGenerationServiceTests.cs:
   ```csharp
   // Add mocks for new dependencies:
   // - Mock<IInstructionService>
   // - Mock<IAIClientFactory> 
   // - Mock<PromptContextAssembler>
   
   // New tests:
   // Test: GeneratePromptAsync_WithValidStory_CallsAIService
   // Test: GeneratePromptAsync_WithLargeContext_OptimizesContext
   // Test: GeneratePromptAsync_WithInvalidPrompt_RetriesGeneration
   // Test: GeneratePromptAsync_Success_SubmitsForReview
   // Test: GeneratePromptAsync_AIFailure_HandlesGracefully
   
   // Integration-style tests:
   // Test: GeneratePromptAsync_EndToEnd_ProducesValidPrompt
   // Test: ContextAssembly_WithRealData_CreatesComprehensiveContext
   ```

   Create Tests/Services/PromptContextAssemblerTests.cs:
   ```csharp
   // Test context assembly logic:
   // Test: AssembleContextAsync_WithValidStory_ReturnsCompleteContext
   // Test: GetRelatedStoriesAsync_ReturnsRelevantStories
   // Test: GetProjectArchitectureAsync_ReturnsFormattedArchitecture
   ```

TECHNICAL REQUIREMENTS:
- Follow existing service patterns exactly
- Use Claude API via IAIClientFactory for prompt generation
- Implement comprehensive error handling and logging
- Manage context size to prevent AI token limit issues
- Ensure generated prompts are enterprise-grade and actionable

CONTEXT MANAGEMENT STRATEGY:
- Target ~8000-10000 characters for combined context
- Prioritize current story details over related stories
- Summarize project planning content while preserving key decisions
- Include only most relevant integration points

QUALITY CRITERIA:
- Generated prompts must include all required sections
- Prompts should be implementable without additional context
- Technical specifications must be specific and actionable
- Integration requirements must be clearly defined

DELIVERABLES:
1. Complete PromptGenerator.md instruction file with examples
2. PromptContextAssembler for intelligent context assembly
3. Completed PromptGenerationService with AI orchestration
4. ContextOptimizer for managing prompt size
5. Comprehensive unit tests covering AI integration
6. Error handling for AI service failures and invalid prompts

INTEGRATION POINTS:
- Uses existing IAIClientFactory and IReviewService
- Integrates with IProjectPlanningService for architecture context
- Leverages Phase 2 story retrieval functionality
- Maintains existing error handling and logging patterns

Focus on creating prompts that are comprehensive enough for senior developers to implement stories without requiring additional context or clarification.
```

**Key Success Metrics for Phase 3**:
- Generates comprehensive, actionable coding prompts
- Properly assembles context from multiple sources
- Manages context size to stay within AI token limits
- Produces prompts that include all necessary technical details
- Ready for API exposure and end-to-end testing in Phase 4

This phase transforms your orchestrator into a sophisticated prompt engineering platform that creates enterprise-grade coding instructions.
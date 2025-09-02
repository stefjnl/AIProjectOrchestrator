# Qwen Implementation Prompt: US-001 Service Configuration System

## Context
You are implementing a service configuration system for an AI Project Orchestrator built with .NET 9 Clean Architecture. The existing solution has Domain, Application, Infrastructure, and API layers with proper dependency injection, Entity Framework Core setup, and working Docker containerization.

## Objective
Create a service that loads AI instruction files dynamically with intelligent caching based on file modification times. This service will provide AI sub-agent instructions to orchestrator services without requiring container restarts during development iterations.

## Implementation Requirements

### 1. Domain Layer Interface
**Location**: `src/AIProjectOrchestrator.Domain/Services/IInstructionService.cs`

Create interface with these signatures:
```csharp
public interface IInstructionService
{
    Task<InstructionContent> GetInstructionAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<bool> IsValidInstructionAsync(string serviceName, CancellationToken cancellationToken = default);
}
```

**Location**: `src/AIProjectOrchestrator.Domain/Models/InstructionContent.cs`

Create model class:
```csharp
public class InstructionContent
{
    public string ServiceName { get; set; }
    public string Content { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; }
}
```

### 2. Application Layer Implementation
**Location**: `src/AIProjectOrchestrator.Application/Services/InstructionService.cs`

Implement with these features:
- **Dynamic Loading**: Check file modification time before each request, reload only when changed
- **Service Name Mapping**: Convert service names to file names using convention:
  - `RequirementsAnalysisService` → `RequirementsAnalyst.md`
  - `ProjectPlanningService` → `ProjectPlanner.md` 
  - `StoryGenerationService` → `StoryGenerator.md`
- **Content Validation**: 
  - Minimum 100 characters
  - Contains sections: "Role", "Task", "Constraints" (case-insensitive)
  - Valid UTF-8 encoding
- **Intelligent Caching**: In-memory cache with modification time tracking using `ConcurrentDictionary`
- **Structured Logging**: Use `ILogger<InstructionService>` with correlation IDs
- **Error Handling**: Custom exceptions for missing files, validation failures, I/O errors

### 3. Configuration Setup
**Location**: `src/AIProjectOrchestrator.API/appsettings.json`

Add configuration section:
```json
{
  "InstructionSettings": {
    "InstructionsPath": "Instructions",
    "CacheTimeoutMinutes": 5,
    "RequiredSections": ["Role", "Task", "Constraints"],
    "MinimumContentLength": 100
  }
}
```

**Location**: `src/AIProjectOrchestrator.Application/Configuration/InstructionSettings.cs`

Create settings class for `IOptions<InstructionSettings>` pattern.

### 4. Dependency Injection Registration
**Location**: `src/AIProjectOrchestrator.API/Program.cs`

Register service as Singleton (instructions don't change frequently and caching is important).

### 5. Sample Instruction File
**Location**: `Instructions/RequirementsAnalyst.md`

Create realistic sample file (~1000+ words) with:
- **Role**: AI persona definition for requirements analysis
- **Task**: Specific responsibilities and expected outputs
- **Constraints**: Limitations, format requirements, quality standards
- **Examples**: Sample input/output scenarios

### 6. Testing Implementation

**Unit Tests** (`tests/AIProjectOrchestrator.UnitTests/Services/InstructionServiceTests.cs`):
- Test instruction loading and caching behavior
- Test service name to filename mapping
- Test content validation rules
- Test error scenarios (missing files, invalid content)
- Test modification time tracking and cache invalidation

**Integration Tests** (`tests/AIProjectOrchestrator.IntegrationTests/Services/InstructionServiceIntegrationTests.cs`):
- Test file system integration with real files
- Test dependency injection registration
- Test end-to-end instruction loading workflow

### 7. Directory Structure
Create this structure in project root:
```
Instructions/
├── RequirementsAnalyst.md  (sample file for testing)
└── .gitkeep                (ensure directory exists in git)
```

## Technical Specifications

### Implementation Patterns
- Use `async/await` for all file I/O operations
- Implement proper exception handling with custom exception types
- Use `IOptions<InstructionSettings>` for configuration
- Follow Clean Architecture dependency rules (no file I/O in Domain layer)
- Use structured logging with correlation IDs and performance metrics

### Performance Requirements
- Cache instructions in memory with modification time checking
- File system checks should be lightweight (avoid reading entire file for validation)
- Support concurrent access with thread-safe caching

### Error Handling Strategy
- Return meaningful error messages for missing or corrupted files
- Log all file operations and errors with structured logging
- Graceful degradation: continue service operation even with some instruction files missing

## Critical Evaluation Points

When reviewing the generated code, specifically examine:
1. **Interface Design**: Single responsibility, clear contracts, appropriate async patterns
2. **Caching Logic**: Thread safety, memory efficiency, cache invalidation correctness
3. **File I/O Patterns**: Proper async usage, exception handling, resource disposal
4. **Dependency Injection**: Correct lifetime management, proper abstraction boundaries
5. **Validation Logic**: Comprehensive but not overly complex, maintainable rules
6. **Test Coverage**: Realistic scenarios, edge cases, integration validation

## Definition of Done
- [ ] All interfaces and classes compile without errors
- [ ] Service properly registered in DI container
- [ ] Unit tests achieve >80% code coverage and pass
- [ ] Integration test demonstrates end-to-end functionality
- [ ] Sample RequirementsAnalyst.md file created with realistic content
- [ ] Configuration properly integrated with appsettings.json
- [ ] Structured logging implemented throughout
- [ ] All existing tests continue to pass
- [ ] Docker containers build and run successfully

## Constraints
- Only modify/add files as specified - do not restructure existing working code
- Follow existing project conventions and patterns
- Maintain Clean Architecture boundaries
- Use existing logging and DI infrastructure
- Ensure backwards compatibility with current Docker setup
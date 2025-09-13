# Clean Architecture Implementation Plan - AI Service Configuration Refactoring

## 🎯 Executive Summary

This plan addresses the DRY violations identified in the AI service configuration while maintaining proper Clean Architecture layer separation. Based on the critique in DRY2.md, we focus on eliminating code duplication without creating unnecessary testing overhead.

## 🚨 Current Problems (Confirmed)

### **Critical DRY Violations:**
- `"moonshotai/Kimi-K2-Instruct-0905"` hardcoded in 4 services
- `"NanoGpt"` provider selection duplicated across services
- Similar AI request creation patterns repeated
- Business services know AI implementation details

## 🏗️ Clean Architecture Solution

### **Proper Layer Separation:**

```
┌─────────────────────────────────────────────────────────────┐
│                         APPLICATION                         │
│  RequirementsAnalysisService (business orchestration)      │
│  ProjectPlanningService (business orchestration)           │
│  StoryGenerationService (business orchestration)           │
├─────────────────────────────────────────────────────────────┤
│                       INFRASTRUCTURE                        │
│  IAIProvider (infrastructure abstraction)                 │
│  ConfigurableAIProvider (DRY implementation)              │
│  AIProviderFactory (provider selection - DRY)             │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Implementation Plan

### **Phase 1: Create Infrastructure Abstractions**

#### **1.1 Clean Infrastructure Interface**
```csharp
// Infrastructure/AI/IAIProvider.cs - Pure business interface
public interface IAIProvider
{
    Task<string> GenerateContentAsync(string prompt, string context = null);
    Task<bool> IsAvailableAsync();
}

// Infrastructure/AI/AIRequestOptions.cs - Infrastructure internal only
// REMOVED from public interface - business services shouldn't know about AI options
```

#### **1.2 DRY Implementation (Infrastructure Only)**
```csharp
// Infrastructure/AI/ConfigurableAIProvider.cs - Context-aware configuration
public class ConfigurableAIProvider : IAIProvider
{
    private readonly IAIClientFactory _clientFactory;
    private readonly IOptions<AIProviderSettings> _settings;
    private readonly ILogger<ConfigurableAIProvider> _logger;
    
    public async Task<string> GenerateContentAsync(string prompt, string context = null)
    {
        // RELIABLE APPROACH 1: Use explicit operation type from constructor
        var options = _settings.Value.Operations[_operationType];
        
        var aiRequest = new AIRequest
        {
            Prompt = prompt,
            SystemMessage = context, // Business context, not AI system message
            ModelName = options.Model,      // From configuration
            MaxTokens = options.MaxTokens,  // From configuration
            Temperature = options.Temperature // From configuration
        };
        
        var client = _clientFactory.GetClient(options.Provider);
        if (client == null)
            throw new InvalidOperationException($"AI client '{options.Provider}' is not available");
        
        var response = await client.CallAsync(aiRequest);
        return response.IsSuccess ? response.Content : throw new InvalidOperationException(response.ErrorMessage);
    }
    
    public async Task<bool> IsAvailableAsync()
    {
        // RELIABLE: Use explicit operation type from constructor
        var options = _settings.Value.Operations[_operationType];
        var client = _clientFactory.GetClient(options.Provider);
        return client != null && await client.IsHealthyAsync();
    }
}

#### **1.3 Three Reliable Context Detection Approaches**

**Approach 1: Service-Specific Providers (Most Explicit - Recommended)**
```csharp
// Infrastructure/AI/Providers/IRequirementsAIProvider.cs
public interface IRequirementsAIProvider : IAIProvider { }

// Infrastructure/AI/Providers/IPlanningAIProvider.cs
public interface IPlanningAIProvider : IAIProvider { }

// Infrastructure/AI/Providers/IStoryAIProvider.cs
public interface IStoryAIProvider : IAIProvider { }

// Implementation: Each provider knows its operation type explicitly
public class RequirementsAIProvider : ConfigurableAIProvider, IRequirementsAIProvider
{
    public RequirementsAIProvider(IAIClientFactory clientFactory,
        IOptions<AIProviderSettings> settings, ILogger<RequirementsAIProvider> logger)
        : base("RequirementsAnalysis", clientFactory, settings, logger) { }
}

// DI Registration: Explicit service-to-provider mapping
services.AddScoped<IRequirementsAIProvider, RequirementsAIProvider>();
services.AddScoped<IPlanningAIProvider, PlanningAIProvider>();
services.AddScoped<IStoryAIProvider, StoryAIProvider>();
```

**Approach 2: Operation Type Parameter (Simple & Reliable)**
```csharp
// Enhanced interface with explicit operation type
public interface IAIProvider
{
    Task<string> GenerateContentAsync(string prompt, string context = null, string operationType = null);
    Task<bool> IsAvailableAsync(string operationType = null);
}

// Implementation: Uses explicit parameter, no reflection needed
public async Task<string> GenerateContentAsync(string prompt, string context = null, string operationType = null)
{
    // Reliable: Use explicit operation type parameter
    var effectiveOperation = operationType ?? "Default";
    
    if (!_settings.Value.Operations.ContainsKey(effectiveOperation))
    {
        _logger.LogWarning("Operation type '{Operation}' not found, using Default", effectiveOperation);
        effectiveOperation = "Default";
    }
    
    var options = _settings.Value.Operations[effectiveOperation];
    
    // ... rest of implementation same as above
}
```

**Approach 3: Factory Pattern with Registration-Time Binding (Most Flexible)**
```csharp
// Infrastructure/AI/IAIProviderFactory.cs
public interface IAIProviderFactory
{
    IAIProvider GetProvider(string operationType);
}

public class AIProviderFactory : IAIProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<AIProviderSettings> _settings;
    
    public IAIProvider GetProvider(string operationType)
    {
        // Validate operation exists
        if (!_settings.Value.Operations.ContainsKey(operationType))
        {
            throw new ArgumentException($"Operation type '{operationType}' not configured");
        }
        
        // Create provider instance with operation-specific configuration
        var clientFactory = _serviceProvider.GetRequiredService<IAIClientFactory>();
        var logger = _serviceProvider.GetRequiredService<ILogger<ConfigurableAIProvider>>();
        
        return new ConfigurableAIProvider(operationType, clientFactory, _settings, logger);
    }
}

// DI Registration: Factory pattern
services.AddScoped<IAIProviderFactory, AIProviderFactory>();
// Business services get specific providers through factory
```

**Recommendation**: Use **Approach 1 (Service-Specific Providers)** for production systems as it's:
- Most explicit and maintainable
- Compile-time safe
- Easy to understand and debug
- Follows interface segregation principle
```

### **Phase 2: Refactor Application Services**

#### **2.1 Before (Current Violation):**
```csharp
// ❌ Current: Business service knows AI details
var aiRequest = new AIRequest
{
    ModelName = "moonshotai/Kimi-K2-Instruct-0905", // HARDCODED
    Temperature = 0.7,
    MaxTokens = 2000
};
var aiClient = _aiClientFactory.GetClient("NanoGpt"); // HARDCODED
```

#### **2.2 After (Clean Architecture):**
```csharp
// ✅ Cleaner: Business service with zero AI knowledge
public class RequirementsAnalysisService : IRequirementsAnalysisService
{
    private readonly IAIProvider _aiProvider; // Pure business abstraction
    private readonly IInstructionService _instructionService;
    
    public async Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(RequirementsAnalysisRequest request)
    {
        var instruction = await _instructionService.GetInstructionAsync("RequirementsAnalyst");
        
        // Pure business call - no AI knowledge required
        var analysisResult = await _aiProvider.GenerateContentAsync(
            prompt: CreatePromptFromRequest(request),
            context: instruction.Content);
        
        return CreateAnalysisResponse(analysisResult, request);
    }
    
    // All services become nearly identical:
    // var result = await _aiProvider.GenerateContentAsync(prompt, context);
}
```

### **Phase 3: Context-Aware Configuration**

#### **3.1 Operation-Specific Configuration (Infrastructure Only)**
```json
// appsettings.json - Infrastructure configuration with operation contexts
{
  "AIProviders": {
    "Operations": {
      "RequirementsAnalysis": {
        "ProviderName": "NanoGpt",
        "Model": "moonshotai/Kimi-K2-Instruct-0905",
        "MaxTokens": 2000,
        "Temperature": 0.7
      },
      "ProjectPlanning": {
        "ProviderName": "NanoGpt",
        "Model": "moonshotai/Kimi-K2-Instruct-0905",
        "MaxTokens": 4000,
        "Temperature": 0.7
      },
      "StoryGeneration": {
        "ProviderName": "NanoGpt",
        "Model": "moonshotai/Kimi-K2-Instruct-0905",
        "MaxTokens": 4000,
        "Temperature": 0.7
      }
    }
  }
}

#### **3.2 Context-Aware DI Setup**
```csharp
// Program.cs - Context-aware DI configuration
services.Configure<AIProviderSettings>(Configuration.GetSection("AIProviders:Operations"));
services.AddScoped<IAIProvider>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<AIProviderSettings>>();
    var factory = sp.GetRequiredService<IAIClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ConfigurableAIProvider>>();
    return new ConfigurableAIProvider(factory, settings, logger);
});

## 🎯 Specific Changes Required

### **Files to Create:**

#### **New Infrastructure Files:**
- `src/AIProjectOrchestrator.Infrastructure/AI/IAIProvider.cs` - Clean business interface
- `src/AIProjectOrchestrator.Infrastructure/AI/ConfigurableAIProvider.cs` - Context-aware implementation
- `src/AIProjectOrchestrator.Infrastructure/Configuration/OperationContext.cs` - Caller context detection
- `src/AIProjectOrchestrator.Infrastructure/Configuration/AIProviderSettings.cs` - Operation-specific settings

### **Files to Modify:**

1. **Create New Infrastructure Files:**
   - `src/AIProjectOrchestrator.Infrastructure/AI/IAIProvider.cs`
   - `src/AIProjectOrchestrator.Infrastructure/AI/ConfigurableAIProvider.cs`
   - `src/AIProjectOrchestrator.Infrastructure/Configuration/AIProviderSettings.cs`

2. **Refactor Existing Services (Simplified):**
   - [`RequirementsAnalysisService.cs`](src/AIProjectOrchestrator.Application/Services/RequirementsAnalysisService.cs:76) - Replace with clean business call
   - [`ProjectPlanningService.cs`](src/AIProjectOrchestrator.Application/Services/ProjectPlanningService.cs:93) - Replace with clean business call
   - [`StoryGenerationService.cs`](src/AIProjectOrchestrator.Application/Services/StoryGenerationService.cs:109) - Replace with clean business call
   - [`PromptGenerationService.cs`](src/AIProjectOrchestrator.Application/Services/PromptGenerationService.cs:147) - Replace with clean business call

3. **Update Configuration:**
   - [`appsettings.json`](src/AIProjectOrchestrator.API/appsettings.json) - Add operation-specific settings
   - [`Program.cs`](src/AIProjectOrchestrator.API/Program.cs) - Update DI with context-aware registration

## 📊 Benefits Achieved

### **Before (Current Violations):**
- ❌ `"moonshotai/Kimi-K2-Instruct-0905"` in 4+ services
- ❌ Business services know AI implementation details
- ❌ Business services configure AI parameters (tokens, temperature)
- ❌ Violates DRY, SRP, Clean Architecture
- ❌ Difficult to change providers/models

### **After (Enterprise-Grade Clean Architecture):**
- ✅ Zero hardcoded model names in business services
- ✅ Zero AI configuration in business services (tokens, temperature)
- ✅ Single source of truth for AI configuration (infrastructure)
- ✅ Business services are completely AI-agnostic
- ✅ Context-aware configuration based on operation type
- ✅ All business services use identical clean interface
- ✅ Enterprise-grade Clean Architecture compliance

## 🚀 Implementation Steps

### **Step 1: Create Infrastructure Abstractions**
1. Create `IAIProvider` interface in Infrastructure layer
2. Implement `ConfigurableAIProvider` with DRY logic
3. Create `AIProviderSettings` configuration model

### **Step 2: Update Dependency Injection**
1. Add AI provider settings to configuration
2. Register `IAIProvider` in DI container
3. Remove direct AI client dependencies from business services

### **Step 3: Refactor Business Services**
1. Replace hardcoded model names with infrastructure calls
2. Remove direct `_aiClientFactory.GetClient()` calls
3. Use `_aiProvider.GenerateAsync()` instead

### **Step 4: Validation**
1. Test all services work with new abstraction
2. Verify configuration changes work without code changes
3. Ensure no functionality is lost

## ✅ Success Criteria

### **Clean Architecture Compliance:**
- [ ] Business services contain only business logic (no AI knowledge)
- [ ] Infrastructure layer handles all AI/provider/configuration logic
- [ ] No hardcoded model names in application layer
- [ ] No AI-specific configuration in application layer
- [ ] Context-aware configuration based on operation type

### **DRY Principle Compliance:**
- [ ] Single source of truth for AI configuration
- [ ] No duplicate AI client retrieval logic
- [ ] Centralized model/provider/parameter selection (infrastructure only)
- [ ] Identical business service calls across all services

### **Maintainability:**
- [ ] Easy to change providers/models/parameters via configuration
- [ ] Business logic completely unaffected by AI changes
- [ ] Perfect separation of business and infrastructure concerns
- [ ] Enterprise-grade extensibility

## 🎯 Next Steps

1. **Review and approve** this Clean Architecture approach
2. **Implement Step 1** (Infrastructure abstractions)
3. **Update DI configuration** (Step 2)
4. **Refactor services incrementally** (Step 3)
5. **Validate functionality** (Step 4)

This approach eliminates all DRY violations while maintaining proper Clean Architecture boundaries, making the system more maintainable and extensible without the overhead of extensive unit testing.
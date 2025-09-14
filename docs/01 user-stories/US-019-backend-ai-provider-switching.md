# User Story: Backend AI Provider Switching Architecture (US-019)

## Story Description
As a **system administrator**, I want to be able to **easily switch between NanoGpt (Python proxy) and OpenRouter as the default AI provider** for all LLM operations, so that I can **optimize performance, costs, and reliability based on current requirements**.

## Business Value
- **Cost Optimization**: Switch to more cost-effective providers when budget constraints arise
- **Performance Tuning**: Use faster providers for time-sensitive operations
- **Reliability**: Fallback to stable providers during outages
- **Compliance**: Meet data residency and privacy requirements by selecting appropriate providers

## Acceptance Criteria

### 1. Configuration Management
```gherkin
Given the system is running
When I update the configuration
Then the system should support:
  - Environment-based provider selection (NanoGpt, OpenRouter, LMStudio, Claude)
  - Provider-specific API keys and endpoints
  - Runtime configuration changes without restart
  - Validation of provider availability before switching
```

### 2. Clean Architecture Implementation
```gherkin
Given the current provider architecture
When I implement the switching mechanism
Then it should adhere to:
  - Single Responsibility Principle for provider configuration
  - Open/Closed Principle for adding new providers
  - Dependency Inversion for provider abstractions
  - Interface Segregation for provider-specific features
```

### 3. Provider Health Monitoring
```gherkin
Given configured AI providers
When the system operates
Then it should continuously:
  - Monitor provider health and availability
  - Log provider performance metrics (response time, success rate)
  - Provide fallback mechanisms when primary provider fails
  - Alert administrators on provider issues
```

### 4. Operational Configuration
```gherkin
Given the system requirements
When configuring provider switching
Then it should support:
  - Per-operation provider selection (RequirementsAnalysis, ProjectPlanning, StoryGeneration, PromptGeneration)
  - Global default provider override
  - Environment variable configuration
  - JSON configuration file support
```

## Technical Requirements

### Current Architecture Analysis
- **ConfigurableAIProvider.cs**: Uses hardcoded provider names and base URLs
- **AIProviderSettings.cs**: Currently only supports operation-level configuration
- **Docker Configuration**: Uses environment variables for proxy setup
- **Proxy Service**: Python-based NanoGpt proxy running on port 5000

### Required Changes

#### 1. Provider Configuration Model
```csharp
public class ProviderConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool UseProxy { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public Dictionary<string, object> ProviderSpecific { get; set; } = new();
}
```

#### 2. Provider Factory Enhancement
```csharp
public interface IProviderFactory
{
    IAIClient CreateProvider(string providerName);
    Task<bool> ValidateProviderAsync(string providerName);
    IEnumerable<string> GetAvailableProviders();
    ProviderConfiguration GetConfiguration(string providerName);
}
```

#### 3. Configuration Structure
```json
{
  "AIProviders": {
    "DefaultProvider": "OpenRouter",
    "Providers": {
      "NanoGpt": {
        "BaseUrl": "http://nanogpt-proxy:5000",
        "UseProxy": true,
        "DefaultModel": "moonshotai/Kimi-K2-Instruct-0905"
      },
      "OpenRouter": {
        "BaseUrl": "https://openrouter.ai/api/v1",
        "UseProxy": false,
        "DefaultModel": "qwen/qwen3-coder"
      }
    },
    "Operations": {
      "RequirementsAnalysis": {
        "ProviderName": "OpenRouter",
        "Model": "qwen/qwen3-coder",
        "MaxTokens": 2000,
        "Temperature": 0.7
      }
    }
  }
}
```

### 4. Environment Variable Support
```bash
# Provider Selection
AI_DEFAULT_PROVIDER=OpenRouter
AI_FALLBACK_PROVIDER=NanoGpt

# OpenRouter Configuration
OPENROUTER_API_KEY=your-api-key
OPENROUTER_BASE_URL=https://openrouter.ai/api/v1

# NanoGpt Configuration
NANOGPT_API_KEY=your-api-key
NANOGPT_BASE_URL=http://nanogpt-proxy:5000
NANOGPT_USE_PROXY=true
```

## Implementation Tasks

### Phase 1: Core Architecture (Priority: High)
- [ ] Create `ProviderConfiguration` class with validation
- [ ] Implement `IProviderFactory` interface
- [ ] Update `ConfigurableAIProvider` to use new configuration model
- [ ] Add provider health check endpoints

### Phase 2: Configuration Management (Priority: High)
- [ ] Create configuration validation service
- [ ] Implement environment variable binding
- [ ] Add runtime configuration update support
- [ ] Create configuration migration utilities

### Phase 3: Monitoring & Logging (Priority: Medium)
- [ ] Add provider performance metrics
- [ ] Implement health check endpoints
- [ ] Create provider status dashboard
- [ ] Add alerting for provider failures

### Phase 4: Testing & Documentation (Priority: Medium)
- [ ] Create comprehensive unit tests for provider switching
- [ ] Add integration tests for configuration changes
- [ ] Document configuration options and migration guide
- [ ] Create troubleshooting guide for provider issues

## Definition of Done
- [ ] All providers can be switched via configuration without code changes
- [ ] System validates provider configuration on startup
- [ ] Health checks confirm provider availability
- [ ] Performance metrics are collected and logged
- [ ] Configuration changes can be made without system restart
- [ ] Comprehensive documentation is provided
- [ ] All existing functionality continues to work with new architecture

## Success Metrics
- **Configuration Change Time**: < 30 seconds to switch providers
- **Provider Validation**: < 5 seconds to validate provider availability
- **Health Check Success Rate**: > 99% uptime monitoring
- **Configuration Migration**: Zero-downtime configuration updates
- **Error Rate**: < 0.1% provider-related errors
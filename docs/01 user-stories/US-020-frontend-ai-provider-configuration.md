# User Story: Frontend AI Provider Configuration Management (US-020)

## Story Description
As a **system administrator or developer**, I want to be able to **manage AI provider configuration through a user-friendly web interface**, so that I can **easily switch between NanoGpt and OpenRouter providers without modifying configuration files or restarting services**.

## Business Value
- **Operational Efficiency**: Reduce configuration time from manual file editing to point-and-click interface
- **Error Prevention**: Validate configurations before applying changes to prevent system failures
- **Real-time Monitoring**: View provider health status and performance metrics in real-time
- **User Empowerment**: Enable non-technical users to manage AI provider settings safely

## Acceptance Criteria

### 1. Provider Configuration Dashboard
```gherkin
Given I am logged in as an administrator
When I navigate to the AI Provider Configuration page
Then I should see:
  - Current active provider with status indicators
  - Available providers list with health status
  - Configuration forms for each provider
  - Real-time provider performance metrics
  - Configuration validation results
```

### 2. Provider Switching Interface
```gherkin
Given multiple providers are configured
When I select a new provider from the dashboard
Then the system should:
  - Validate the new provider configuration
  - Test provider connectivity and API key validity
  - Show preview of configuration changes
  - Apply changes with zero downtime
  - Provide rollback capability if issues occur
```

### 3. Provider Health Monitoring
```gherkin
Given providers are configured
When viewing the dashboard
Then I should see:
  - Real-time connection status for each provider
  - Response time metrics for recent API calls
  - Success/failure rates for each provider
  - Last successful API call timestamp
  - Error messages and troubleshooting guidance
```

### 4. Configuration Validation
```gherkin
Given I am updating provider configuration
When I submit changes
Then the system should:
  - Validate API key format and permissions
  - Check endpoint accessibility
  - Verify model availability and permissions
  - Provide detailed validation feedback
  - Prevent invalid configurations from being applied

## Technical Requirements

### Current Frontend Analysis
- **API Client**: Uses centralized API client (`window.APIClient`) for all backend communication
- **Configuration**: Currently hardcoded in backend, no frontend configuration management
- **Error Handling**: Basic error messages, no provider-specific error handling
- **User Interface**: No dedicated configuration management UI

### Required Frontend Components

#### 1. Configuration Management API
```javascript
// New API endpoints needed
window.APIClient.getAIProviders = async () => {
    return this.get('/ai/providers');
};

window.APIClient.updateAIProvider = async (providerName, config) => {
    return this.post('/ai/providers/' + providerName, config);
};

window.APIClient.testAIProvider = async (providerName) => {
    return this.post('/ai/providers/' + providerName + '/test');
};

window.APIClient.getAIProviderHealth = async (providerName) => {
    return this.get('/ai/providers/' + providerName + '/health');
};
```

#### 2. Provider Configuration Form Component
```javascript
class ProviderConfigurationForm {
    constructor(containerId, providerName) {
        this.container = document.getElementById(containerId);
        this.providerName = providerName;
        this.form = null;
        this.validation = null;
    }

    async render() {
        const config = await APIClient.getAIProviderConfig(this.providerName);
        this.container.innerHTML = this.createFormHTML(config);
        this.bindEvents();
    }

    createFormHTML(config) {
        return `
            <div class="provider-config-form">
                <h3>${this.providerName} Configuration</h3>
                <div class="form-group">
                    <label>Base URL</label>
                    <input type="url" id="baseUrl" value="${config.baseUrl}" required>
                </div>
                <div class="form-group">
                    <label>API Key</label>
                    <input type="password" id="apiKey" value="${config.apiKey}" required>
                </div>
                <div class="form-group">
                    <label>Default Model</label>
                    <input type="text" id="defaultModel" value="${config.defaultModel}" required>
                </div>
                <div class="form-group">
                    <label>Use Proxy</label>
                    <input type="checkbox" id="useProxy" ${config.useProxy ? 'checked' : ''}>
                </div>
                <div class="form-actions">
                    <button class="btn btn-primary" onclick="this.save()">Save Configuration</button>
                    <button class="btn btn-secondary" onclick="this.test()">Test Connection</button>
                </div>
                <div id="validation-results"></div>
            </div>
        `;
    }
}
```

#### 3. Provider Health Dashboard
```javascript
class ProviderHealthDashboard {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.providers = [];
        this.healthData = new Map();
        this.updateInterval = null;
    }

    async initialize() {
        await this.loadProviders();
        this.renderDashboard();
        this.startHealthMonitoring();
    }

    renderDashboard() {
        this.container.innerHTML = `
            <div class="provider-dashboard">
                <h2>AI Provider Health Dashboard</h2>
                <div class="provider-grid">
                    ${this.providers.map(provider => this.renderProviderCard(provider)).join('')}
                </div>
                <div class="metrics-summary">
                    <h3>System Metrics</h3>
                    <div class="metrics-grid">
                        <div class="metric-card">
                            <h4>Total Requests</h4>
                            <span class="metric-value" id="total-requests">-</span>
                        </div>
                        <div class="metric-card">
                            <h4>Success Rate</h4>
                            <span class="metric-value" id="success-rate">-</span>
                        </div>
                        <div class="metric-card">
                            <h4>Average Response Time</h4>
                            <span class="metric-value" id="avg-response-time">-</span>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderProviderCard(provider) {
        const health = this.healthData.get(provider.name) || {};
        const statusClass = health.isHealthy ? 'status-healthy' : 'status-error';
        const statusText = health.isHealthy ? 'Healthy' : 'Unhealthy';

        return `
            <div class="provider-card ${statusClass}">
                <h3>${provider.name}</h3>
                <div class="provider-status">
                    <span class="status-indicator ${statusClass}"></span>
                    <span>${statusText}</span>
                </div>
                <div class="provider-stats">
                    <div>Response Time: ${health.responseTime || '-'}ms</div>
                    <div>Success Rate: ${health.successRate || '-'}%</div>
                    <div>Last Check: ${health.lastCheck || 'Never'}</div>
                </div>
                <div class="provider-actions">
                    <button class="btn btn-sm" onclick="this.selectProvider('${provider.name}')">
                        ${provider.isActive ? 'Current' : 'Select'}
                    </button>
                    <button class="btn btn-sm" onclick="this.editProvider('${provider.name}')">
                        Edit
                    </button>
                </div>
            </div>
        `;
    }
}
```

### 4. Real-time Updates with WebSocket/SSE
```javascript
class ProviderStatusStream {
    constructor() {
        this.eventSource = null;
        this.callbacks = new Map();
    }

    connect() {
        this.eventSource = new EventSource('/api/ai/providers/status/stream');
        
        this.eventSource.addEventListener('health-update', (event) => {
            const data = JSON.parse(event.data);
            this.notifyCallbacks('health-update', data);
        });

        this.eventSource.addEventListener('config-change', (event) => {
            const data = JSON.parse(event.data);
            this.notifyCallbacks('config-change', data);
        });
    }

    on(event, callback) {
        if (!this.callbacks.has(event)) {
            this.callbacks.set(event, []);
        }
        this.callbacks.get(event).push(callback);
    }

    notifyCallbacks(event, data) {
        if (this.callbacks.has(event)) {
            this.callbacks.get(event).forEach(callback => callback(data));
        }
    }
}
```

## Implementation Tasks

### Phase 1: Backend API Endpoints (Priority: High)
- [ ] Create `/api/ai/providers` GET endpoint for listing all providers
- [ ] Create `/api/ai/providers/{name}` GET endpoint for provider configuration
- [ ] Create `/api/ai/providers/{name}` POST endpoint for updating configuration
- [ ] Create `/api/ai/providers/{name}/test` POST endpoint for connectivity testing
- [ ] Create `/api/ai/providers/{name}/health` GET endpoint for health status
- [ ] Create `/api/ai/providers/status/stream` SSE endpoint for real-time updates

### Phase 2: Frontend Components (Priority: High)
- [ ] Create `ProviderConfigurationForm` component with validation
- [ ] Create `ProviderHealthDashboard` component with real-time updates
- [ ] Create `ProviderSelector` component for quick provider switching
- [ ] Create configuration validation utilities
- [ ] Add error handling and user feedback mechanisms

### Phase 3: User Experience (Priority: Medium)
- [ ] Add provider configuration page to admin navigation
- [ ] Create provider status indicators in main dashboard
- [ ] Add configuration backup and restore functionality
- [ ] Implement configuration change audit logging
- [ ] Add provider performance comparison charts

### Phase 4: Security & Validation (Priority: Medium)
- [ ] Add role-based access control for configuration changes
- [ ] Implement secure API key storage and masking
- [ ] Add configuration change approval workflow
- [ ] Create configuration validation rules
- [ ] Add rate limiting for configuration changes

### Phase 5: Testing & Documentation (Priority: Low)
- [ ] Create unit tests for all frontend components
- [ ] Add end-to-end tests for provider switching
- [ ] Create user documentation with screenshots
- [ ] Add tooltips and help text throughout the interface
- [ ] Create video tutorials for common tasks

## User Interface Design

### Provider Configuration Page
```
┌─────────────────────────────────────────────────────────┐
│  AI Provider Configuration                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Current Provider: [OpenRouter] [🔴 Unhealthy]          │
│                                                         │
│  ┌─────────────────┐  ┌─────────────────┐              │
│  │   OpenRouter    │  │     NanoGpt     │              │
│  │   [Active]      │  │   [Configure]   │              │
│  │   Response: 2s  │  │   Response: -   │              │
│  │   Success: 99%  │  │   Success: -    │              │
│  └─────────────────┘  └─────────────────┘              │
│                                                         │
│  Provider Settings:                                     │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Base URL: [https://openrouter.ai/api/v1____]   │   │
│  │ API Key:  [•••••••••••••••••••••••••••••••]  │   │
│  │ Model:    [qwen/qwen3-coder______________]     │   │
│  │                                         [Save] │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [Test Connection] [Apply Configuration] [Rollback]    │
└─────────────────────────────────────────────────────────┘
```

### Provider Health Dashboard
```
┌─────────────────────────────────────────────────────────┐
│  Provider Health Dashboard                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  System Metrics:                                        │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │
│  │ 1,234 Req   │ │ 99.2%       │ │ 2.3s Avg        │   │
│  │ This Hour   │ │ Success     │ │ Response Time   │   │
│  └─────────────┘ └─────────────┘ └─────────────────┘   │
│                                                         │
│  Provider Status:                                       │
│  ┌─────────────────┐  ┌─────────────────┐              │
│  │ OpenRouter      │  │ NanoGpt         │              │
│  │ ✅ Healthy      │  │ ⚠️ Testing      │              │
│  │ Last: 2s ago    │  │ Last: 5m ago    │              │
│  │ Uptime: 99.9%   │  │ Uptime: -       │              │
│  └─────────────────┘  └─────────────────┘              │
└─────────────────────────────────────────────────────────┘
```

## Definition of Done
- [ ] All API endpoints are implemented and tested
- [ ] Frontend components render correctly in all browsers
- [ ] Real-time updates work without page refresh
- [ ] Configuration validation prevents invalid settings
- [ ] User interface is responsive and accessible
- [ ] Error messages are helpful and actionable
- [ ] Documentation includes screenshots and examples
- [ ] Security measures protect sensitive configuration data

## Success Metrics
- **Configuration Time**: < 30 seconds to change providers via UI
- **Validation Speed**: < 3 seconds for configuration validation
- **Health Check Frequency**: Real-time updates every 30 seconds
- **User Error Rate**: < 1% configuration errors due to UI
- **Accessibility**: WCAG 2.1 AA compliant interface
- **Browser Support**: Chrome, Firefox, Safari, Edge (latest 2 versions)
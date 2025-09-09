
# Frontend Implementation Specification

This document provides the exact implementation details for rebuilding the AI Project Orchestrator frontend according to the requirements in `docs/02 prompts/US018B.md`.

## Phase 1: Infrastructure Implementation

### 1.1 Program.cs Modifications

**File**: `src/AIProjectOrchestrator.API/Program.cs`

**Required Changes**:
```csharp
// Add to service registration section (after line 27)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add to middleware pipeline (after app.UseRouting())
app.MapControllers();           // Keep existing API
app.MapRazorPages();           // Add Razor pages
app.MapDefaultControllerRoute(); // Add MVC routing
```

### 1.2 Directory Structure Creation

Create the following directory structure under `src/AIProjectOrchestrator.API/`:

```
wwwroot/
├── css/
│   └── styles.css
├── js/
│   ├── api.js
│   ├── app.js
│   └── workflow.js
├── Pages/
│   ├── _ViewStart.cshtml
│   ├── _Layout.cshtml
│   ├── Index.cshtml
│   └── Error.cshtml
└── Projects/
    ├── Index.cshtml
    ├── Create.cshtml
    ├── List.cshtml
    └── Workflow.cshtml
```

### 1.3 _ViewStart.cshtml Implementation

**File**: `src/AIProjectOrchestrator.API/Pages/_ViewStart.cshtml`

```html
@{
    Layout = "_Layout";
}
```

### 1.4 _Layout.cshtml Implementation

**File**: `src/AIProjectOrchestrator.API/Pages/_Layout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewData["Title"] - AI Project Orchestrator</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="~/css/styles.css">
    <link rel="icon" type="image/x-icon" href="~/favicon.ico">
</head>
<body>
    <header class="header">
        <div class="header-content">
            <div class="logo">
                <span class="logo-icon">🚀</span>
                <span class="logo-text">AI Project Orchestrator</span>
            </div>
            <nav class="header-nav">
                <a href="/" class="nav-item @(ViewContext.RouteData.Values["page"]?.ToString() == "/" ? "active" : "")">
                    <span class="nav-icon">🏠</span>Dashboard
                </a>
                <a href="/Projects" class="nav-item @(ViewContext.RouteData.Values["page"]?.ToString().StartsWith("Projects") ? "active" : "")">
                    <span class="nav-icon">📁</span>Projects
                </a>
                <a href="/Reviews/Queue" class="nav-item @(ViewContext.RouteData.Values["page"]?.ToString().StartsWith("Reviews") ? "active" : "")">
                    <span class="nav-icon">👀</span>Reviews
                </a>
                <a href="/faq" class="nav-item">
                    <span class="nav-icon">❓</span>FAQ
                </a>
            </nav>
            <div class="user-menu">
                <span class="status-indicator online"></span>
                <span class="user-name">Developer</span>
            </div>
        </div>
    </header>
    
    <main class="main-content">
        @RenderBody()
    </main>
    
    <footer class="footer">
        <div class="footer-content">
            <p>&copy; 2024 AI Project Orchestrator. Built with .NET 9 and ❤️.</p>
            <div class="footer-links">
                <a href="/api/health">Health Check</a>
                <a href="/swagger">API Docs</a>
                <a href="https://github.com">GitHub</a>
            </div>
        </div>
    </footer>
    
    <!-- Global JavaScript -->
    <script src="~/js/api.js"></script>
    <script src="~/js/app.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### 1.5 styles.css Implementation

**File**: `src/AIProjectOrchestrator.API/wwwroot/css/styles.css`

```css
/* CSS Custom Properties - Design System */
:root {
  /* Primary Brand Colors */
  --color-primary-50: #eff6ff;
  --color-primary-100: #dbeafe;
  --color-primary-500: #3b82f6;
  --color-primary-600: #2563eb;
  --color-primary-700: #1d4ed8;
  --color-primary-900: #1e3a8a;
  
  /* Semantic Colors */
  --color-success-50: #ecfdf5;
  --color-success-500: #10b981;
  --color-success-600: #059669;
  --color-warning-500: #f59e0b;
  --color-danger-500: #ef4444;
  
  /* Neutral Grays */
  --color-gray-25: #fcfcfd;
  --color-gray-50: #f9fafb;
  --color-gray-100: #f3f4f6;
  --color-gray-200: #e5e7eb;
  --color-gray-300: #d1d5db;
  --color-gray-400: #9ca3af;
  --color-gray-500: #6b7280;
  --color-gray-600: #4b5563;
  --color-gray-700: #374151;
  --color-gray-800: #1f2937;
  --color-gray-900: #111827;
  
  /* Gradients */
  --gradient-primary: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  --gradient-success: linear-gradient(135deg, #a8edea 0%, #fed6e3 100%);
  --gradient-card: linear-gradient(145deg, #ffffff 0%, #f8fafc 100%);
  
  /* Font Families */
  --font-sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --font-mono: 'JetBrains Mono', 'Fira Code', Consolas, 'Courier New', monospace;
  
  /* Font Sizes */
  --text-xs: 0.75rem;
  --text-sm: 0.875rem;
  --text-base: 1rem;
  --text-lg: 1.125rem;
  --text-xl: 1.25rem;
  --text-2xl: 1.5rem;
  --text-3xl: 1.875rem;
  --text-4xl: 2.25rem;
  
  /* Font Weights */
  --font-light: 300;
  --font-normal: 400;
  --font-medium: 500;
  --font-semibold: 600;
  --font-bold: 700;
  
  /* Spacing */
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --spacing-xl: 2rem;
  --spacing-2xl: 3rem;
}

/* Reset and Base Styles */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html {
  font-size: 16px;
  scroll-behavior: smooth;
}

body {
  font-family: var(--font-sans);
  font-size: var(--text-base);
  font-weight: var(--font-normal);
  line-height: 1.6;
  color: var(--color-gray-900);
  background-color: var(--color-gray-50);
}

/* Typography */
h1, h2, h3, h4, h5, h6 {
  font-weight: var(--font-semibold);
  line-height: 1.2;
  margin-bottom: var(--spacing-md);
}

h1 { font-size: var(--text-4xl); }
h2 { font-size: var(--text-3xl); }
h3 { font-size: var(--text-2xl); }
h4 { font-size: var(--text-xl); }
h5 { font-size: var(--text-lg); }
h6 { font-size: var(--text-base); }

p {
  margin-bottom: var(--spacing-md);
}

a {
  color: var(--color-primary-600);
  text-decoration: none;
  transition: color 0.2s ease;
}

a:hover {
  color: var(--color-primary-700);
}

/* Layout */
.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 var(--spacing-md);
}

.main-content {
  min-height: calc(100vh - 200px);
  padding: var(--spacing-xl) 0;
}

/* Header Styles */
.header {
  background: white;
  border-bottom: 1px solid var(--color-gray-200);
  position: sticky;
  top: 0;
  z-index: 1000;
  backdrop-filter: blur(10px);
  background: rgba(255, 255, 255, 0.95);
}

.header-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-md) var(--spacing-lg);
  max-width: 1200px;
  margin: 0 auto;
}

.logo {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-weight: var(--font-bold);
  font-size: var(--text-lg);
}

.logo-icon {
  font-size: var(--text-xl);
}

.logo-text {
  color: var(--color-gray-900);
}

.header-nav {
  display: flex;
  gap: var(--spacing-lg);
}

.nav-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: 8px;
  color: var(--color-gray-600);
  font-weight: var(--font-medium);
  transition: all 0.2s ease;
}

.nav-item:hover {
  background: var(--color-gray-100);
  color: var(--color-gray-900);
}

.nav-item.active {
  background: var(--color-primary-50);
  color: var(--color-primary-700);
}

.nav-icon {
  font-size: var(--text-sm);
}

.user-menu {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--color-success-500);
}

.status-indicator.online {
  box-shadow: 0 0 0 2px white, 0 0 0 4px var(--color-success-500);
}

.user-name {
  font-size: var(--text-sm);
  font-weight: var(--font-medium);
  color: var(--color-gray-700);
}

/* Card Components */
.card {
  background: var(--gradient-card);
  border: 1px solid var(--color-gray-200);
  border-radius: 12px;
  box-shadow: 
    0 1px 3px 0 rgb(0 0 0 / 0.1),
    0 1px 2px -1px rgb(0 0 0 / 0.1);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  padding: var(--spacing-xl);
}

.card:hover {
  border-color: var(--color-primary-200);
  box-shadow: 
    0 20px 25px -5px rgb(0 0 0 / 0.1),
    0 8px 10px -6px rgb(0 0 0 / 0.1);
  transform: translateY(-2px);
}

/* Button Components */
.btn {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-lg);
  border: none;
  border-radius: 8px;
  font-weight: var(--font-medium);
  font-size: var(--text-sm);
  cursor: pointer;
  transition: all 0.2s ease;
  text-decoration: none;
  outline: none;
}

.btn-primary {
  background: var(--gradient-primary);
  color: white;
  position: relative;
  overflow: hidden;
}

.btn-primary::before {
  content: '';
  position: absolute;
  top: 0; left: 0; right: 0; bottom: 0;
  background: linear-gradient(135deg, rgba(255,255,255,0.2) 0%, transparent 50%);
  opacity: 0;
  transition: opacity 0.2s ease;
}

.btn-primary:hover::before {
  opacity: 1;
}

.btn-primary:hover {
  transform: translateY(-1px);
  box-shadow: 0 10px 20px -5px rgb(0 0 0 / 0.2);
}

.btn-secondary {
  background: white;
  color: var(--color-gray-700);
  border: 1px solid var(--color-gray-300);
}

.btn-secondary:hover {
  background: var(--color-gray-50);
  border-color: var(--color-gray-400);
}

.btn-success {
  background: var(--color-success-500);
  color: white;
}

.btn-success:hover {
  background: var(--color-success-600);
}

.btn-danger {
  background: var(--color-danger-500);
  color: white;
}

.btn-danger:hover {
  background: var(--color-danger-600);
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: var(--spacing-xl);
  margin-bottom: var(--spacing-2xl);
}

.stat-card {
  background: white;
  border-radius: 16px;
  padding: var(--spacing-xl);
  border: 1px solid var(--color-gray-200);
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
  text-align: center;
  transition: all 0.3s ease;
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.1);
}

.stat-icon {
  font-size: var(--text-3xl);
  margin-bottom: var(--spacing-md);
}

.stat-value {
  font-size: var(--text-3xl);
  font-weight: var(--font-bold);
  background: var(--gradient-primary);
  background-clip: text;
  -webkit-background-clip: text;
  color: transparent;
  margin-bottom: var(--spacing-sm);
}

.stat-label {
  color: var(--color-gray-600);
  font-size: var(--text-sm);
  font-weight: var(--font-medium);
}

/* Workflow Pipeline */
.workflow-pipeline {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-2xl);
  background: white;
  border-radius: 16px;
  box-shadow: 0 10px 15px -3px rgb(0 0 0 / 0.1);
  margin-bottom: var(--spacing-2xl);
}

.pipeline-stage {
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
  flex: 1;
}

.pipeline-stage:not(:last-child)::after {
  content: '';
  position: absolute;
  top: 24px;
  right: -50%;
  width: 100%;
  height: 2px;
  background: var(--color-gray-300);
  z-index: 0;
}

.stage-indicator {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: var(--font-bold);
  margin-bottom: var(--spacing-sm);
  transition: all 0.3s ease;
  z-index: 1;
  background: var(--color-gray-200);
  color: var(--color-gray-600);
}

.stage-indicator.completed {
  background: var(--color-success-500);
  color: white;
  animation: pulse-success 2s infinite;
}

.stage-indicator.active {
  background: var(--color-primary-500);
  color: white;
  box-shadow: 0 0 0 4px var(--color-primary-100);
}

.stage-title {
  font-size: var(--text-sm);
  font-weight: var(--font-semibold);
  color: var(--color-gray-700);
  margin-bottom: var(--spacing-xs);
}

.stage-description {
  font-size: var(--text-xs);
  color: var(--color-gray-500);
  text-align: center;
}

@keyframes pulse-success {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.05); }
}

/* Forms */
.form-group {
  margin-bottom: var(--spacing-lg);
}

.form-label {
  display: block;
  margin-bottom: var(--spacing-sm);
  font-weight: var(--font-medium);
  color: var(--color-gray-700);
}

.form-input,
.form-textarea,
.form-select {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--color-gray-300);
  border-radius: 8px;
  font-size: var(--text-base);
  transition: border-color 0.2s ease;
}

.form-input:focus,
.form-textarea:focus,
.form-select:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-textarea {
  resize: vertical;
  min-height: 120px;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-lg);
}

/* Footer */
.footer {
  background: var(--color-gray-900);
  color: white;
  padding: var(--spacing-xl) 0;
  margin-top: var(--spacing-2xl);
}

.footer-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 var(--spacing-md);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.footer-links {
  display: flex;
  gap: var(--spacing-lg);
}

.footer-links a {
  color: var(--color-gray-300);
  text-decoration: none;
  transition: color 0.2s ease;
}

.footer-links a:hover {
  color: white;
}

/* Responsive Design */
@media (max-width: 768px) {
  .header-nav {
    display: none;
  }
  
  .stats-grid {
    grid-template-columns: 1fr;
    gap: var(--spacing-lg);
  }
  
  .workflow-pipeline {
    flex-direction: column;
    gap: var(--spacing-xl);
  }
  
  .pipeline-stage:not(:last-child)::after {
    display: none;
  }
  
  .form-row {
    grid-template-columns: 1fr;
  }
  
  .footer-content {
    flex-direction: column;
    gap: var(--spacing-md);
    text-align: center;
  }
}

/* Loading States */
.loading {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-gray-300);
  border-radius: 50%;
  border-top-color: var(--color-primary-500);
  animation: spin 1s ease-in-out infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Utility Classes */
.text-center { text-align: center; }
.text-left { text-align: left; }
.text-right { text-align: right; }

.mb-0 { margin-bottom: 0; }
.mb-sm { margin-bottom: var(--spacing-sm); }
.mb-md { margin-bottom: var(--spacing-md); }
.mb-lg { margin-bottom: var(--spacing-lg); }
.mb-xl { margin-bottom: var(--spacing-xl); }

.mt-0 { margin-top: 0; }
.mt-sm { margin-top: var(--spacing-sm); }
.mt-md { margin-top: var(--spacing-md); }
.mt-lg { margin-top: var(--spacing-lg); }
.mt-xl { margin-top: var(--spacing-xl); }

.d-flex { display: flex; }
.d-block { display: block; }
.d-inline { display: inline; }
.d-inline-block { display: inline-block; }

.justify-content-center { justify-content: center; }
.justify-content-between { justify-content: space-between; }
.align-items-center { align-items: center; }

.flex-column { flex-direction: column; }
.flex-wrap { flex-wrap: wrap; }
.flex-grow-1 { flex-grow: 1; }

.w-100 { width: 100%; }
.h-100 { height: 100%; }

.position-relative { position: relative; }
.position-absolute { position: absolute; }

.rounded { border-radius: 8px; }
.rounded-lg { border-radius: 12px; }
.rounded-xl { border-radius: 16px; }

.shadow { box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1); }
.shadow-lg { box-shadow: 0 10px 15px -3px rgb(0 0 0 / 0.1); }

.overflow-hidden { overflow: hidden; }
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
```

### 1.6 api.js Implementation

**File**: `src/AIProjectOrchestrator.API/wwwroot/js/api.js`

```javascript
window.APIClient = {
    baseUrl: '/api',  // Ensure this matches your API routing
    
    async makeRequest(method, endpoint, data = null) {
        const url = `${this.baseUrl}${endpoint}`;
        const config = {
            method,
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        };
        
        if (data) config.body = JSON.stringify(data);
        
        try {
            const response = await fetch(url, config);
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(`HTTP ${response.status}: ${errorData.message || response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    },

    // Project Management
    async getProjects() {
        return this.makeRequest('GET', '/projects');
    },

    async getProject(id) {
        return this.makeRequest('GET', `/projects/${id}`);
    },

    async createProject(projectData) {
        return this.makeRequest('POST', '/projects', projectData);
    },

    async updateProject(id, projectData) {
        return this.makeRequest('PUT', `/projects/${id}`, projectData);
    },

    async deleteProject(id) {
        return this.makeRequest('DELETE', `/projects/${id}`);
    },

    // Requirements Analysis
    async analyzeRequirements(projectId, requirements) {
        return this.makeRequest('POST', `/projects/${projectId}/requirements`, { requirements });
    },

    async getRequirements(projectId) {
        return this.makeRequest('GET', `/projects/${projectId}/requirements`);
    },

    // Project Planning
    async createPlanning(projectId, planningData) {
        return this.makeRequest('POST', `/projects/${projectId}/planning`, planningData);
    },

    async getPlanning(projectId) {
        return this.makeRequest('GET', `/projects/${projectId}/planning`);
    },

    // Story Generation
    async generateStories(projectId, storyData) {
        return this.makeRequest('POST', `/projects/${projectId}/stories`, storyData);
    },

    async getStories(projectId) {
        return this.makeRequest('GET', `/projects/${projectId}/stories`);
    },

    async updateStory(projectId, storyId, storyData) {
        return this.makeRequest('PUT', `/projects/${projectId}/stories/${storyId}`, storyData);
    },

    async approveStory(projectId, storyId) {
        return this.makeRequest('POST', `/projects/${projectId}/stories/${storyId}/approve`);
    },

    async rejectStory(projectId, storyId, feedback) {
        return this.makeRequest('POST', `/projects/${projectId}/stories/${storyId}/reject`, { feedback });
    },

    // Prompt Generation
    async generatePrompt(storyId, promptData) {
        return this.makeRequest('POST', `/stories/${storyId}/prompts`, promptData);
    },

    async getPrompts(storyId) {
        return this.makeRequest('GET', `/stories/${storyId}/prompts`);
    },

    // Review Management
    async getReviews() {
        return this.makeRequest('GET', '/reviews');
    },

    async getReview(id) {
        return this.makeRequest('GET', `/reviews/${id}`);
    },

    async approveReview(id) {
        return this.makeRequest('POST', `/reviews/${id}/approve`);
    },

    async rejectReview(id, feedback) {
        return this.makeRequest('POST', `/reviews/${id}/reject`, { feedback });
    },

    // Utility Methods
    async getHealth() {
        return this.makeRequest('GET', '/health');
    },

    async getStatus(projectId) {
        return this.makeRequest('GET', `/projects/${projectId}/status`);
    }
};

// Error handling utility
function handleApiError(error, defaultMessage = 'An error occurred') {
    console.error('API Error:', error);
    const message = error.message || defaultMessage;
    
    // Show user-friendly error message
    const errorElement = document.getElementById('error-message');
    if (errorElement) {
        errorElement.textContent = message;
        errorElement.style.display = 'block';
    } else {
        alert(message);
    }
    
    throw error;
}

// Success notification utility
function showSuccess(message) {
    const notification = document.createElement('div');
    notification.className = 'notification success';
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: var(--color-success-500);
        color: white;
        padding: 12px 24px;
        border-radius: 8px;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
        z-index: 10000;
        animation: slideIn 0.3s ease;
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

// Loading state utility
function setLoading(element, isLoading) {
    if (isLoading) {
        element.classList.add('loading');
        element.disabled = true;
    } else {
        element.classList.remove('loading');
        element.disabled = false;
    }
}

// Add CSS animations for notifications
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    
    @keyframes slideOut {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(100%); opacity: 0; }
    }
`;
document.head.appendChild(style);
```

## Phase 2: Core Pages Implementation

### 2.1 Index.cshtml (Home Page)

**File**: `src/AIProjectOrchestrator.API/Pages/Index.cshtml`

```html
@{
    ViewData["Title"] = "Dashboard";
}

<div class="container">
    <!-- Welcome Section -->
    <div class="welcome-section">
        <h1>Welcome to AI Project Orchestrator</h1>
        <p class="subtitle">Transform your ideas into reality with AI-powered project development</p>
    </div>

    <!-- Stats Grid -->
    <div class="stats-grid">
        <div class="stat-card">
            <div class="stat-icon">🚀</div>
            <div class="stat-value" id="active-projects">0</div>
            <div class="stat-label">Active Projects</div>
        </div>
        <div class="stat-card">
            <div class="stat-icon">📋</div>
            <div class="stat-value" id="pending-reviews">0</div>
            <div class="stat-label">Pending Reviews</div>
        </div>
        <div class="stat-card">
            <div class="stat-icon">✅</div>
            <div class="stat-value" id="completed-stories">0</div>
            <div class="stat-label">Stories Completed</div>
        </div>
        <div class="stat-card">
            <div class="stat-icon">⚡</div>
            <div class="stat-value" id="ai-tasks">0</div>
            <div class="stat-label">AI Tasks Today</div>
        </div>
    </div>

    <!-- Quick Actions -->
    <div class="quick-actions">
        <h2>Quick Actions</h2>
        <div class="action-buttons">
            <a href="/Projects/Create" class="btn btn-primary">
                <span>➕</span> Create New Project
            </a>
            <a href="/Projects" class="btn btn-secondary">
                <span>📁</span> View All Projects
            </a>
            <a href="/Reviews/Queue" class="btn btn-secondary">
                <span>👀</span> Review Queue
            </a>
        </div>
    </div>

    <!-- Recent Projects -->
    <div class="recent-projects">
        <h2>Recent Projects</h2>
        <div class="projects-grid" id="recent-projects">
            <!-- Projects will be loaded here -->
        </div>
    </div>

    <!-- System Status -->
    <div class="system-status">
        <h2>System Status</h2>
        <div class="status-grid" id="system-status">
            <!-- System status will be loaded here -->
        </div>
    </div>
</div>

@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            loadDashboardData();
            loadSystemStatus();
            
            // Refresh data every 30 seconds
            setInterval(() => {
                loadDashboardData();
                loadSystemStatus();
            }, 30000);
        });

        async function loadDashboardData() {
            try {
                const [projects, reviews] = await Promise.all([
                    APIClient.getProjects(),
                    APIClient.getReviews()
                ]);

                // Update stats
                document.getElementById('active-projects').textContent = projects.length;
                document.getElementById('pending-reviews').textContent = 
                    reviews.filter(r => r.status === 'pending').length;
                
                // Calculate completed stories (mock data for now)
                document.getElementById('completed-stories').textContent = 
                    Math.floor(Math.random() * 50) + 10;
                
                // Calculate AI tasks (mock data for now)
                document.getElementById('ai-tasks').textContent = 
                    Math.floor(Math.random() * 20) + 5;

                // Load recent projects
                loadRecentProjects(projects.slice(0, 4));
            } catch (error) {
                handleApiError(error, 'Failed to load dashboard data');
            }
        }

        async function loadRecentProjects(projects) {
            const container = document.getElementById('recent-projects');
            
            if (projects.length === 0) {
                container.innerHTML = '<p class="text-center">No projects yet. Create your first project!</p>';
                return;
            }

            container.innerHTML = projects.map(project => `
                <div class="project-card">
                    <div class="project-header">
                        <h3>${project.name}</h3>
                        <span class="project-status ${project.status}">${project.status}</span>
                    </div>
                    <p class="project-description">${project.description || 'No description'}</p>
                    <div class="project-meta">
                        <span class="project-date">Created: ${new Date(project.createdAt).toLocaleDateString()}</span>
                    </div>
                    <div class="project-actions">
                        <a href="/Projects/Workflow?projectId=${project.id}" class="btn btn-primary">
                            View Workflow
                        </a>
                    </div>
                </div>
            `).join('');
        }

        async function loadSystemStatus() {
            try {
                const health = await APIClient.getHealth();
                const container = document.getElementById('system-status');
                
                container.innerHTML = Object.entries(health).map(([service, status]) => `
                    <div class="status-item">
                        <span class="service-name">${service}</span>
                        <span class="status-indicator ${status ? 'healthy' : 'unhealthy'}">
                            ${status ? '✅' : '❌'}
                        </span>
                    </div>
                `).join('');
            } catch (error) {
                console.error('Failed to load system status:', error);
                document.getElementById('system-status').innerHTML = 
                    '<p class="text-center">Unable to load system status</p>';
            }
        }
    </script>
}
```

### 2.2 Projects/Create.cshtml

**File**: `src/AIProjectOrchestrator.API/Projects/Create.cshtml`

```html
@{
    ViewData["Title"] = "Create Project";
}

<div class="container">
    <div class="page-header">
        <h1>Create New Project</h1>
        <p class="subtitle">Define your project requirements and let AI orchestrate the development process</p>
    </div>

    <div class="form-container">
        <form id="create-project-form">
            <div class="form-row">
                <div class="form-group">
                    <label for="project-name" class="form-label">Project Name *</label>
                    <input type="text" id="project-name" name="name" class="form-input" required 
                           placeholder="Enter project name">
                </div>
                <div class="form-group">
                    <label for="project-type" class="form-label">Project Type</label>
                    <select id="project-type" name="type" class="form-select">
                        <option value="web">Web Application</option>
                        <option value="mobile">Mobile App</option>
                        <option value="api">API Service</option>
                        <option value="desktop">Desktop Application</option>
                        <option value="other">Other</option>
                    </select>
                </div>
            </div>

            <div class="form-group">
                <label for="project-description" class="form-label">Project Description *</label>
                <textarea id="project-description" name="description" class="form-textarea" required 
                          placeholder="Describe your project goals, features, and requirements..."></textarea>
            </div>

            <div class="form-row">
                <div class="form-group">
                    <label for="tech-stack" class="form-label">Tech Stack</label>
                    <input type="text" id="tech-stack" name="techStack" 
                           placeholder="e.g., React, Node.js, PostgreSQL">
                </div>
                <div class="form-group">
                    <label for="timeline" class="form-label">Timeline</label>
                    <select id="timeline" name="timeline" class="form-select">
                        <option value="1-2 weeks">1-2 weeks</option>
                        <option value="3-4 weeks">3-4 weeks</option>
                        <option value="1-2 months">1-2 months</option>
                        <option value="3-6 months">3-6 months</option>
                    </select>
                </div>
            </div>

            <div class="form-group">
                <label for="requirements" class="form-label">Detailed Requirements</label>
                <textarea id="requirements" name="requirements" class="form-textarea" 
                          placeholder="Provide detailed requirements, user stories, acceptance criteria..."></textarea>
            </div>

            <div class="form-actions">
                <button type="submit" class="btn btn-primary" id="submit-btn">
                    <span>🚀</span> Create Project & Start AI Analysis
                </button>
                <a href="/" class="btn btn-secondary">Cancel</a>
            </div>
        </form>

        <!-- Real-time Preview -->
        <div class="preview-section" id="preview-section" style="display: none;">
            <h3>Project Preview</h3>
            <div class="preview-content" id="preview-content">
                <!-- Preview will be generated here -->
            </div>
        </div>
    </div>

    <!-- Loading Overlay -->
    <div class="loading-overlay" id="loading-overlay" style="display: none;">
        <div class="loading-content">
            <div class="loading-spinner"></div>
            <h3>Creating Project...</h3>
            <p>Initiating AI analysis of your requirements</p>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            const form = document.getElementById('create-project-form');
            const previewSection = document.getElementById('preview-section');
            const previewContent = document.getElementById('preview-content');
            const loadingOverlay = document.getElementById('loading-overlay');

            // Real-time preview
            const formInputs = form.querySelectorAll('input, textarea, select');
            formInputs.forEach(input => {
                input.addEventListener('input', updatePreview);
                input.addEventListener('change', updatePreview);
            });

            function updatePreview() {
                const formData = new FormData(form);
                const data = Object.fromEntries(formData);
                
                if (data.name || data.description) {
                    previewSection.style.display = 'block';
                    previewContent.innerHTML = `
                        <div class="preview-card">
                            <h4>${data.name || 'Untitled Project'}</h4>
                            <p class="preview-description">${data.description || 'No description provided'}</p>
                            <div class="preview-meta">
                                ${data.type ? `<span>Type: ${data.type}</span>` : ''}
                                ${data.techStack ? `<span>Tech Stack: ${data.techStack}</span>` : ''}
                                ${data.timeline ? `<span>Timeline: ${data.timeline}</span>` : ''}
                            </div>
                        </div>
                    `;
                } else {
                    previewSection.style.display = 'none';
                }
            }

            // Form submission
            form.addEventListener('submit', async function(e) {
                e.preventDefault();
                
                const submitBtn = document.getElementById('submit-btn');
                const originalText = submitBtn.innerHTML;
                
                try {
                    setLoading(submitBtn, true);
                    loadingOverlay.style.display = 'flex';

                    const formData = new FormData(form);
                    const projectData = Object.fromEntries(formData);
                    
                    // Create project
                    const project = await APIClient.createProject(projectData);
                    
                    // Show success and redirect
                    showSuccess('Project created successfully! Starting AI analysis...');
                    
                    setTimeout(() => {
                        window.location.href = `/Projects/Workflow?projectId=${project.id}&newProject=true`;
                    }, 1500);
                    
                } catch (error) {
                    handleApiError(error, 'Failed to create project');
                } finally {
                    setLoading(submitBtn, false);
                    loadingOverlay.style.display = 'none';
                }
            });

            // Add loading spinner CSS
            const style = document.createElement('style');
            style.textContent = `
                .loading-overlay {
                    position: fixed;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background: rgba(0, 0, 0, 0.5);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 10000;
                }
                
                .loading-content {
                    background: white;
                    padding: 2rem;
                    border-radius: 12px;
                    text-align: center;
                    max-width: 400px;
                }
                
                .loading-spinner {
                    width: 40px;
                    height: 40px;
                    border: 4px solid var(--color-gray-200);
                    border-top: 4px solid var(--color-primary-500);
                    border-radius: 50%;
                    animation: spin 1s linear infinite;
                    margin: 0 auto 1rem;
                }
                
                .preview-card {
                    background: var(--color-gray-50);
                    padding: 1rem;
                    border-radius: 8px;
                    border: 1px solid var(--color-gray-200);
                }
                
                .preview-meta {
                    margin-top: 1rem;
                    display: flex;
                    gap: 1rem;
                    font-size: 0.875rem;
                    color: var(--color-gray-600);
                }
                
                @keyframes spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }
            `;
            document.head.appendChild(style);
        });
    </script>
}
```

### 2.3 Projects/List.cshtml

**File**: `src/AIProjectOrchestrator.API/Projects/List.cshtml`

```html
@{
    ViewData["Title"] = "Projects";
}

<div class="container">
    <div class="page-header">
        <div class="header-content">
            <div>
                <h1>Projects</h1>
                <p class="subtitle">Manage your AI-powered development projects</p>
            </div>
            <a href="/Projects/Create" class="btn btn-primary">
                <span>➕</span> New Project
            </a>
        </div>
    </div>

    <!-- Filters and Search -->
    <div class="filters-section">
        <div class="search-filters">
            <div class="search-box">
                <input type="text" id="search-input" placeholder="Search projects..." class="form-input">
                <span class="search-icon">🔍</span>
            </div>
            <div class="filter-controls">
                <select id="status-filter" class="form-select">
                    <option value="">All Status</option>
                    <option value="planning">Planning</option>
                    <option value="active">Active</option>
                    <option value="completed">Completed</option>
                    <option value="paused">Paused</option>
                </select>
                <select id="type-filter" class="form-select">
                    <option value="">All Types</option>
                    <option value="web">Web Application</option>
                    <option value="mobile">Mobile App</option>
                    <option value="api">API Service</option>
                    <option value="desktop">Desktop Application</option>
                </select>
            </div>
        </div>
    </div>

    <!-- Projects Grid -->
    <div class="projects-grid" id="projects-container">
        <!-- Projects will be loaded here -->
    </div>

    <!-- Empty State -->
    <div class="empty-state" id="empty-state" style="display: none;">
        <div class="empty-content">
            <div class="empty-icon">📁</div>
            <h3>No Projects Found</h3>
            <p>Create your first AI-powered project to get started</p>
            <a href="/Projects/Create" class="btn btn-primary">Create Project</a>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            loadProjects();
            
            // Setup event listeners
            document.getElementById('search-input').addEventListener('input', filterProjects);
            document.getElementById('status-filter').addEventListener('change', filterProjects);
            document.getElementById('type-filter').addEventListener('change', filterProjects);
        });

        let allProjects = [];

        async function loadProjects() {
            try {
                const projects = await APIClient.getProjects();
                allProjects = projects;
                renderProjects(projects);
            } catch (error) {
                handleApiError(error, 'Failed to load projects');
            }
        }

        function renderProjects(projects) {
            const container = document.getElementById('projects-container');
            const emptyState = document.getElementById('empty-state');

            if (projects.length === 0) {
                container.style.display = 'none';
                emptyState.style.display = 'block';
                return;
            }

            container.style.display = 'grid';
            emptyState.style.display = 'none';

            container.innerHTML = projects.map(project => `
                <div class="project-card" data-project='${JSON.stringify(project)}'>
                    <div class="project-header">
                        <h3>${project.name}</h3>
                        <span class="project-status ${project.status}">${project.status}</span>
                    </div>
                    <p class="project-description">${project.description || 'No description provided'}</p>
                    <div class="project-meta">
                        <span class="project-type">${project.type || 'web'}</span>
                        <span class="project-date">Created: ${new Date(project.createdAt).toLocaleDateString()}</span>
                    </div>
                    <div class="project-actions">
                        <a href="/Projects/Workflow?projectId=${project.id}" class="btn btn-primary">
                            View Workflow
                        </a>
                        <button class="btn btn-secondary" onclick="editProject('${project.id}')">
                            Edit
                        </button>
                    </div>
                </div>
            `).join('');
        }

        function filterProjects() {
            const searchTerm = document.getElementById('search-input').value.toLowerCase();
            const statusFilter = document.getElementById('status-filter').value;
            const typeFilter = document.getElementById('type-filter').value;

            const filtered = allProjects.filter(project => {
                const matchesSearch = !searchTerm || 
                    project.name.toLowerCase().includes(searchTerm) ||
                    (project.description && project.description.toLowerCase().includes(searchTerm));
                
                const matchesStatus = !statusFilter || project.status === statusFilter;
                const matchesType = !typeFilter || project.type === typeFilter;

                return matchesSearch && matchesStatus && matchesType;
            });

            renderProjects(filtered);
        }

        function editProject(projectId) {
            // Placeholder for edit functionality
            console.log('Edit project:', projectId);
        }

        // Add CSS for filters and search
        const style = document.createElement('style');
        style.textContent = `
            .page-header {
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
                margin-bottom: 2rem;
            }
            
            .header-content h1 {
                margin: 0;
                font-size: 2rem;
            }
            
            .header-content .subtitle {
                margin: 0.5rem 0 0 0;
                color: var(--color-gray-600);
            }
            
            .filters-section {
                background: white;
                padding: 1.5rem;
                border-radius: 12px;
                box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
                margin-bottom: 2rem;
            }
            
            .search-filters {
                display: flex;
                gap: 1rem;
                align-items: center;
                flex-wrap: wrap;
            }
            
            .search-box {
                position: relative;
                flex: 1;
                min-width: 250px;
            }
            
            .search-box .form-input {
                padding-left: 2.5rem;
            }
            
            .search-icon {
                position: absolute;
                left: 0.75rem;
                top: 50%;
                transform: translateY(-50%);
                color: var(--color-gray-400);
            }
            
            .filter-controls {
                display: flex;
                gap: 1rem;
            }
            
            .projects-grid {
                display: grid;
                grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
                gap: 1.5rem;
            }
            
            .project-card {
                background: white;
                border-radius: 12px;
                padding: 1.5rem;
                box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
                border: 1px solid var(--color-gray-200);
                transition: all 0.3s ease;
            }
            
            .project-card:hover {
                transform: translateY(-2px);
                box-shadow: 0 10px 25px -5px rgb(0 0 0 / 0.1);
            }
            
            .project-header {
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
                margin-bottom: 1rem;
            }
            
            .project-header h3 {
                margin: 0;
                font-size: 1.25rem;
                color: var(--color-gray-900);
            }
            
            .project-status {
                padding: 0.25rem 0.75rem;
                border-radius: 20px;
                font-size: 0.75rem;
                font-weight: 500;
                text-transform: uppercase;
            }
            
            .project-status.planning {
                background: var(--color-warning-100);
                color: var(--color-warning-700);
            }
            
            .project-status.active {
                background: var(--color-success-100);
                color: var(--color-success-700);
            }
            
            .project-status.completed {
                background: var(--color-gray-100);
                color: var(--color-gray-600);
            }
            
            .project-status.paused {
                background: var(--color-gray-100);
                color: var(--color-gray-600);
            }
            
            .project-description {
                color: var(--color-gray-600);
                margin-bottom: 1rem;
                line-height: 1.5;
            }
            
            .project-meta {
                display: flex;
                gap: 1rem;
                margin-bottom: 1.5rem;
                font-size: 0.875rem;
                color: var(--color-gray-500);
            }
            
            .project-actions {
                display: flex;
                gap: 0.5rem;
            }
            
            .project-actions .btn {
                flex: 1;
                justify-content: center;
            }
            
            .empty-state {
                text-align: center;
                padding: 4rem 2rem;
            }
            
            .empty-content {
                max-width: 400px;
                margin: 0 auto;
            }
            
            .empty-icon {
                font-size: 4rem;
                margin-bottom: 1rem;
            }
            
            .empty-state h3 {
                margin-bottom: 0.5rem;
                color: var(--color-gray-900);
            }
            
            .empty-state p {
                color: var(--color-gray-600);
                margin-bottom: 1.5rem;
            }
            
            @media (max-width: 768px) {
                .page-header {
                    flex-direction: column;
                    gap: 1rem;
                }
                
                .search-filters {
                    flex-direction: column;
                }
                
                .search-box {
                    width: 100%;
                }
                
                .filter-controls {
                    width: 100%;
                }
                
                .filter-controls .form-select {
                    flex: 1;
                }
                
                .projects-grid {
                    grid-template-columns: 1fr;
                }
            }
        `;
        document.head.appendChild(style);
    </script>
}
```

### 2.4 Projects/Workflow.cshtml

**File**: `src/AIProjectOrchestrator.API/Projects/Workflow.cshtml`

```html
@{
    ViewData["Title"] = "Project Workflow";
    var projectId = Context.Request.Query["projectId"];
    var newProject = Context.Request.Query["newProject"].ToString() == "true";
}

<div class="container">
    <div class="page-header">
        <div>
            <h1>Project Workflow</h1>
            <p class="subtitle">AI-powered development pipeline for @Context.Request.Query["projectName"]</p>
        </div>
        <div class="workflow-actions">
            <a href="/Projects" class="btn btn-secondary">← Back to Projects</a>
            <button class="btn btn-primary" onclick="exportProject()">
                <span>📥</span> Export
            </button>
        </div>
    </div>

    <!-- Project Overview -->
    <div class="project-overview" id="project-overview">
        <div class="overview-card">
            <h3>Project Information</h3>
            <div class="overview-content">
                <div class="overview-item">
                    <span class="label">Name:</span>
                    <span class="value" id="project-name">Loading...</span>
                </div>
                <div class="overview-item">
                    <span class="label">Status:</span>
                    <span class="value status" id="project-status">Loading...</span>
                </div>
                <div class="overview-item">
                    <span class="label">Created:</span>
                    <span class="value" id="project-created">Loading...</span>
                </div>
                <div class="overview-item">
                    <span class="label">Progress:</span>
                    <span class="value" id="project-progress">0%</span>
                </div>
            </div>
        </div>
    </div>

    <!-- Workflow Pipeline -->
    <div class="workflow-pipeline">
        <div class="pipeline-stage">
            <div class="stage-indicator completed" id="stage-1">
                <span>1</span>
            </div>
            <div class="stage-title">Requirements</div>
            <div class="stage-description">Define project requirements</div>
        </div>
        
        <div class="pipeline-stage">
            <div class="stage-indicator completed" id="stage-2">
                <span>2</span>
            </div>
            <div class="stage-title">Planning</div>
            <div class="stage-description">Technical architecture</div>
        </div>
        
        <div class="pipeline-stage">
            <div class="stage-indicator active" id="stage-3">
                <span>3</span>
            </div>
            <div class="stage-title">Stories</div>
            <div class="stage-description">User stories & tasks</div>
        </div>
        
        <div class="pipeline-stage">
            <div class="stage-indicator" id="stage-4">
                <span>4</span>
            </div>
            <div class="stage-title">Prompts</div>
            <div class="stage-description">Code generation</div>
        </div>
        
        <div class="pipeline-stage">
            <div class="stage-indicator" id="stage-5">
                <span>5</span>
            </div>
            <div class="stage-title">Review</div>
            <div class="stage-description">Quality assurance</div>
        </div>
    </div>

    <!-- Stage Content -->
    <div class="stage-content" id="stage-content">
        <!-- Dynamic content will be loaded here based on current stage -->
    </div>

    <!-- Stage Navigation -->
    <div class="stage-navigation">
        <button class="btn btn-secondary" id="prev-stage" onclick="navigateStage(-1)" disabled>
            ← Previous
        </button>
        <span class="stage-counter" id="stage-counter">Stage 3 of 5</span>
        <button class="btn btn-primary" id="next-stage" onclick="navigateStage(1)">
            Next →
        </button>
    </div>
</div>

@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            const urlParams = new URLSearchParams(window.location.search);
            const projectId = urlParams.get('projectId');
            
            if (!projectId) {
                alert('Project ID is required');
                window.location.href = '/Projects';
                return;
            }

            loadProjectData(projectId);
            loadWorkflowStage(projectId, 3); // Start with Stories stage
            
            // Auto-refresh workflow status
            setInterval(() => loadWorkflowStatus(projectId), 10000);
        });

        let currentStage = 3;
        let projectData = {};

        async function loadProjectData(projectId) {
            try {
                projectData = await APIClient.getProject(projectId);
                
                // Update project overview
                document.getElementById('project-name').textContent = projectData.name;
                document.getElementById('project-status').textContent = projectData.status;
                document.getElementById('project-created').textContent = 
                    new Date(projectData.createdAt).toLocaleDateString();
                
                // Update progress based on stages
                const progress = calculateProgress(projectData);
                document.getElementById('project-progress').textContent = `${progress}%`;
                
                // Update pipeline indicators
                updatePipelineIndicators(progress);
                
            } catch (error) {
                handleApiError(error, 'Failed to load project data');
            }
        }

        function calculateProgress(project) {
            // Mock progress calculation based on project data
            // In real implementation, this would check actual stage completion
            const stages = ['requirements', 'planning', 'stories', 'prompts', 'review'];
            let completed = 0;
            
            stages.forEach(stage => {
                if (project[stage]?.completed) completed++;
            });
            
            return Math.round((completed / stages.length) * 100);
        }

        function updatePipelineIndicators(progress) {
            const stages = ['stage-1', 'stage-2', 'stage-3', 'stage-4', 'stage-5'];
            
            stages.forEach((stageId, index) => {
                const stage = document.getElementById(stageId);
                const stageProgress = ((index + 1) / 5) * 100;
                
                if (progress >= stageProgress) {
                    stage.classList.add('completed');
                    stage.classList.remove('active');
                } else if (index === Math.floor(progress / 20)) {
                    stage.classList.add('active');
                    stage.classList.remove('completed');
                } else {
                    stage.classList.remove('completed', 'active');
                }
            });
        }

        async function loadWorkflowStage(projectId, stage) {
            currentStage = stage;
            
            // Update stage counter
            document.getElementById('stage-counter').textContent = `Stage ${stage} of 5`;
            
            // Update navigation buttons
            document.getElementById('prev-stage').disabled = stage === 1;
            document.getElementById('next-stage').textContent = stage === 5 ? 'Complete' : 'Next →';
            
            // Load stage-specific content
            const content = await getStageContent(projectId, stage);
            document.getElementById('stage-content').innerHTML = content;
        }

        function getStageContent(projectId, stage) {
            const templates = {
                1: getRequirementsStage,
                2: getPlanningStage,
                3: getStoriesStage,
                4: getPromptsStage,
                5: getReviewStage
            };
            
            return templates[stage] ? templates[stage](projectId) : '<p>Stage not found</p>';
        }

        function getRequirementsStage(projectId) {
            return `
                <div class="stage-container">
                    <h2>Requirements Analysis</h2>
                    <div class="requirements-content">
                        <div class="requirements-list" id="requirements-list">
                            <!-- Requirements will be loaded here -->
                        </div>
                        <div class="requirements-actions">
                            <button class="btn btn-primary" onclick="analyzeRequirements()">
                                🔄 Re-analyze Requirements
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        function getPlanningStage(projectId) {
            return `
                <div class="stage-container">
                    <h2>Project Planning</h2>
                    <div class="planning-content">
                        <div class="architecture-overview" id="architecture-overview">
                            <!-- Architecture will be loaded here -->
                        </div>
                        <div class="planning-actions">
                            <button class="btn btn-primary" onclick="regeneratePlan()">
                                🔄 Regenerate Plan
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        function getStoriesStage(projectId) {
            return `
                <div class="stage-container">
                    <h2>User Stories</h2>
                    <div class="stories-content">
                        <div class="stories-controls">
                            <button class="btn btn-primary" onclick="generateStories()">
                                ✨ Generate Stories
                            </button>
                        </div>
                        <div class="stories-list" id="stories-list">
                            <!-- Stories will be loaded here -->
                        </div>
                    </div>
                </div>
            `;
        }

        function getPromptsStage(projectId) {
            return `
                <div class="stage-container">
                    <h2>Prompt Generation</h2>
                    <div class="prompts-content">
                        <div class="prompts-list" id="prompts-list">
                            <!-- Prompts will be loaded here -->
                        </div>
                        <div class="prompts-actions">
                            <button class="btn btn-primary" onclick="generateAllPrompts()">
                                🚀 Generate All Prompts
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        function getReviewStage(projectId) {
            return `
                <div class="stage-container">
                    <h2>Final Review</h2>
                    <div class="review-content">
                        <div class="review-summary" id="review-summary">
                            <!-- Review summary will be loaded here -->
                        </div>
                        <div class="review-actions">
                            <button class="btn btn-success" onclick="completeProject()">
                                ✅ Complete Project
                            </button>
                            <button class="btn btn-secondary" onclick="exportProject()">
                                📥 Export Results
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        async function navigateStage(direction) {
            const newStage = currentStage + direction;
            
            if (newStage >= 1 && newStage <= 5) {
                await loadWorkflowStage(projectId, newStage);
            }
        }

        async function loadWorkflowStatus(projectId) {
            try {
                const status = await APIClient.getStatus(projectId);
                // Update UI based on status changes
                console.log('Workflow status updated:', status);
            } catch (error) {
                console.error('Failed to load workflow status:', error);
            }
        }

        // Placeholder functions for stage actions
        async function analyzeRequirements() {
            showNotification('Analyzing requirements...', 'info');
            // Implementation for requirements analysis
        }

        async function regeneratePlan() {
            showNotification('Regenerating project plan...', 'info');
            // Implementation for plan regeneration
        }

        async function generateStories() {
            showNotification('Generating user stories...', 'info');
            // Implementation for story generation
        }

        async function generateAllPrompts() {
            showNotification('Generating all prompts...', 'info');
            // Implementation for prompt generation
        }

        async function completeProject() {
            showNotification('Completing project...', 'info');
            // Implementation for project completion
        }

        function exportProject() {
            showNotification('Exporting project...', 'info');
            // Implementation for project export
        }

        function showNotification(message, type = 'info') {
            // Implementation for showing notifications
            console.log(`${type.toUpperCase()}: ${message}`);
        }

        // Add CSS for workflow stages
        const style = document.createElement('style');
        style.textContent = `
            .page-header {
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
                margin-bottom: 2rem;
            }
            
            .page-header h1 {
                margin: 0;
                font-size: 2rem;
            }
            
            .page-header .subtitle {
                margin: 0.5rem 0 0 0;
                color: var(--color-gray-600);
            }
            
            .workflow-actions {
                display: flex;
                gap: 0.5rem;
            }
            
            .project-overview {
                margin-bottom: 2rem;
            }
            
            .overview-card {
                background: white;
                border-radius: 12px;
                padding: 1.5rem;
                box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
                border: 1px solid var(--color-gray-200);
            }
            
            .overview-card h3 {
                margin-bottom: 1rem;
                color: var(--color-gray-900);
            }
            
            .overview-content {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                gap: 1rem;
            }
            
            .overview-item {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 0.5rem 0;
                border-bottom: 1px solid var(--color-gray-100);
            }
            
            .overview-item:last-child {
                border-bottom: none;
            }
            
            .overview-item .label {
                font-weight: 500;
                color: var(--color-gray-600);
            }
            
            .overview-item .value {
                font-weight: 500;
                color: var(--color-gray-900);
            }
            
            .overview-item .value.status {
                padding: 0.25rem 0.75rem;
                border-radius: 20px;
                font-size: 0.75rem;
                background: var(--color-success-100);
                color: var(--color-success-700);
            }
            
            .stage-container {
                background: white;
                border-radius: 12px;
                padding: 2rem;
                box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
                border: 1px solid var(--color-gray-200);
                margin-bottom: 2rem;
            }
            
            .stage-container h2 {
                margin-bottom: 1.5rem;
                color: var(--color-gray-900);
            }
            
            .stage-navigation {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 1rem 0;
            }
            
            .stage-counter {
                font-weight: 500;
                color: var(--color-gray-600);
            }
            
            .requirements-content,
            .planning-content,
            .stories-content,
            .prompts-content,
            .review-content {
                margin-top: 1.5rem;
            }
            
            .requirements-list,
            .stories-list,
            .prompts-list,
            .review-summary {
                background: var(--color-gray-50);
                border-radius: 8px;
                padding: 1.5rem;
                margin-bottom: 1rem;
                min-height: 200px;
            }
            
            .requirements-actions,
            .planning-actions,
            .stories-controls,
            .prompts-actions,
            .review-actions {
                display: flex;
                gap: 1rem;
                justify-content: flex-start;
            }
            
            @media (max-width: 768px) {
                .page-header {
                    flex-direction: column;
                    gap: 1rem;
                }
                
                .workflow-actions {
                    width: 100%;
                    justify-content: flex-start;
                }
                
                .overview-content {
                    grid-template-columns: 1fr;
                }
                
                .stage-navigation {
                    flex-direction: column;
                    gap: 1rem;
                }
                
                .stage-navigation .btn {
                    width: 100%;
                }
            }
        `;
        document.head.appendChild(style);
    </script>
}
```

## Phase 3: Additional Pages and Features

### 3.1 Reviews/Queue.cshtml

**File**: `src/AIProjectOrchestrator.API/Reviews/Queue.cshtml`

```html
@{
    ViewData["Title"] = "Review Queue";
}

<div class="container">
    <div class="page-header">
        <div>
            <h1>Review Queue</h1>
            <p class="subtitle">Review and approve AI-generated content</p>
        </div>
        <div class="queue-stats">
            <span class="stat-item">
                <span class="stat-number" id="pending-count">0</span>
                <span class="stat-label">Pending</span>
            </span>
            <span class="stat-item">
                <span class="stat-number" id="approved-count">0</span>
                <span class="stat-label">Approved</span>
            </span>
        </div>
    </div>

    <!-- Filter Tabs -->
    <div class="review-tabs">
        <button class="tab-btn active" data-filter="all">All Reviews</button>
        <button class="tab-btn" data-filter="pending">Pending</button>
        <button class="tab-btn" data-filter="approved">Approved</button>
        <button class="tab-btn" data-filter="rejected">Rejected</button>
    </div>

    <!-- Batch Actions -->
    <div class="batch-actions" id="batch-actions" style="display: none;">
        <span class="batch-text">0 items selected</span>
        <div class="batch-buttons">
            <button class="btn btn-success" onclick="batchApprove()">Approve Selected</button>
            <button class="btn btn-danger" onclick="batchReject()">Reject Selected</button>
            <button class="btn btn-secondary" onclick="clearSelection()">Clear</button>
        </div>
    </div>

    <!-- Reviews List -->
    <div class="reviews-list" id="reviews-container">
        <!-- Reviews will be loaded here -->
    </div>

    <!-- Empty State -->
    <div class="empty-state" id="empty-state" style="display: none;">
        <div class="empty-content">
            <div class="empty-icon">👀</div>
            <h3>No Reviews Pending</h3>
            <p>Reviews will appear here when they need your approval</p>
        </div>
    </div>
</div>

<!-- Review Modal -->
<div class="modal" id="review-modal">
    <div class="modal-content">
        <div class="modal-header">
            <h3 id="modal-title">Review Content</h3>
            <button class="modal-close" onclick="closeModal()">&times;</button>
        </div>
        <div class="modal-body" id="modal-body">
            <!-- Review content will be loaded here -->
        </div>
        <div class="modal-footer">
            <button class="btn btn-secondary" onclick="closeModal()">Cancel</button>
            <button class="btn btn-danger" onclick="rejectReview()">Reject</button>
            <button class="btn btn-success" onclick="approveReview()">Approve</button>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            loadReviews();
            setupEventListeners();
        });

        let currentFilter = 'all';
        let selectedReviews = new Set();
        let currentReview = null;

        function setupEventListeners() {
            // Tab switching
            document.querySelectorAll('.tab-btn').forEach(btn => {
                btn.addEventListener('click', function() {
                    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
                    this.classList.add('active');
                    currentFilter = this.dataset.filter;
                    loadReviews();
                });
            });
        }

        async function loadReviews() {
            try {
                const reviews = await APIClient.getReviews();
                const filteredReviews = filterReviews(reviews, currentFilter);
                
                renderReviews(filteredReviews);
                updateStats(reviews);
            } catch (error) {
                handleApiError(error, 'Failed to load reviews');
            }
        }

        function filterReviews(reviews, filter) {
            switch (filter) {
                case 'pending':
                    return reviews.filter(r => r.status === 'pending');
                case 'approved':
                    return reviews.filter(r => r.status === 'approved');
                case 'rejected':
                    return reviews.filter(r => r.status === 'rejected');
                default:
                    return reviews;
            }
        }

        function renderReviews(reviews) {
            const container = document.getElementById('reviews-container');
            const emptyState = document.getElementById('empty-state');

            if (reviews.length === 0) {
                container.style.display = 'none';
                emptyState.style.display = 'block';
                return;
            }

            container.style.display = 'block';
            emptyState.style.display = 'none';

            container.innerHTML = reviews.map(review => `
                <div class="review-item ${review.status}" data-review-id="${review.id}">
                    <div class="review-header">
                        <div class="review-checkbox">
                            <input type="checkbox" id="review-${review.id}" 
                                   onchange="toggleReviewSelection('${review.id}')">
                        </div>
                        <div class="review-info">
                            <h4>${review.title || 'Untitled Review'}</h4>
                            <div class="review-meta">
                                <span class="review-type">${review.type}</span>
                                <span class="review-date">${new Date(review.createdAt).toLocaleDateString()}</span>
                                <span class="review-status ${review.status}">${review.status}</span>
                            </div>
                        </div>
                    </div>
                    <div class="review-content">
                        <p class="review-description">${review.description || 'No description'}</p>
                        <div class="review-preview">
                            <pre>${review.content ? review.content.substring(0, 200) + '...' : 'No content'}</pre>
                        </div>
                    </div>
                    <div class="review-actions">
                        <button class="btn btn-primary" onclick="openReviewModal('${review.id}')">
                            Review Details
                        </button>
                        ${review.status === 'pending' ? `
                            <button class="btn btn-success" onclick="quickApprove('${review.id}')">
                                Approve
                            </button>
                            <button class="btn btn-danger" onclick="quickReject('${review.id}')">
                                Reject
                            </button>
                        ` : ''}
                    </div>
                </div>
            `).join('');
        }

        function updateStats(reviews) {
            const pending = reviews.filter(r => r.status === 'pending').length;
            const approved = reviews.filter(r => r.status === 'approved').length;
            
            document.getElementById('pending-count').textContent = pending;
            document.getElementById('approved-count').textContent = approved;
        }

        function toggleReviewSelection(reviewId) {
            const checkbox = document.getElementById(`review-${reviewId}`);
            
            if (checkbox.checked) {
                selectedReviews.add(reviewId);
            } else {
                selectedReviews.delete(reviewId);
            }
            
            updateBatchActions();
        }

        function updateBatchActions() {
            const batchActions = document.getElementById('batch-actions');
            const batchText = batchActions.querySelector('.batch-text');
            
            if (selectedReviews.size > 0) {
                batchActions.style.display = 'flex';
                batchText.textContent = `${selectedReviews.size} item${selectedReviews.size > 1 ? 's' : ''} selected`;
            } else {
                batchActions.style.display = 'none';
            }
        }

        function clearSelection() {
            selectedReviews.clear();
            document.querySelectorAll('.review-checkbox input').forEach(cb => cb.checked = false);
            updateBatchActions();
        }

        async function batchApprove() {
            if (selectedReviews.size === 0) return;
            
            try {
                for (const reviewId of selectedReviews) {
                    await APIClient.approveReview(reviewId);
                }
                
                showSuccess(`Approved ${selectedReviews.size} review${selectedReviews.size > 1 ? 's' : ''}`);
                clearSelection();
                loadReviews();
            } catch (error) {
                handleApiError(error, 'Failed to approve reviews');
            }
        }

        async function batchReject() {
            if (selectedReviews.size === 0) return;
            
            const feedback = prompt('Please provide feedback for rejection:');
            if (!feedback) return;
            
            try {
                for (const reviewId of selectedReviews) {
                    await APIClient.rejectReview(reviewId, feedback);
                }
                
                showSuccess(`Rejected ${selectedReviews.size} review${selectedReviews.size > 1 ? 's' : ''}`);
                clearSelection();
                loadReviews();
            } catch (error) {
                handleApiError(error, 'Failed to reject reviews');
            }
        }

        async function quickApprove(reviewId) {
            try {
                await APIClient.approveReview(reviewId);
                showSuccess('Review approved successfully');
                loadReviews();
            } catch (error) {
                handleApiError(error, 'Failed to approve review');
            }
        }

        async function quickReject(reviewId) {
            const feedback = prompt('Please provide feedback for rejection:');
            if (!feedback) return;
            
            try {
                await APIClient.rejectReview(reviewId, feedback);
                showSuccess('Review rejected successfully');
                loadReviews();
            } catch (error) {
                handleApiError(error, 'Failed to reject review');
            }
        }

        async function openReviewModal(reviewId) {
            try {
                currentReview = await APIClient.getReview(reviewId);
                
                document.getElementById('modal-title').textContent = currentReview.title || 'Review Content';
                document.getElementById('modal-body').innerHTML = `
                    <div class="review-details">
                        <div class="detail-item">
                            <strong>Type:</strong> ${currentReview.type}
                        </div>
                        <div class="detail-item">
                            <strong>Status:</strong> <span class="status ${currentReview.status}">${currentReview.status}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Created:</strong> ${new Date(currentReview.createdAt).toLocaleString()}
                        </div>
                        ${currentReview.reviewedAt ? `
                        <div class="detail-item">
                            <strong>Reviewed:</strong> ${new Date(currentReview.reviewedAt).toLocaleString()}
                        </div>
                        ` : ''}
                        <div class="detail-item">
                            <strong>Description:</strong>
                            <p>${currentReview.description || 'No description'}</p>
                        </div>
                        <div class="detail-item">
                            <strong>Content:</strong>
                            <pre class="review-full-content">${currentReview.content || 'No content'}</pre>
                        </div>
                        ${currentReview.feedback ? `
                        <div class="detail-item">
                            <strong>Feedback:</strong>
                            <p class="feedback">${currentReview.feedback}</p>
                        </div>
                        ` : ''}
                    </div>
                `;
                
                document.getElementById('review-modal').style.display = 'block';
            } catch (error) {
                handleApiError(error, 'Failed to load review details');
            }
        }

        function closeModal() {
            document.getElementById('review-modal').style.display = 'none';
            currentReview = null;
        }

        async function approveReview() {
            if (!currentReview) return;
            
            try {
                await APIClient.approveReview(currentReview.id);
                showSuccess('Review approved successfully');
                closeModal();
                loadReviews();
            } catch (error) {
                handleApiError(error, 'Failed to approve review');
            }
        }

        async function rejectReview() {
            if (!currentReview) return;
            
            const feedback = prompt('Please provide feedback for rejection:');
            if (!feedback) return;
            
            try {
                await APIClient.rejectReview(currentReview.id, feedback);
                showSuccess('Review rejected successfully');
                closeModal();
                loadReviews();
            } catch (error) {
                handleApiError(error, 'Failed to reject review');
            }
        }

        // Add CSS for review interface
        const style = document.createElement('style');
        style.textContent = `
            .page-header {
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
                margin-bottom: 2rem;
            }
            
            .page-header h1 {
                margin: 0;
                font-size: 2rem;
            }
            
            .page-header .subtitle {
                margin: 0.5rem 0 0 0;
                color: var(--color-gray-600);
            }
            
            .queue-stats {
                display: flex;
                gap: 2rem;
                background: white;
                padding: 1rem 1.5rem;
                border-radius: 12px;
                box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
                border: 1px solid var(--color-gray-200);
            }
            
            .stat-item {
                display: flex;
                flex-direction: column;
                align-items: center;
                gap: 0.25rem;
            }
            
            .stat-number {
                font-size: 1.5rem;
                font-weight: var(--font-bold);
                color: var(--color-primary-600);
            }
            
            .stat-label {
                font-size: 0.875rem;
                color: var(--color-gray-600);
            }
            
            .review-tabs {
                display: flex;
                gap: 1rem;
                margin-bottom: 1.5rem;
                border-bottom: 1px solid var(--color-gray-200);
            }
            
            .tab-btn {
                padding: 0.75rem 1.5rem;
                background: none;
                border: none;
                border-bottom: 2px solid transparent;
                color: var(--color-gray-600);
                font-weight: 500;
                cursor: pointer;
                transition: all 0.2s ease;
            }
            
            .tab-btn:hover {
                color: var(--color-primary-600);
            }
            
            .tab-btn.active {
                color: var(--color-primary-600);
                border-bottom-color: var(--color-primary-600);
            }
            
            .batch-actions {
                display: none;
                align-items: center;
                gap: 1rem;
                padding: 1rem;
                background: var(--color-gray-50);
                border-radius: 8px;
                margin-bottom: 1rem;
            }
            
            .batch-text {
                font-weight: 500;
                color: var(--color-gray-700);
            }
            
            .reviews-list {
                display: flex;
                flex-direction: column;
                gap: 1rem;
            }
            
            .review-item {
                background: white;
                border-radius: 12px;
                padding: 1.5rem;
                box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
                border: 1px solid var(--color-gray-200);
                transition: all 0.3s ease;
            }
            
            .review-item:hover {
                box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
            }
            
            .review-item.pending {
                border-left: 4px solid var(--color-warning-500);
            }
            
            .review-item.approved {
                border-left: 4px solid var(--color-success-500);
            }
            
            .review-item.rejected {
                border-left: 4px solid var(--color-danger-500);
            }
            
            .review-header {
                display: flex;
                align-items: flex-start;
                gap: 1rem;
                margin-bottom: 1rem;
            }
            
            .review-checkbox {
                margin-top: 0.25rem;
            }
            
            .review-info h4 {
                margin: 0 0 0.5rem 0;
                color: var(--color-gray-900);
            }
            
            .review-meta {
                display: flex;
                gap: 1rem;
                font-size: 0.875rem;
                color: var(--color-gray-600);
            }
            
            .review-type {
                background: var(--color-gray-100);
                padding: 0.25rem 0.5rem;
                border-radius: 4px;
            }
            
            .review-date {
                color: var(--color-gray-500);
            }
            
            .review-status {
                padding: 0.25rem 0.5rem;
                border-radius: 4px;
                font-size: 0.75rem;
                font-weight: 500;
                text-transform: uppercase;
            }
            
            .review-status.pending {
                background: var(--color-warning-100);
                color: var(--color-warning-700);
            }
            
            .review-status.approved {
                background: var(--color-success-100);
                color: var(--color-success-700);
            }
            
            .review-status.rejected {
                background: var(--color-danger-100);
                color: var(--color-danger-700);
            }
            
            .review-description {
                color: var(--color-gray-700);
                margin-bottom: 1rem;
                line-height: 1.5;
            }
            
            .review-preview {
                background: var(--color-gray-50);
                border-radius: 6px;
                padding: 1rem;
                margin-bottom: 1rem;
            }
            
            .review-preview pre {
                margin: 0;
                font-family: var(--font-mono);
                font-size: 0.875rem;
                color: var(--color-gray-700);
                white-space: pre-wrap;
                word-break: break-word;
            }
            
            .review-actions {
                display: flex;
                gap: 0.5rem;
                flex-wrap: wrap;
            }
            
            .review-actions .btn {
                font-size: 0.875rem;
                padding: 0.5rem 1rem;
            }
            
            .empty-state {
                text-align: center;
                padding: 4rem 2rem;
            }
            
            .empty-content {
                max-width: 400px;
                margin: 0 auto;
            }
            
            .empty-icon {
                font-size: 4rem;
                margin-bottom: 1rem;
            }
            
            .empty-state h3 {
                margin-bottom: 0.5rem;
                color: var(--color-gray-900);
            }
            
            .empty-state p {
                color: var(--color-gray-600);
                margin-bottom: 1.5rem;
            }
            
            /* Modal Styles */
            .modal {
                display: none;
                position: fixed;
                z-index: 1000;
                left: 0;
                top: 0;
                width: 100%;
                height: 100%;
                background-color: rgba(0, 0, 0, 0.5);
                backdrop-filter: blur(4px);
            }
            
            .modal-content {
                background-color: white;
                margin: 5% auto;
                padding: 0;
                border-radius: 12px;
                width: 90%;
                max-width: 800px;
                max-height: 80vh;
                overflow: hidden;
                box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.1);
            }
            
            .modal-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 1.5rem;
                border-bottom: 1px solid var(--color-gray-200);
            }
            
            .modal-header h3 {
                margin: 0;
                color: var(--color-gray-900);
            }
            
            .modal-close {
                background: none;
                border: none;
                font-size: 1.5rem;
                color: var(--color-gray-400);
                cursor: pointer;
                padding: 0.25rem;
                border-radius: 4px;
                transition: all 0.2s ease;
            }
            
            .modal-close:hover {
                background: var(--color-gray-100);
                color: var(--color-gray-600);
            }
            
            .modal-body {
                padding: 1.5rem;
                overflow-y: auto;
                max-height: calc(80vh - 140px);
            }
            
            .modal-footer {
                display: flex;
                justify-content: flex-end;
                gap: 0.5rem;
                padding: 1rem 1.5rem;
                border-top: 1px solid var(--color-gray-200);
            }
            
            .review-details {
                display: flex;
                flex-direction: column;
                gap: 1rem;
            }
            
            .detail-item {
                display: flex;
                flex-direction: column;
                gap: 0.25rem;
            }
            
            .detail-item strong {
                color: var(--color-gray-700);
                font-weight: 500;
            }
            
            .detail-item p {
                margin: 0;
                line-height: 1.5;
            }
            
            .review-full-content {

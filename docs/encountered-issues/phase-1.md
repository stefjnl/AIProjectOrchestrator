# Phase 1: API Client Implementation

## Objective
Create a single, focused API client file that handles communication with the .NET backend running on Docker port 8086.

## Context
- Backend API is fully functional at `http://localhost:8086/api`
- Need basic HTTP communication layer before building any UI
- This file will be the foundation for all subsequent frontend functionality

## Requirements

### File to Create
`/js/api.js` - Single file containing all API communication logic

### Technical Specifications

**Base Configuration:**
- API base URL: `http://localhost:8086/api`
- Use fetch() for HTTP requests
- Return raw JSON responses (not wrapped in custom objects)
- Include proper error handling for network and HTTP errors

**HTTP Methods Required:**
```javascript
window.APIClient = {
    baseUrl: 'http://localhost:8086/api',
    
    // Core HTTP methods
    async get(endpoint),
    async post(endpoint, data),
    
    // Project endpoints only (for this phase)
    async getProjects(),
    async getProject(id), 
    async createProject(projectData)
};
```

**Error Handling:**
- Network errors should throw with clear message
- HTTP error status codes (4xx, 5xx) should throw with status and message
- Include response status in error for debugging

**Data Format:**
- Accept and return plain JavaScript objects
- POST requests should send JSON with proper Content-Type header
- No data transformation or wrapping

### Implementation Requirements

**Function Signatures:**
```javascript
// GET /api/projects
async getProjects() 
// Returns: array of project objects

// GET /api/projects/{id}  
async getProject(id)
// Returns: single project object

// POST /api/projects
async createProject(projectData)
// Input: { name: string, description: string }
// Returns: created project object with id
```

**Error Example:**
```javascript
// Should throw errors like:
throw new Error(`HTTP ${response.status}: ${errorMessage}`);
```

### Testing Approach
After implementation, I should be able to test in browser console:
```javascript
// Test project creation
const project = await window.APIClient.createProject({
    name: "Test Project", 
    description: "Test Description"
});
console.log(project);

// Test project retrieval
const projects = await window.APIClient.getProjects();
console.log(projects);
```

## Constraints
- **Single file only** - Do not create any HTML files or other JavaScript files
- **No UI dependencies** - This is pure API logic
- **No external libraries** - Use only native fetch() and JavaScript
- **Global namespace** - Must be accessible as `window.APIClient`
- **No localStorage** - Pure API communication only

Create a clean, minimal, testable API client that serves as the foundation for the frontend rebuild.
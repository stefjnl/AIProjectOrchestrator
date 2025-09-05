# Phase 3: Basic Workflow Page Structure

## Objective
Create a simple workflow page that displays project information and the 4-stage pipeline structure, preparing for state management in Phase 4.

## Context
- Phase 1: Working API client (`/js/api.js`)
- Phase 2: Working project creation flow that redirects to `workflow.html?projectId={id}`
- Need basic workflow page structure before adding complex state management
- Focus on HTML structure and project display functionality

## Requirements

### File to Create

**`/projects/workflow.html` - Main Workflow Interface**
- Load project data using projectId from URL parameter
- Display project information clearly
- Show all 4 workflow stages with basic structure
- Placeholder buttons for each stage (non-functional for now)
- Basic status display for each stage

### Technical Specifications

**Page Structure:**
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Project Workflow - AI Orchestrator</title>
</head>
<body>
    <!-- Navigation back to home -->
    <!-- Project information section -->
    <!-- 4-stage workflow sections -->
    
    <script src="/js/api.js"></script>
    <!-- Page initialization script -->
</body>
</html>
```

**Project Information Display:**
- Project name as page header
- Project description in readable format
- Clear visual separation from workflow stages

**Workflow Stages Structure:**
```html
<!-- Stage 1: Requirements Analysis -->
<div class="workflow-stage">
    <h3>1. Requirements Analysis</h3>
    <div class="stage-status">Not Started</div>
    <button disabled>Start Analysis</button>
</div>

<!-- Stage 2: Project Planning -->
<div class="workflow-stage">
    <h3>2. Project Planning</h3>
    <div class="stage-status">Not Started</div>
    <button disabled>Start Planning</button>
</div>

<!-- Stage 3: User Stories -->
<div class="workflow-stage">
    <h3>3. User Stories</h3>
    <div class="stage-status">Not Started</div>
    <button disabled>Generate Stories</button>
</div>

<!-- Stage 4: Code Generation -->
<div class="workflow-stage">
    <h3>4. Code Generation</h3>
    <div class="stage-status">Not Started</div>
    <button disabled>Generate Code</button>
</div>
```

### JavaScript Requirements

**Page Initialization:**
```javascript
window.addEventListener('DOMContentLoaded', async function() {
    // Get projectId from URL parameter
    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('projectId');
    
    if (!projectId) {
        // Handle missing project ID
        alert('No project ID provided');
        window.location.href = '/index.html';
        return;
    }
    
    try {
        // Load and display project information
        const project = await window.APIClient.getProject(projectId);
        displayProjectInfo(project);
    } catch (error) {
        // Handle API errors
        alert('Error loading project: ' + error.message);
    }
});

function displayProjectInfo(project) {
    // Update page title and project display
    document.title = `${project.name} - Workflow`;
    // Update DOM elements with project data
}
```

**Error Handling:**
- Missing projectId parameter should redirect to home
- Invalid projectId should show error and redirect
- Network errors should show clear error messages
- Fallback navigation if project load fails

### UI Requirements

**Visual Hierarchy:**
- Clear project information at top
- Workflow stages in logical sequence
- Visual separation between stages
- Consistent spacing and layout

**Stage Display:**
- Each stage clearly numbered and labeled
- Status text for each stage ("Not Started" for now)
- Disabled buttons showing future functionality
- Clean, readable layout

**Responsive Design:**
- Works on mobile and desktop
- Stages stack appropriately on small screens
- Readable text at all sizes

### Functional Requirements

**URL Parameter Handling:**
- Extract projectId from `?projectId={id}`
- Validate projectId exists
- Handle malformed URLs gracefully

**Project Loading:**
- Use `window.APIClient.getProject(id)`
- Display project name and description
- Handle loading states and errors

**Navigation:**
- Link back to home/project list
- Breadcrumb or clear navigation context

### Success Criteria

**Testing Flow:**
1. Create project in Phase 2 - should redirect to workflow page
2. Workflow page loads with correct project ID
3. Project information displays correctly
4. All 4 stages show with "Not Started" status
5. All buttons are disabled (preparing for Phase 4)
6. Error handling works for invalid project IDs

**API Integration:**
- Must use `window.APIClient.getProject(projectId)`
- Must handle API responses and errors correctly
- Must display actual project data from backend

## Constraints

- **Single file only** - `/projects/workflow.html`
- **No state management yet** - Just display and structure
- **No functional buttons** - All buttons disabled for now
- **No localStorage** - Pure project display functionality
- **Basic styling only** - Focus on structure over appearance
- **No workflow logic** - Just prepare the foundation

Create a clean, structured workflow page that successfully loads and displays project information, preparing for the state management and workflow functionality in Phase 4.
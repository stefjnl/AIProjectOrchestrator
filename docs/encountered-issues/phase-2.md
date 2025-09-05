# Phase 2: Project Creation Flow

## Objective
Create a simple, functional project creation flow that connects to the working API client from Phase 1.

## Context
- Phase 1 API client (`/js/api.js`) is working and tested
- Need basic project creation before building complex workflow functionality
- Focus on core functionality with clean, minimal UI

## Requirements

### Files to Create

**1. `/index.html` - Landing Page**
- Simple navigation to create projects or view projects
- Load the API client
- Basic HTML structure with navigation

**2. `/projects/create.html` - Project Creation Form**
- Form with project name and description fields
- Submit button that calls API client
- Success handling with redirect to workflow page
- Error handling with user feedback

### Technical Specifications

**Landing Page (`/index.html`):**
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>AI Project Orchestrator</title>
</head>
<body>
    <!-- Simple navigation and main actions -->
    <!-- Link to create new project -->
    <!-- Future: Link to view existing projects -->
    
    <script src="/js/api.js"></script>
</body>
</html>
```

**Project Creation Form:**
```html
<form id="projectForm">
    <input type="text" id="projectName" required>
    <textarea id="projectDescription" required></textarea>
    <button type="submit">Create Project</button>
</form>
```

**Form Submission Logic:**
```javascript
async function createProject(event) {
    event.preventDefault();
    
    // Get form data
    const projectData = {
        name: document.getElementById('projectName').value,
        description: document.getElementById('projectDescription').value
    };
    
    try {
        // Call API client
        const project = await window.APIClient.createProject(projectData);
        
        // Success: redirect to workflow page
        window.location.href = `workflow.html?projectId=${project.id}`;
    } catch (error) {
        // Error: show user-friendly message
        alert('Error creating project: ' + error.message);
    }
}
```

### Functional Requirements

**User Flow:**
1. User visits `/index.html`
2. User clicks "Create New Project"
3. User fills form on `/projects/create.html`
4. User submits form
5. Success: Redirect to `workflow.html?projectId={id}`
6. Error: Show error message, keep user on form

**Form Validation:**
- Both name and description required
- Show loading state during submission
- Disable submit button during API call
- Clear error handling

**Navigation:**
- Simple, clean navigation between pages
- Proper page titles
- Basic responsive design

### UI Requirements

**Styling:**
- Clean, minimal design
- No external CSS frameworks yet
- Basic responsive layout
- Clear visual hierarchy

**User Experience:**
- Form should be intuitive and accessible
- Loading states for API calls
- Clear success/error feedback
- Proper form labels and placeholders

### Success Criteria

**Testing Flow:**
1. Load `/index.html` - should show clean landing page
2. Navigate to create project - should show working form
3. Submit valid project - should redirect to workflow page with project ID
4. Submit invalid data - should show validation errors
5. Network error - should show clear error message

**API Integration:**
- Must use `window.APIClient.createProject()`
- Must handle API responses correctly
- Must pass project ID to workflow page via URL parameter

## Constraints

- **Two files only** - index.html and create.html
- **No complex CSS** - Focus on functionality over styling
- **No state management** - Simple form submission flow
- **No workflow functionality yet** - Just redirect to workflow.html (doesn't need to exist)
- **Use existing API client** - Don't modify api.js

Create a clean, functional project creation flow that successfully creates projects and prepares for the workflow implementation in Phase 3.
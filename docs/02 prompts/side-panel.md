# Implementation Task: Artifacts Side Panel for AI Project Orchestrator

## Business Requirements

**Objective**: Add a persistent side panel to the Project Workflow page that displays all generated artifacts (Requirements Analysis, Project Planning, User Stories, Generated Prompts) for easy access without disrupting the main workflow interface.

**User Problem**: Currently users must navigate between workflow stages or return to review queue to access previously generated content. This breaks workflow continuity and reduces productivity.

**Success Criteria**: Users can view any completed artifact instantly while remaining on the workflow page, improving content accessibility by eliminating navigation friction.

## Technical Context

**Project Architecture**: .NET 9 Web API backend with Clean Architecture (Domain/Application/Infrastructure/API layers), vanilla JavaScript frontend with modular service architecture, Docker containerized deployment.

**Existing Components**:
- `APIClient` global object handles all backend communication
- `WorkflowManager` class manages workflow state with localStorage persistence
- Project workflow page at `/Projects/Workflow` with 5-stage pipeline visualization
- Artifact data available via existing endpoints: `/api/requirements/{id}/status`, `/api/planning/{id}/status`, `/api/stories/{id}/status`, `/api/prompts/{id}/status`

**File Locations**:
- Frontend: `frontend/projects/workflow.html` (main workflow page)
- JavaScript: `frontend/js/workflow.js` (WorkflowManager class)
- API Client: `frontend/js/api.js` (APIClient global object)
- Styles: `frontend/css/styles.css`

## Implementation Requirements

### Frontend Components

**HTML Structure (workflow.html)**
Add side panel container alongside existing workflow pipeline:
```html
<div class="workflow-layout">
  <div class="workflow-main">
    <!-- Existing 5-stage workflow pipeline stays unchanged -->
  </div>
  <div class="artifacts-panel" id="artifactsPanel">
    <div class="panel-header">
      <h3>Generated Artifacts</h3>
      <button class="panel-toggle" id="panelToggle">⟨</button>
    </div>
    <div class="panel-content" id="panelContent">
      <!-- Artifacts dynamically loaded here -->
    </div>
  </div>
</div>
```

**CSS Styling Requirements**
- Panel width: 350px, fixed position on right side
- Collapsible to 40px width when minimized
- Smooth transitions for expand/collapse (0.3s ease)
- Responsive: convert to bottom sheet on mobile (<768px)
- Artifact items: card-like appearance with hover states
- Expandable content areas within each artifact item
- Status indicators: green (approved), yellow (pending), red (rejected)

**JavaScript Functionality (WorkflowManager Extension)**

Add these methods to existing WorkflowManager class:
```javascript
// New methods for artifacts panel
async loadArtifactsPanel() {
  // Fetch all completed artifacts for current project
  // Populate panel with artifact cards
  // Handle empty state display
}

toggleArtifactsPanel() {
  // Collapse/expand panel with animation
  // Update localStorage panel state
}

expandArtifact(artifactType, artifactId) {
  // Load full artifact content
  // Display in expandable content area
  // Handle loading states
}

refreshArtifactStatus() {
  // Update artifact approval statuses
  // Called when returning from review queue
}
```

**API Client Extensions (api.js)**

Add these methods to existing APIClient object:
```javascript
// New artifact retrieval methods
async getRequirementsContent(analysisId) {
  // GET /api/requirements/{id} - return full analysis content
}

async getPlanningContent(planningId) {
  // GET /api/planning/{id} - return full planning content  
}

async getStoriesContent(generationId) {
  // GET /api/stories/{id} - return all user stories
}

async getPromptsContent(generationId) {
  // GET /api/prompts/{id} - return generated prompts
}
```

### Backend API Extensions

**Required New Endpoints** (if content retrieval endpoints don't exist):
- `GET /api/requirements/{id}` - Return full requirements analysis content
- `GET /api/planning/{id}` - Return full project planning content
- `GET /api/stories/{id}` - Return complete user stories collection
- `GET /api/prompts/{id}` - Return generated prompts with metadata

**Response Format** (standardized across all endpoints):
```json
{
  "id": "string",
  "content": "string", 
  "status": "Approved|Pending|Rejected",
  "createdAt": "datetime",
  "approvedAt": "datetime?",
  "title": "string"
}
```

## Implementation Specifications

### Data Flow
1. Page load → WorkflowManager.loadArtifactsPanel() checks for completed artifacts
2. For each completed stage → API call to retrieve artifact content and status
3. Populate panel with artifact cards showing title, status, timestamp
4. User clicks artifact → expandArtifact() loads full content in expandable area
5. Panel state (open/closed, expanded artifacts) persisted to localStorage

### Error Handling
- Loading states during API calls (spinner/skeleton UI)
- Network error fallbacks with retry options
- Empty state messaging when no artifacts exist
- Graceful handling of missing or corrupted artifact data

### Integration Points
- Must work with existing WorkflowManager state management
- Panel updates when workflow stages complete (artifacts added dynamically)
- Integrates with existing review approval flow (status updates)
- Maintains existing workflow navigation and functionality

### Mobile Responsive Behavior
- Desktop: 350px fixed right panel
- Tablet (768px-1024px): 300px panel width
- Mobile (<768px): Convert to bottom sheet that slides up, full width

## Testing Requirements

**Manual Testing Scenarios**:
1. Load workflow page with no completed artifacts → Empty state displays
2. Complete requirements analysis → Panel shows new artifact with "Pending" status
3. Approve requirements in review queue → Return to workflow, artifact shows "Approved"
4. Click artifact → Content expands inline showing full analysis text
5. Complete multiple stages → Panel shows multiple artifacts in chronological order
6. Toggle panel collapse → Smooth animation, state persists on page refresh
7. Test on mobile → Panel converts to bottom sheet behavior

**Edge Cases to Handle**:
- Very long artifact content (>10,000 characters)
- Network timeouts during artifact loading
- Corrupted artifact data or API errors
- Browser localStorage limitations
- Concurrent user sessions modifying same project

## Code Quality Standards

**JavaScript Requirements**:
- Use async/await consistently, avoid callback hell
- Implement proper error handling with try/catch blocks
- Follow existing code patterns from WorkflowManager and APIClient
- Add comprehensive console logging for debugging
- Use semantic variable and function names
- Implement loading states for all async operations

**CSS Requirements**:
- Use CSS custom properties for theming consistency
- Implement smooth transitions and animations
- Follow mobile-first responsive design principles
- Maintain accessibility with proper ARIA labels
- Use flexbox/grid for layout, avoid float-based positioning

**HTML Requirements**:
- Semantic HTML5 structure with proper heading hierarchy
- Accessibility attributes (aria-labels, role attributes)
- Clean separation between structure, styling, and behavior
- Progressive enhancement approach (works without JavaScript)

## Integration Specifications

**Existing Code Modifications**:
- Update `workflow.html` layout structure (add wrapper div)
- Extend WorkflowManager class with artifact panel methods
- Add artifact endpoints to APIClient object  
- Update CSS with new panel styles and responsive breakpoints
- Ensure panel integrates with existing workflow state management

**Backwards Compatibility**:
- All existing workflow functionality must remain unchanged
- No breaking changes to existing API contracts or Razor page functionality
- Maintain existing state management patterns and JavaScript integration
- Preserve existing CSS class names and styling conventions

## Deliverables Checklist

**Analysis Phase**:
- [ ] Document current JavaScript architecture and patterns
- [ ] Identify existing API endpoints and response formats
- [ ] Document current CSS/styling approach and conventions
- [ ] Identify existing state management patterns

**Implementation Phase**:
- [ ] Updated `src/AIProjectOrchestrator.API/Pages/Projects/Workflow.cshtml` with new layout
- [ ] JavaScript artifacts panel functionality (using existing patterns)  
- [ ] CSS styling for panel and responsive design (following existing conventions)
- [ ] Backend endpoint extensions (if needed, following existing patterns)

**Quality Assurance**:
- [ ] All existing workflow functionality works unchanged
- [ ] Panel displays correctly across desktop/tablet/mobile
- [ ] Smooth animations and transitions implemented
- [ ] Error states handled gracefully using existing patterns
- [ ] Loading states provide clear user feedback
- [ ] State persistence working with existing approach

This implementation will provide instant access to generated artifacts while maintaining the existing Razor Pages architecture and not disrupting current functionality.
# Implementation Task: Artifacts Side Panel for AI Project Orchestrator

## Business Requirements

**Objective**: Add a persistent side panel to the Project Workflow page that displays all generated artifacts (Requirements Analysis, Project Planning, User Stories, Generated Prompts) for easy access without disrupting the main workflow interface.

**User Problem**: Currently users must navigate between workflow stages or return to review queue to access previously generated content. This breaks workflow continuity and reduces productivity.

**Success Criteria**: Users can view any completed artifact instantly while remaining on the workflow page, improving content accessibility by eliminating navigation friction.

## Technical Context

**Project Architecture**: .NET 9 Web API with Clean Architecture, ASP.NET Core Razor Pages, vanilla JavaScript with modular service architecture, Docker containerized deployment.

**Target File**: `src/AIProjectOrchestrator.API/Pages/Projects/Workflow.cshtml`

## Implementation Requirements

### Step 1: Codebase Analysis
Before implementation, analyze the existing codebase to understand:
- Current JavaScript architecture and patterns used throughout the application
- Existing API client implementation and available endpoints
- CSS/styling conventions and responsive design patterns
- State management approach used in workflow functionality
- Backend API endpoints available for retrieving full artifact content
- Integration patterns between Razor pages and JavaScript

### Step 2: Workflow Page Enhancement

**Target**: `src/AIProjectOrchestrator.API/Pages/Projects/Workflow.cshtml`

**Required Enhancement**: Add a collapsible side panel (350px width, collapsible to 40px) positioned on the right side of the existing workflow pipeline. The panel should:

- Display "Generated Artifacts" header with toggle button
- Show all completed artifacts for the current project with status indicators
- Show friendly message when certain artifacts are not available yet (i.e. early on in the process)
- Allow expanding individual artifacts to view full content
- Maintain responsive behavior (convert to bottom sheet on mobile)
- Integrate seamlessly with existing page layout and functionality

**Layout Addition** (adapt to existing Razor page patterns):
```html
<div class="artifacts-panel" id="artifactsPanel">
  <div class="panel-header">
    <h3>Generated Artifacts</h3>
    <button class="panel-toggle" id="panelToggle">⟨</button>
  </div>
  <div class="panel-content" id="panelContent">
    <!-- Artifacts dynamically loaded here -->
  </div>
</div>
```

### Step 3: JavaScript Implementation

**Approach**: Follow existing JavaScript patterns to implement:
- Panel toggle functionality with state persistence
- Artifact loading and display using current API patterns  
- Content expansion/collapse for individual artifacts
- Error handling consistent with existing implementation
- Loading states following current conventions

### Step 4: Styling Implementation

**Requirements**: 
- Follow existing CSS conventions and class naming patterns
- Implement smooth transitions (0.3s ease for panel toggle)
- Status indicators: green (approved), yellow (pending), red (rejected)
- Responsive design matching existing breakpoints
- Card-like artifact items with hover states

### Step 5: Backend Integration

**Analysis Needed**: Determine if current API endpoints provide full artifact content retrieval or just status information.

**Implementation**: If content retrieval endpoints don't exist, implement them following existing controller patterns and response formats.

## Data Flow Requirements

1. Page load → Check for completed artifacts using existing API patterns
2. For each completed stage → Retrieve artifact content and status
3. Populate panel with artifact cards showing title, status, timestamp
4. User clicks artifact → Load full content in expandable area
5. Panel state persistence using existing state management approach

## Integration Specifications

**Existing Code Preservation**:
- All current workflow functionality must remain unchanged
- No breaking changes to existing Razor page structure or JavaScript
- Maintain existing state management and API communication patterns
- Follow established CSS conventions and responsive design approach

## Quality Requirements

**Error Handling**: Use existing error handling patterns for network issues and missing data

**Loading States**: Implement loading indicators consistent with current implementation

**Responsive Design**: Ensure panel works across all device sizes using existing responsive patterns

**Performance**: No impact on existing workflow functionality or page load times

## Deliverables

- [ ] Enhanced Workflow.cshtml with integrated side panel
- [ ] JavaScript functionality following existing architectural patterns
- [ ] CSS styling consistent with current design system
- [ ] Backend endpoints (if needed) following existing controller patterns
- [ ] Full integration with existing workflow state management
- [ ] Responsive behavior matching existing mobile/desktop patterns

This implementation will provide instant access to generated artifacts while maintaining consistency with existing codebase patterns and architectural decisions.
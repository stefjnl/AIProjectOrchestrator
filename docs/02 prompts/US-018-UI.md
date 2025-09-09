# Frontend Rebuild: Modern Enterprise AI Project Orchestrator

## Project Overview
Rebuild the frontend for an AI Project Orchestrator - a sophisticated enterprise application that manages a 5-stage AI-driven development workflow. The application orchestrates multiple AI models through human approval checkpoints to transform project ideas into working code.

## Design Requirements

### Visual Style & Branding
- **Modern Enterprise Aesthetic**: Clean, professional, suitable for B2B software
- **Color Scheme**: Primary blue (#2563eb), success green (#10b981), warning amber (#f59e0b), danger red (#ef4444)
- **Typography**: System font stack with clear hierarchy (Inter or similar modern sans-serif)
- **Layout**: Desktop-only, minimum 1200px width, maximum 1920px
- **Inspiration**: GitLab pipelines, Azure DevOps, Linear, Notion - modern SaaS interfaces

### Technical Requirements
- **Modern CSS**: CSS Grid for layouts, Flexbox for components, CSS custom properties
- **No Frameworks**: Vanilla HTML/CSS/JS only (existing JavaScript must be preserved)
- **Responsive Grid**: CSS Grid with auto-fit columns for card layouts
- **Animation**: Subtle CSS transitions and micro-interactions
- **Performance**: Minimal CSS, efficient selectors, optimized rendering

## Application Architecture

### Page Hierarchy & Navigation
```
1. index.html - Dashboard with recent projects and system status
2. projects/create.html - Project creation form with markdown preview
3. projects/list.html - All projects grid with status badges
4. projects/workflow.html - 5-stage workflow management (core interface)
5. projects/stories-overview.html - Individual story management
6. reviews/queue.html - Central approval queue
7. prompt-playground.html - Interactive prompt editor
8. Additional utility pages (faq.html, system-analysis.html, test pages)
```

## Critical JavaScript Integration Requirements

### Existing JavaScript Files & Their Target Pages
The following JavaScript files are page-specific and contain hardcoded DOM selectors:

- **api.js** - Global APIClient, used across all pages
- **app.js** - Application initialization and global utilities  
- **markdown-utils.js** - Markdown rendering for project descriptions
- **project-form.js** - Targets projects/create.html form elements
- **project-overview.js** - Targets projects/list.html grid and card elements
- **projects-list.js** - Additional project list functionality
- **prompt-playground.js** - Targets prompt-playground.html interface
- **reviews-list.js** - Targets reviews/queue.html review cards and actions
- **template-loader.js** - Template system for dynamic content
- **workflow.js** - Targets projects/workflow.html stage buttons and status indicators

### DOM Selector Preservation Requirements
Each JavaScript file expects specific HTML structure and CSS classes/IDs. When implementing new HTML:

1. **Maintain Existing IDs**: Any `document.getElementById()` calls must find their targets
2. **Preserve Form Names**: Form elements with `name` attributes must match existing JavaScript
3. **Keep Button IDs**: Action buttons (submit, approve, reject) must maintain their IDs
4. **Status Container Classes**: Workflow status indicators need consistent class names
5. **Navigation Elements**: Global navigation must preserve existing click handlers

### Testing Strategy
After implementing each page:
1. Load the page in browser
2. Open developer console 
3. Verify no JavaScript errors
4. Test all interactive elements (buttons, forms, navigation)
5. Confirm API calls execute properly

**Priority**: JavaScript compatibility is more important than perfect visual design. If there's a conflict between modern CSS and JavaScript functionality, preserve the JavaScript.

## High-Risk Integration Points

### Workflow Page (projects/workflow.html)
- Stage buttons with specific IDs: `startRequirementsBtn`, `startPlanningBtn`, etc.
- Status display elements that JavaScript updates dynamically
- Progress indicators that show percentage completion

### Review Queue (reviews/queue.html)  
- Review cards with approve/reject buttons containing review IDs
- Dynamic content loading that populates review list
- Pipeline-aware redirect logic after approvals

### Project Forms (projects/create.html)
- Form submission handlers expecting specific input names
- Real-time markdown preview targeting specific DOM elements
- Validation message containers

**Implementation Approach**: Build HTML structure first, then verify JavaScript integration before adding advanced CSS styling.

### Core UI Components Needed

#### 1. Global Navigation Header
- Consistent across all pages
- 4 primary navigation buttons: Create Project, View All Projects, Review Queue, Prompt Playground
- Modern button styling with hover states and active indicators

#### 2. Project Cards
- Used in dashboard and project list
- Display: title, description (truncated), creation date, current stage status
- Status badges with color coding
- Action buttons (Continue Workflow, Delete)
- Hover effects and smooth transitions

#### 3. Workflow Pipeline Visualization
- 5-stage horizontal progress indicator with connecting lines
- Stage states: Not Started, In Progress, Pending Review, Approved, Failed
- Milestone dots with status colors and completion percentages
- Clean, modern pipeline aesthetic (reference: GitLab CI/CD interface)

#### 4. Form Components
- Modern input styling with floating labels or clean placeholder text
- Textarea with live markdown preview
- Primary and secondary button styles
- Form validation indicators

#### 5. Review Interface
- Review cards with approve/reject actions
- Expandable content areas
- Clear action buttons with loading states
- Success/error feedback with toast notifications

#### 6. Status Indicators & Badges
- Color-coded status badges for workflow stages
- Loading spinners and progress indicators
- System health indicators
- Notification counters

## JavaScript Integration Requirements

### Critical: Preserve Existing JavaScript
- **window.APIClient** object must work unchanged
- **WorkflowManager** class integration for workflow.html
- **renderMarkdownToHTML()** function for markdown rendering
- All existing event handlers and API calls must function
- Form submission handlers and validation logic
- Auto-polling and state management systems

### Required JavaScript Integration Points
```javascript
// Global navigation - ensure these work on all pages
window.APIClient.getProjects()
window.APIClient.createProject()
window.APIClient.getPendingReviews()

// Workflow page - preserve all functionality
WorkflowManager class with localStorage state
startRequirementsAnalysis()
startProjectPlanning()
startStoryGeneration()
generatePrompt()
startCodeGeneration()

// Review queue - maintain approval workflow
loadPendingReviews()
approveReview()
rejectReview()

// Project management
populateProjectList()
deleteProject()
```

## Specific Implementation Instructions

### CSS Architecture
```css
:root {
  /* Color System */
  --color-primary: #2563eb;
  --color-success: #10b981;
  --color-warning: #f59e0b;
  --color-danger: #ef4444;
  --color-gray-50: #f9fafb;
  --color-gray-100: #f3f4f6;
  --color-gray-900: #111827;
  
  /* Typography */
  --font-family-base: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui;
  --font-size-xs: 0.75rem;
  --font-size-sm: 0.875rem;
  --font-size-base: 1rem;
  --font-size-lg: 1.125rem;
  --font-size-xl: 1.25rem;
  --font-size-2xl: 1.5rem;
  
  /* Spacing */
  --space-2: 0.5rem;
  --space-4: 1rem;
  --space-6: 1.5rem;
  --space-8: 2rem;
  
  /* Layout */
  --container-max-width: 1200px;
  --border-radius: 0.5rem;
  --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1);
}
```

### Layout Patterns
- **Container Pattern**: Max-width containers with auto margins
- **Card Grid**: CSS Grid with `grid-template-columns: repeat(auto-fit, minmax(300px, 1fr))`
- **Sidebar Layouts**: CSS Grid with fixed sidebar widths
- **Flexbox Components**: Navigation bars, button groups, form layouts

### Component Specifications

#### Project Card Design
```css
.project-card {
  display: flex;
  flex-direction: column;
  padding: var(--space-6);
  background: white;
  border-radius: var(--border-radius);
  box-shadow: var(--shadow-sm);
  transition: all 0.2s ease;
}

.project-card:hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-2px);
}
```

#### Workflow Pipeline Component
- Horizontal layout with connecting lines
- Stage indicators as circles with icons or numbers
- Progress bar or connecting lines between stages
- Clear visual hierarchy for stage status

#### Modern Button System
```css
.btn-primary {
  background: var(--color-primary);
  color: white;
  border: none;
  padding: var(--space-3) var(--space-6);
  border-radius: var(--border-radius);
  transition: all 0.2s ease;
}

.btn-primary:hover {
  background: color-mix(in srgb, var(--color-primary) 80%, black);
  transform: translateY(-1px);
}
```

## Content Preservation Requirements

### Existing Content to Maintain
- All navigation button text and functionality
- Form field labels and validation messages
- Status text and error messages
- Project creation flow and workflow stage names
- Review approval interface text

### JavaScript Dependencies
- Preserve all existing API integration code
- Maintain event listener bindings
- Keep localStorage state management
- Preserve error handling and retry logic

## Deliverables Required

### Primary Pages (Implement in Order)
1. **index.html** - Dashboard with modern card layout and status overview
2. **projects/workflow.html** - Core 5-stage pipeline interface with modern progress visualization
3. **projects/create.html** - Clean form design with real-time markdown preview
4. **projects/list.html** - Grid-based project overview with status filtering
5. **reviews/queue.html** - Modern review interface with clear approve/reject actions

### CSS Structure
- **styles.css** - Single comprehensive stylesheet with:
  - CSS custom properties (variables)
  - Modern reset/normalize
  - Component-based organization
  - Responsive utilities
  - Animation/transition library

### Success Criteria
- All existing JavaScript functionality works unchanged
- Modern, professional enterprise aesthetic
- Smooth animations and micro-interactions
- Consistent design system across all pages
- Improved user experience while maintaining workflow integrity
- Desktop-optimized layouts with proper information hierarchy

Focus on creating a cohesive, modern enterprise application that feels like a premium SaaS product while preserving all existing functionality.
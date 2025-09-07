## AI Coding Assistant Implementation Prompt

**Objective**: Create a unified project overview interface that consolidates all project content (requirements, planning, stories, prompts) into a sophisticated tabbed interface with smooth navigation.

### **Implementation Task**

Create `/frontend/projects/overview.html` that replaces the current fragmented navigation with a single comprehensive project interface.

### **Required Features**

1. **Tabbed Navigation Interface** with 5 tabs:
   - Pipeline Status (current workflow state)
   - Requirements Analysis (formatted content display)
   - Project Planning (roadmap and architecture)
   - User Stories (current stories grid)
   - Generated Prompts (organized by story)

2. **Hash-Based Routing**: URL structure `/projects/overview.html?projectId=16#requirements` for direct tab linking

3. **Content Loading Strategy**: Lazy load content only when tabs are accessed, with caching to prevent repeated API calls

4. **Responsive Design**: Professional enterprise interface with smooth transitions

### **Technical Requirements**

#### **File Structure**
```
frontend/projects/overview.html  # New unified interface
frontend/js/project-overview.js  # New JavaScript module
```

#### **Core JavaScript Classes**
```javascript
class ProjectOverviewManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.activeTab = this.getHashFromURL() || 'pipeline';
        this.contentCache = new Map();
    }
    
    async switchTab(tabName) {
        // Show loading, load content, update UI, update URL hash
    }
    
    async loadTabContent(tabName) {
        // Check cache first, then API call, then cache result
    }
}

class ContentRenderer {
    static renderRequirements(data) {
        // Format requirements analysis content
    }
    
    static renderPlanning(data) {
        // Format project planning content
    }
    
    // Additional render methods for each content type
}
```

#### **API Integration Points**
Use existing API endpoints:
- `GET /api/projects/{id}` - Project basic info
- `GET /api/requirements/{analysisId}` - Requirements content
- `GET /api/projectplanning/{planningId}` - Planning content  
- `GET /api/stories/{storyGenId}/approved` - Stories data
- `GET /api/prompts/{promptId}` - Individual prompt content
- `GET /api/review/workflow-status/{projectId}` - Overall status

#### **HTML Structure**
```html
<div class="project-overview">
    <header class="project-header">
        <h1 id="projectTitle"></h1>
        <div class="progress-summary" id="progressSummary"></div>
    </header>
    
    <nav class="content-tabs">
        <button class="tab-button active" data-tab="pipeline">Pipeline</button>
        <button class="tab-button" data-tab="requirements">Requirements</button>
        <button class="tab-button" data-tab="planning">Planning</button>
        <button class="tab-button" data-tab="stories">Stories</button>
        <button class="tab-button" data-tab="prompts">Prompts</button>
    </nav>
    
    <main class="content-area">
        <div class="loading-spinner" id="loadingSpinner" style="display: none;"></div>
        <div class="tab-content" id="tabContent"></div>
    </main>
</div>
```

#### **CSS Requirements**
- Modern tab interface with active state styling
- Smooth transitions between tabs (0.3s ease)
- Loading spinner for content switches
- Responsive design for mobile/tablet
- Professional enterprise styling consistent with existing interface

### **Content Display Specifications**

#### **Pipeline Tab**
Reuse existing workflow interface from `workflow.html` - show current stage status and next actions

#### **Requirements Tab**
```javascript
// Display format:
{
    status: "Approved/Pending/Rejected",
    content: "Full requirements analysis text",
    createdAt: "timestamp",
    actions: ["Regenerate", "Export", "Edit"]
}
```

#### **Planning Tab**
```javascript
// Display format:
{
    roadmap: "Project roadmap text",
    architecture: "Architecture decisions",
    milestones: "Milestone definitions",
    status: "Approved/Pending",
    actions: ["Update", "Export"]
}
```

#### **Stories Tab**
Integrate existing stories grid from `stories-overview.html` with enhancements:
- Story cards with status indicators
- Generate prompt buttons
- View existing prompts links

#### **Prompts Tab**
```javascript
// Display format per story:
{
    storyTitle: "User Registration",
    prompts: [
        {
            id: "guid",
            content: "Full prompt text",
            status: "Approved/Pending",
            createdAt: "timestamp",
            actions: ["Copy", "Download", "Regenerate"]
        }
    ]
}
```

### **Implementation Steps**

1. **Create base HTML structure** with tab navigation
2. **Implement ProjectOverviewManager class** with tab switching logic
3. **Add URL hash routing** for direct tab access
4. **Create content loading system** with caching
5. **Implement content renderers** for each tab type
6. **Add loading states and transitions**
7. **Style the interface** with professional CSS
8. **Test all tabs** and navigation flows

### **Integration Requirements**

- **Reuse existing APIClient**: All API calls through `window.APIClient`
- **Maintain WorkflowManager**: Pipeline tab should use existing workflow logic
- **Preserve functionality**: All current features (story generation, prompt creation) must work in new interface
- **Update navigation**: Modify existing pages to link to new overview interface

### **Expected Deliverables**

1. `/frontend/projects/overview.html` - Complete unified interface
2. `/frontend/js/project-overview.js` - JavaScript module with all required classes
3. Updated CSS in `/frontend/css/styles.css` for new interface
4. Modified existing pages to link to new overview (workflow.html, stories-overview.html)

### **Success Criteria**

- Single page displays all project content through tabs
- Smooth navigation between tabs with loading states
- Hash-based URLs work for direct tab access  
- All existing functionality preserved and accessible
- Professional enterprise-grade interface design
- Mobile responsive layout

**Note**: This replaces the need for separate `stories-overview.html` and enhances the overall user experience by consolidating all project-related content into one sophisticated interface.
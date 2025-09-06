# Phase 1 Implementation: Dedicated Stories Management Page

## Project Context & Vision
You are implementing **Phase 1 of 3** for the AI Project Orchestrator's transition from linear workflow to project management UX. The system currently has a 4-stage linear workflow, but after Stage 3 (User Stories) completion, it needs to transform into a sophisticated project management interface for individual story/prompt handling.

**Current System State:**
- ✅ Stages 1-3: Linear workflow (Requirements → Planning → Stories) working
- ✅ Backend: Complete prompt generation API endpoints functional
- ✅ US-011A: API client extensions and WorkflowManager enhancements implemented
- 🔄 **Phase 1 Goal**: Create standalone stories management page

## Strategic Vision (3-Phase Implementation)

### **Phase 1: Create Dedicated Stories Management Page** ← **YOU ARE HERE**
Build `frontend/projects/stories-overview.html` as comprehensive story/prompt management interface

### **Phase 2: Modify Workflow Integration** (Future)
Update Stage 3 completion to redirect to stories page and simplify Stage 4

### **Phase 3: Enhanced State Management** (Future) 
Optimize cross-page state coordination and navigation flow

## Current Architecture Assets Available

### **Existing Components You Can Use:**
- **`window.APIClient`**: Has prompt generation methods from US-011A
  - `generatePrompt(storyGenerationId, storyIndex, preferences)`
  - `getPromptStatus(promptId)`
  - `canGeneratePrompt(storyGenerationId, storyIndex)`
  - `getPrompt(promptId)`
- **`WorkflowManager`**: Has story-level state tracking from US-011A
  - `storyPrompts` object for tracking prompt IDs and status
  - `approvedStories` array for story data
  - `setStoryPromptId()`, `getStoryPromptStatus()` methods
  - `checkStoryPromptApprovals()` for status polling

### **URL Parameters Available:**
- Page will be accessed via: `stories-overview.html?projectId={projectId}`
- WorkflowManager will be initialized with this projectId

## Phase 1 Requirements: Standalone Stories Management Page

### **File to Create:**
`frontend/projects/stories-overview.html`

### **Page Architecture:**
```
Header Section
├── Breadcrumb navigation (Home > Projects > Workflow > Stories & Prompts)  
├── Project title and description
└── Back to workflow link

Summary Statistics
├── Total Stories count
├── Prompts Generated count
├── Prompts Approved count  
└── Pending Reviews count

Bulk Operations Bar
├── Select All/None toggle
├── Generate Selected Prompts button
├── Download All Approved button
└── Review Queue link with count

Stories Grid
├── Story cards in responsive grid (min 500px width)
├── Each card shows: title, user story, acceptance criteria, priority, story points
├── Status indicator per story (Not Started/Pending/Approved/Rejected)
└── Action buttons per story based on status

Navigation Footer
├── Back to Workflow button
└── Continue to Code Generation button (disabled until all approved)

Prompt Viewing Modal
├── Full-screen overlay for viewing generated prompts
├── Copy to clipboard functionality
└── Download individual prompt as .md file
```

### **Key Functional Requirements:**

#### **Story Display & Management**
- Load approved stories from WorkflowManager state (use mock data for Phase 1)
- Display stories as cards with full acceptance criteria visible
- Show priority level (High/Medium/Low) and story points
- Visual status indicators with color coding
- Individual story selection with checkboxes

#### **Prompt Generation Workflow**
- Generate prompt button per story (when status = "Not Started")
- Use existing `window.APIClient.generatePrompt()` method
- Update WorkflowManager state with prompt IDs
- Show "Generating..." state during processing
- Display appropriate actions based on prompt status

#### **Status Management**
- Poll for prompt approval status using existing `checkStoryPromptApprovals()`
- Update summary statistics dynamically
- Show pending review count in review queue link
- Enable/disable "Continue to Code Generation" based on completion

#### **Bulk Operations**
- Select/deselect all stories functionality
- Generate prompts for multiple selected stories
- Download all approved prompts as individual .md files
- Smart selection (only allow generation for "Not Started" stories)

#### **Prompt Viewing**
- Modal overlay for viewing approved prompts
- Monospace font for technical content
- Copy to clipboard functionality
- Download individual prompts with meaningful filenames

### **Technical Implementation Notes:**

#### **Mock Data for Phase 1**
Since actual story API may not exist yet, use this mock data structure:
```javascript
const mockStories = [
    {
        id: 1,
        title: "User Registration",
        userType: "new user",
        wantStatement: "create an account with email and password",
        soThatStatement: "I can access personalized features",
        acceptanceCriteria: [
            "User can enter email and password",
            "Email validation is performed", 
            "Password meets security requirements",
            "Account is created successfully",
            "Confirmation email is sent"
        ],
        priority: "High",
        storyPoints: 5
    },
    // Add 2-3 more realistic stories for testing
];
```

#### **Integration Patterns**
- Use existing `WorkflowManager` initialization pattern
- Follow existing error handling patterns from other pages
- Use existing localStorage state persistence
- Maintain consistency with existing CSS styling approach

#### **Performance Considerations**
- Efficient re-rendering of story grid on status changes
- Proper event listener cleanup
- Smooth modal open/close transitions
- Responsive grid layout for various screen sizes

### **Definition of Done for Phase 1:**
- [ ] Standalone stories-overview.html page created and functional
- [ ] Stories display as comprehensive cards with all acceptance criteria
- [ ] Individual prompt generation works per story
- [ ] Status tracking and updates work correctly
- [ ] Modal prompt viewing with copy/download functionality
- [ ] Bulk operations (select all, generate selected) functional
- [ ] Summary statistics update dynamically
- [ ] Page integrates with existing WorkflowManager and APIClient
- [ ] Responsive design works on desktop and tablet
- [ ] Mock data provides realistic testing scenarios

### **Out of Scope for Phase 1:**
- Workflow.html modifications (Phase 2)
- Automatic redirect from Stage 3 completion (Phase 2)
- Advanced state synchronization optimizations (Phase 3)
- Real story API integration (use mocks for now)

## Implementation Context

**Current State:** You have a working US-011A implementation with API client and WorkflowManager extensions. Now create the dedicated management page that provides a superior UX for handling multiple story prompts.

**Testing Approach:** Access page directly via URL with projectId parameter. Verify all functionality works with mock story data before integrating with workflow.

**Architecture Philosophy:** This page represents the transition from linear workflow to project management - users should feel they're managing a collection of development tasks rather than progressing through stages.

Build a professional, comprehensive interface that demonstrates the sophistication of the AI Project Orchestrator as a prompt engineering platform.
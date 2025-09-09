# UI Requirements Document

## Page Structure

### Landing & Navigation Pages
- **index.html**: Main dashboard with system status overview, recent projects display, and primary navigation buttons
- **faq.html**: System analysis documentation page with architecture diagrams and detailed component breakdown

### Project Management Pages
- **projects/create.html**: Project creation form with real-time Markdown preview, validation, and submission handling
- **projects/list.html**: Enterprise card grid displaying all projects with status badges, stage information, and delete functionality
- **projects/workflow.html**: 5-stage workflow management interface with progress indicators, status tracking, and stage-specific actions
- **projects/stories-overview.html**: Individual story management interface with approval/rejection, prompt generation, and bulk operations

### Review & Quality Pages
- **reviews/queue.html**: Central review queue for all pending approvals with approve/reject/delete actions and pipeline-aware redirects

### Development & Testing Pages
- **prompt-playground.html**: Interactive prompt template editor with LLM integration and response management
- **system-analysis.html**: System health monitoring dashboard with AI model status and performance metrics
- **test-scenarios.html**: Predefined and custom project scenario testing interface
- **test-truncation-simple.html**: Text truncation functionality testing page

### Template & Utilities
- **template/master.html**: Base template structure (under construction)

## Required JavaScript Integration

### Core API Client ([`js/api.js`](src/AIProjectOrchestrator.API/wwwroot/js/api.js:1))
- Centralized REST API communication with error handling and circuit breaker pattern
- Supports GET, POST, DELETE operations with JSON parsing and validation
- Handles network errors and provides user-friendly error messages

### Workflow Management ([`js/workflow.js`](src/AIProjectOrchestrator.API/wwwroot/js/workflow.js:1))
**WorkflowManager Class Integration:**
- **State Management**: API-driven state synchronization with polling (2s active, 30s idle)
- **Progress Tracking**: 4-stage milestone system with visual progress indicators
- **Circuit Breaker**: API failure tracking with maintenance mode fallback
- **Stage Navigation**: Prerequisite validation and pipeline-aware redirects

**Key Methods:**
- [`loadStateFromAPI()`](src/AIProjectOrchestrator.API/wwwroot/js/workflow.js:110): Initialize workflow state from backend
- [`updateWorkflowUI()`](src/AIProjectOrchestrator.API/wwwroot/js/workflow.js:556): Apply enterprise status badges and button styling
- [`updateProgressIndicator()`](src/AIProjectOrchestrator.API/wwwroot/js/workflow.js:687): Synchronize progress bar and milestone animations
- [`getPromptCompletionProgress()`](src/AIProjectOrchestrator.API/wwwroot/js/workflow.js:96): Calculate Stage 4 completion statistics

### Markdown Utilities ([`js/markdown-utils.js`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:1))
- **Secure Rendering**: [`renderMarkdownToHTML()`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:9) with DOMPurify sanitization
- **Text Truncation**: [`initTruncatedDescription()`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:93) with expand/collapse functionality
- **HTML Escaping**: [`escapeHTML()`](src/AIProjectOrchestrator.API/wwwroot/js/markdown-utils.js:51) for XSS prevention

### Project-Specific Scripts
- **projects-list.js**: Project card grid population with stage status calculation
- **project-form.js**: Form validation and submission handling
- **reviews-list.js**: Review queue management with approval/rejection workflows
- **prompt-playground.js**: Template management and LLM interaction
- **template-loader.js**: Dynamic template loading system

## Key Functional Requirements

### Visual Design & User Experience
1. **Enterprise Status System**: Consistent status-badge classes (status-approved, status-pending-review, status-not-started)
2. **Progress Indicators**: Milestone-based progress tracking with animated fill bars and completion percentages
3. **Responsive Card Layout**: Grid-based project cards with hover effects and action buttons
4. **Real-time Updates**: Polling-based state synchronization without page reloads
5. **Loading States**: Spinner indicators and disabled states during API operations

### Navigation Flow
1. **Project Creation Flow**: 
   - index.html → create.html → workflow.html (with projectId)
   
2. **Workflow Stage Progression**:
   - Stage 1: Requirements Analysis → Review Queue → Approval → Stage 2
   - Stage 2: Project Planning → Review Queue → Approval → Stage 3  
   - Stage 3: User Stories → Review Queue → Approval → stories-overview.html
   - Stage 4: Individual Story Prompts → Review Queue → Approval → Stage 5
   - Stage 5: Code Generation → Completion

3. **Review System Integration**:
   - Pipeline-aware redirects (stories → stories-overview.html, others → workflow.html)
   - Centralized queue with project context preservation

### Data Management & State
1. **API-First Architecture**: All state managed through backend API calls
2. **Feature Flags**: Enable/disable dynamic features (enableDynamicStage4, enableStoriesMVP)
3. **Error Handling**: Circuit breaker pattern with maintenance mode fallback
4. **Local Storage**: Minimal usage, primarily for UI preferences and temporary state

### Security & Validation
1. **Input Sanitization**: DOMPurify integration for all user-generated content
2. **XSS Prevention**: HTML escaping for all dynamic content injection
3. **Form Validation**: Client-side validation with real-time feedback
4. **API Error Handling**: Structured error responses with user-friendly messages

### Performance Optimization
1. **Adaptive Polling**: Faster polling (2s) during active operations, slower (30s) when idle
2. **Lazy Loading**: On-demand content loading with loading indicators
3. **DOM Optimization**: Efficient element updates without full page refreshes
4. **Resource Management**: Proper cleanup of event listeners and intervals

### Accessibility Requirements
1. **ARIA Labels**: Comprehensive labeling for screen readers
2. **Keyboard Navigation**: Full keyboard accessibility support
3. **Color Contrast**: WCAG-compliant color schemes
4. **Focus Management**: Proper focus handling during dynamic updates

### Integration Points
1. **Backend API**: RESTful endpoints for all data operations
2. **AI Model Integration**: OpenRouter API with retry logic and fallback
3. **Database Operations**: Entity Framework Core with PostgreSQL
4. **External Libraries**: Marked.js, DOMPurify, and enterprise CSS frameworks

## Technical Implementation Notes

### CSS Architecture
- Enterprise status badge system with consistent styling
- Responsive grid layouts for project cards and story management
- Animated progress indicators and micro-interactions
- Mobile-first responsive design patterns

### JavaScript Architecture
- Modular class-based design with clear separation of concerns
- Async/await patterns with proper error handling
- Event-driven architecture with proper cleanup
- State management through centralized WorkflowManager class

### API Integration Patterns
- Consistent error handling and user feedback
- Retry logic with exponential backoff
- Circuit breaker pattern for resilience
- Structured logging and debugging support
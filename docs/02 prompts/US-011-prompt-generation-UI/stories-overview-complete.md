# Implementation Plan: Dedicated Stories Overview Page

## **Strategic Approach: Transform to Project Management UX**

The following describes a workflow transition from **linear pipeline** (Stages 1-3) to **project management** (Story/Prompt management). This creates a much more intuitive user experience.

## **Implementation Strategy**

### **Phase 1: Create Dedicated Stories Management Page**
Build `frontend/projects/stories-overview.html` as a comprehensive story/prompt management interface.

### **Phase 2: Modify Workflow Integration** 
Update Stage 3 completion to redirect and simplify Stage 4 to a navigation link.

### **Phase 3: Enhanced State Management**
Extend WorkflowManager for cross-page story/prompt state coordination.

---

# Complete Implementation Prompt for AI Assistant

## Project Context
You are implementing **Option B: Dedicated Stories Overview Page** for the AI Project Orchestrator. This transforms the workflow from linear progression to project management after Stage 3 (User Stories) completion. Users need a dedicated interface for managing individual story prompts rather than cramming this into the existing workflow page.

## Current System Architecture
- **Stages 1-3**: Linear workflow (Requirements → Planning → Stories)
- **Stage 4**: **NEW APPROACH** - Redirect to dedicated management page
- **Backend**: Complete prompt generation API endpoints functional
- **Frontend**: Vanilla JavaScript with `window.APIClient` and `WorkflowManager` classes

## Implementation Requirements

### 1. Create Stories Overview Page

**File**: `frontend/projects/stories-overview.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Stories & Prompt Management - AI Project Orchestrator</title>
    <link rel="stylesheet" href="../css/styles.css">
</head>
<body>
    <div class="container">
        <!-- Header Section -->
        <div class="page-header">
            <div class="breadcrumb">
                <a href="../index.html">← Home</a> > 
                <a href="list.html">Projects</a> > 
                <a href="#" id="workflowLink">Workflow</a> > 
                <strong>Stories & Prompts</strong>
            </div>
            <h1 id="projectTitle">Project Stories & Prompt Management</h1>
            <p id="projectDescription"></p>
        </div>

        <!-- Stories Summary Section -->
        <div class="stories-summary">
            <div class="summary-stats">
                <div class="stat-card">
                    <div class="stat-number" id="totalStories">0</div>
                    <div class="stat-label">Total Stories</div>
                </div>
                <div class="stat-card">
                    <div class="stat-number" id="promptsGenerated">0</div>
                    <div class="stat-label">Prompts Generated</div>
                </div>
                <div class="stat-card">
                    <div class="stat-number" id="promptsApproved">0</div>
                    <div class="stat-label">Prompts Approved</div>
                </div>
                <div class="stat-card">
                    <div class="stat-number" id="pendingReviews">0</div>
                    <div class="stat-label">Pending Reviews</div>
                </div>
            </div>
        </div>

        <!-- Bulk Actions Bar -->
        <div class="bulk-actions">
            <div class="actions-left">
                <button id="selectAllBtn" onclick="toggleSelectAll()">Select All</button>
                <button id="generateSelectedBtn" onclick="generateSelectedPrompts()" disabled>
                    Generate Selected Prompts
                </button>
                <button id="downloadApprovedBtn" onclick="downloadApprovedPrompts()">
                    Download All Approved
                </button>
            </div>
            <div class="actions-right">
                <button onclick="refreshStatus()">Refresh Status</button>
                <button onclick="window.location.href='../reviews/queue.html'">
                    Review Queue (<span id="queueCount">0</span>)
                </button>
            </div>
        </div>

        <!-- Stories Grid -->
        <div class="stories-grid" id="storiesGrid">
            <!-- Story cards populated dynamically -->
        </div>

        <!-- Navigation Footer -->
        <div class="page-footer">
            <button onclick="goBackToWorkflow()" class="secondary-btn">
                ← Back to Workflow
            </button>
            <button id="continueToCodeBtn" onclick="continueToCodeGeneration()" 
                    class="primary-btn" disabled>
                Continue to Code Generation →
            </button>
        </div>
    </div>

    <!-- Prompt Viewing Modal -->
    <div id="promptModal" class="prompt-modal" style="display: none;">
        <div class="prompt-modal-content">
            <div class="prompt-modal-header">
                <h3 id="promptModalTitle">Generated Coding Prompt</h3>
                <button onclick="closePromptModal()" class="close-btn">×</button>
            </div>
            <div class="prompt-modal-body">
                <pre id="promptContent" class="prompt-content"></pre>
            </div>
            <div class="prompt-modal-actions">
                <button onclick="copyPromptToClipboard()">Copy to Clipboard</button>
                <button onclick="downloadCurrentPrompt()">Download .md</button>
                <button onclick="closePromptModal()" class="secondary-btn">Close</button>
            </div>
        </div>
    </div>

    <script src="../js/api.js"></script>
    <script src="../js/workflow.js"></script>
    <script>
        let workflowManager;
        let selectedStories = new Set();
        let currentPromptData = null;

        // Initialize page
        window.addEventListener('DOMContentLoaded', async function() {
            const urlParams = new URLSearchParams(window.location.search);
            const projectId = urlParams.get('projectId');
            
            if (!projectId) {
                alert('Project ID not found. Redirecting to projects list.');
                window.location.href = 'list.html';
                return;
            }

            workflowManager = new WorkflowManager(projectId);
            workflowManager.loadState();

            await initializePage();
            await loadProjectInfo();
            await loadApprovedStories();
            await refreshAllStatus();
            
            updateSummaryStats();
            renderStoriesGrid();
        });

        async function initializePage() {
            const urlParams = new URLSearchParams(window.location.search);
            const projectId = urlParams.get('projectId');
            
            // Set up navigation links
            document.getElementById('workflowLink').href = `workflow.html?projectId=${projectId}`;
        }

        async function loadProjectInfo() {
            try {
                const project = await window.APIClient.getProject(workflowManager.projectId);
                document.getElementById('projectTitle').textContent = project.name + ' - Stories & Prompts';
                document.getElementById('projectDescription').textContent = project.description;
            } catch (error) {
                console.error('Error loading project info:', error);
            }
        }

        async function loadApprovedStories() {
            try {
                // For now, use mock data - replace with actual API call
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
                    {
                        id: 2,
                        title: "User Login",
                        userType: "registered user",
                        wantStatement: "log into my account",
                        soThatStatement: "I can access my personal dashboard",
                        acceptanceCriteria: [
                            "User can enter credentials",
                            "Credentials are validated",
                            "User is redirected to dashboard",
                            "Session is established",
                            "Remember me option works"
                        ],
                        priority: "High",
                        storyPoints: 3
                    },
                    {
                        id: 3,
                        title: "Product Catalog",
                        userType: "customer",
                        wantStatement: "browse available products",
                        soThatStatement: "I can find items I want to purchase",
                        acceptanceCriteria: [
                            "Products are displayed in grid",
                            "Search functionality works",
                            "Filter by category works",
                            "Product details are visible",
                            "Images load properly"
                        ],
                        priority: "Medium",
                        storyPoints: 8
                    }
                ];

                workflowManager.state.approvedStories = mockStories;
                workflowManager.saveState();
            } catch (error) {
                console.error('Error loading approved stories:', error);
            }
        }

        async function refreshAllStatus() {
            // Check prompt approval status for all stories
            await workflowManager.checkStoryPromptApprovals();
            
            // Update UI
            workflowManager.updateStoryPromptUI();
        }

        function updateSummaryStats() {
            const stories = workflowManager.state.approvedStories;
            const prompts = workflowManager.state.storyPrompts;
            
            document.getElementById('totalStories').textContent = stories.length;
            
            let generated = 0, approved = 0, pending = 0;
            
            Object.values(prompts).forEach(prompt => {
                if (prompt.promptId) generated++;
                if (prompt.approved) approved++;
                if (prompt.pending) pending++;
            });
            
            document.getElementById('promptsGenerated').textContent = generated;
            document.getElementById('promptsApproved').textContent = approved;
            document.getElementById('pendingReviews').textContent = pending;
            document.getElementById('queueCount').textContent = pending;

            // Enable continue button if all prompts are approved
            const continueBtn = document.getElementById('continueToCodeBtn');
            continueBtn.disabled = approved < stories.length;
        }

        function renderStoriesGrid() {
            const grid = document.getElementById('storiesGrid');
            const stories = workflowManager.state.approvedStories;
            
            grid.innerHTML = stories.map((story, index) => {
                const promptStatus = workflowManager.getStoryPromptStatus(index);
                const isSelected = selectedStories.has(index);
                
                return `
                    <div class="story-card ${isSelected ? 'selected' : ''}" data-story-index="${index}">
                        <div class="story-card-header">
                            <div class="story-select">
                                <input type="checkbox" id="select-${index}" 
                                       ${isSelected ? 'checked' : ''}
                                       onchange="toggleStorySelection(${index})">
                            </div>
                            <div class="story-title">
                                <h4>${story.title}</h4>
                                <div class="story-meta">
                                    <span class="priority priority-${story.priority.toLowerCase()}">${story.priority}</span>
                                    <span class="story-points">${story.storyPoints} pts</span>
                                </div>
                            </div>
                            <div class="prompt-status prompt-status-${promptStatus.toLowerCase().replace(' ', '-')}">
                                ${promptStatus}
                            </div>
                        </div>
                        
                        <div class="story-card-body">
                            <div class="user-story">
                                <p><strong>As a</strong> ${story.userType}</p>
                                <p><strong>I want</strong> ${story.wantStatement}</p>
                                <p><strong>So that</strong> ${story.soThatStatement}</p>
                            </div>
                            
                            <div class="acceptance-criteria">
                                <h5>Acceptance Criteria:</h5>
                                <ul>
                                    ${story.acceptanceCriteria.map(criteria => `<li>${criteria}</li>`).join('')}
                                </ul>
                            </div>
                        </div>
                        
                        <div class="story-card-actions">
                            ${renderStoryActions(index, promptStatus)}
                        </div>
                    </div>
                `;
            }).join('');
        }

        function renderStoryActions(storyIndex, promptStatus) {
            switch (promptStatus) {
                case 'Not Started':
                    return `
                        <button onclick="generateStoryPrompt(${storyIndex})" class="btn-primary">
                            Generate Prompt
                        </button>
                    `;
                case 'Pending Review':
                    return `
                        <button disabled class="btn-disabled">
                            Generating...
                        </button>
                        <button onclick="window.location.href='../reviews/queue.html'" class="btn-secondary">
                            Check Review Queue
                        </button>
                    `;
                case 'Approved':
                    return `
                        <button onclick="viewPrompt(${storyIndex})" class="btn-primary">
                            View Prompt
                        </button>
                        <button onclick="downloadPrompt(${storyIndex})" class="btn-secondary">
                            Download
                        </button>
                        <button onclick="regeneratePrompt(${storyIndex})" class="btn-tertiary">
                            Regenerate
                        </button>
                    `;
                case 'Rejected':
                    return `
                        <button onclick="generateStoryPrompt(${storyIndex})" class="btn-primary">
                            Retry Generation
                        </button>
                    `;
                default:
                    return '';
            }
        }

        // Story Selection Management
        function toggleStorySelection(storyIndex) {
            if (selectedStories.has(storyIndex)) {
                selectedStories.delete(storyIndex);
            } else {
                selectedStories.add(storyIndex);
            }
            
            updateSelectionUI();
            renderStoriesGrid();
        }

        function toggleSelectAll() {
            const stories = workflowManager.state.approvedStories;
            const allSelected = selectedStories.size === stories.length;
            
            if (allSelected) {
                selectedStories.clear();
                document.getElementById('selectAllBtn').textContent = 'Select All';
            } else {
                selectedStories.clear();
                stories.forEach((_, index) => selectedStories.add(index));
                document.getElementById('selectAllBtn').textContent = 'Select None';
            }
            
            updateSelectionUI();
            renderStoriesGrid();
        }

        function updateSelectionUI() {
            const generateBtn = document.getElementById('generateSelectedBtn');
            generateBtn.disabled = selectedStories.size === 0;
            generateBtn.textContent = `Generate Selected Prompts (${selectedStories.size})`;
        }

        // Prompt Generation Functions
        async function generateStoryPrompt(storyIndex) {
            try {
                const canGenerate = await window.APIClient.canGeneratePrompt(
                    workflowManager.state.storyGenerationId, 
                    storyIndex
                );
                
                if (!canGenerate) {
                    alert('Cannot generate prompt. Stories must be approved first.');
                    return;
                }
                
                const preferences = {
                    framework: 'ASP.NET Core',
                    testingFramework: 'xUnit',
                    architecture: 'Clean Architecture'
                };
                
                const response = await window.APIClient.generatePrompt(
                    workflowManager.state.storyGenerationId,
                    storyIndex, 
                    preferences
                );
                
                workflowManager.setStoryPromptId(storyIndex, response.promptId);
                
                updateSummaryStats();
                renderStoriesGrid();
                
                alert(`Prompt generation started for "${workflowManager.state.approvedStories[storyIndex].title}". Check the review queue.`);
                
            } catch (error) {
                alert('Error generating prompt: ' + error.message);
                console.error('Prompt generation error:', error);
            }
        }

        async function generateSelectedPrompts() {
            if (selectedStories.size === 0) {
                alert('Please select at least one story.');
                return;
            }

            const confirmed = confirm(`Generate prompts for ${selectedStories.size} selected stories?`);
            if (!confirmed) return;

            for (const storyIndex of selectedStories) {
                const status = workflowManager.getStoryPromptStatus(storyIndex);
                if (status === 'Not Started' || status === 'Rejected') {
                    await generateStoryPrompt(storyIndex);
                    // Small delay to prevent overwhelming the system
                    await new Promise(resolve => setTimeout(resolve, 1000));
                }
            }

            selectedStories.clear();
            updateSelectionUI();
            renderStoriesGrid();
        }

        // Prompt Viewing Functions
        async function viewPrompt(storyIndex) {
            const prompt = workflowManager.state.storyPrompts[storyIndex];
            if (prompt && prompt.promptId) {
                try {
                    const promptData = await window.APIClient.getPrompt(prompt.promptId);
                    currentPromptData = promptData;
                    
                    document.getElementById('promptModalTitle').textContent = 
                        `Prompt: ${workflowManager.state.approvedStories[storyIndex].title}`;
                    document.getElementById('promptContent').textContent = promptData.generatedPrompt;
                    document.getElementById('promptModal').style.display = 'flex';
                    
                } catch (error) {
                    alert('Error loading prompt: ' + error.message);
                }
            }
        }

        function closePromptModal() {
            document.getElementById('promptModal').style.display = 'none';
            currentPromptData = null;
        }

        function copyPromptToClipboard() {
            if (currentPromptData) {
                navigator.clipboard.writeText(currentPromptData.generatedPrompt).then(() => {
                    alert('Prompt copied to clipboard!');
                });
            }
        }

        function downloadCurrentPrompt() {
            if (currentPromptData) {
                const blob = new Blob([currentPromptData.generatedPrompt], { type: 'text/markdown' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `prompt-${currentPromptData.promptId}.md`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            }
        }

        async function downloadPrompt(storyIndex) {
            const prompt = workflowManager.state.storyPrompts[storyIndex];
            if (prompt && prompt.promptId) {
                try {
                    const promptData = await window.APIClient.getPrompt(prompt.promptId);
                    const story = workflowManager.state.approvedStories[storyIndex];
                    
                    const blob = new Blob([promptData.generatedPrompt], { type: 'text/markdown' });
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = `story-${storyIndex + 1}-${story.title.replace(/\s+/g, '-').toLowerCase()}.md`;
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    URL.revokeObjectURL(url);
                    
                } catch (error) {
                    alert('Error downloading prompt: ' + error.message);
                }
            }
        }

        async function downloadApprovedPrompts() {
            const approvedPrompts = [];
            
            for (const storyIndex in workflowManager.state.storyPrompts) {
                const prompt = workflowManager.state.storyPrompts[storyIndex];
                if (prompt.approved && prompt.promptId) {
                    try {
                        const promptData = await window.APIClient.getPrompt(prompt.promptId);
                        const story = workflowManager.state.approvedStories[storyIndex];
                        approvedPrompts.push({
                            storyIndex: parseInt(storyIndex),
                            story: story,
                            prompt: promptData.generatedPrompt
                        });
                    } catch (error) {
                        console.error(`Error loading prompt ${prompt.promptId}:`, error);
                    }
                }
            }
            
            if (approvedPrompts.length === 0) {
                alert('No approved prompts available for download.');
                return;
            }
            
            // Download each prompt as individual file
            approvedPrompts.forEach(({storyIndex, story, prompt}) => {
                const blob = new Blob([prompt], { type: 'text/markdown' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `story-${storyIndex + 1}-${story.title.replace(/\s+/g, '-').toLowerCase()}.md`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            });
            
            alert(`Downloaded ${approvedPrompts.length} approved prompts.`);
        }

        async function regeneratePrompt(storyIndex) {
            const confirmed = confirm('Are you sure you want to regenerate this prompt? The current prompt will be replaced.');
            if (confirmed) {
                await generateStoryPrompt(storyIndex);
            }
        }

        // Utility Functions
        async function refreshStatus() {
            await refreshAllStatus();
            updateSummaryStats();
            renderStoriesGrid();
        }

        function goBackToWorkflow() {
            window.location.href = `workflow.html?projectId=${workflowManager.projectId}`;
        }

        function continueToCodeGeneration() {
            // TODO: Implement code generation workflow
            alert('Code generation workflow will be implemented in future release.');
        }

        // Modal click-outside-to-close
        document.getElementById('promptModal').addEventListener('click', function(e) {
            if (e.target === this) {
                closePromptModal();
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                closePromptModal();
            }
        });
    </script>
</body>
</html>
```

### 2. Add CSS Styles for Stories Overview

**File**: `frontend/css/styles.css` (append these styles)

```css
/* Stories Overview Page Styles */
.page-header {
    margin-bottom: 30px;
}

.breadcrumb {
    font-size: 14px;
    color: #666;
    margin-bottom: 10px;
}

.breadcrumb a {
    color: #007bff;
    text-decoration: none;
}

.breadcrumb a:hover {
    text-decoration: underline;
}

.stories-summary {
    background: #f8f9fa;
    border-radius: 8px;
    padding: 20px;
    margin-bottom: 30px;
}

.summary-stats {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 20px;
}

.stat-card {
    background: white;
    border-radius: 6px;
    padding: 20px;
    text-align: center;
    border: 1px solid #dee2e6;
}

.stat-number {
    font-size: 2rem;
    font-weight: bold;
    color: #007bff;
    margin-bottom: 5px;
}

.stat-label {
    font-size: 14px;
    color: #666;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

/* Bulk Actions */
.bulk-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    padding: 15px;
    background: #f8f9fa;
    border-radius: 6px;
    border: 1px solid #dee2e6;
}

.actions-left, .actions-right {
    display: flex;
    gap: 10px;
}

.bulk-actions button {
    padding: 8px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
}

.bulk-actions button:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

/* Stories Grid */
.stories-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
    gap: 20px;
    margin-bottom: 30px;
}

.story-card {
    border: 1px solid #dee2e6;
    border-radius: 8px;
    background: white;
    overflow: hidden;
    transition: all 0.2s;
}

.story-card:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    transform: translateY(-2px);
}

.story-card.selected {
    border-color: #007bff;
    box-shadow: 0 0 0 2px rgba(0,123,255,0.25);
}

.story-card-header {
    display: flex;
    align-items: center;
    padding: 15px;
    background: #f8f9fa;
    border-bottom: 1px solid #dee2e6;
    gap: 15px;
}

.story-select input[type="checkbox"] {
    transform: scale(1.2);
}

.story-title h4 {
    margin: 0 0 5px 0;
    font-size: 16px;
    color: #333;
}

.story-meta {
    display: flex;
    gap: 10px;
    align-items: center;
}

.priority {
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 12px;
    font-weight: bold;
    text-transform: uppercase;
}

.priority-high {
    background: #fff5f5;
    color: #e53e3e;
    border: 1px solid #fed7d7;
}

.priority-medium {
    background: #fffbeb;
    color: #d69e2e;
    border: 1px solid #feebc8;
}

.priority-low {
    background: #f0fff4;
    color: #38a169;
    border: 1px solid #c6f6d5;
}

.story-points {
    background: #edf2f7;
    color: #4a5568;
    padding: 2px 8px;
    border-radius: 4px;
    font-size: 12px;
}

.prompt-status {
    margin-left: auto;
    padding: 6px 12px;
    border-radius: 4px;
    font-size: 12px;
    font-weight: bold;
}

.prompt-status-not-started {
    background: #f7fafc;
    color: #718096;
    border: 1px solid #e2e8f0;
}

.prompt-status-pending-review {
    background: #fffbeb;
    color: #d69e2e;
    border: 1px solid #feebc8;
}

.prompt-status-approved {
    background: #f0fff4;
    color: #38a169;
    border: 1px solid #c6f6d5;
}

.prompt-status-rejected {
    background: #fff5f5;
    color: #e53e3e;
    border: 1px solid #fed7d7;
}

.story-card-body {
    padding: 15px;
}

.user-story p {
    margin: 5px 0;
    font-size: 14px;
    line-height: 1.4;
}

.acceptance-criteria {
    margin-top: 15px;
    padding-top: 15px;
    border-top: 1px solid #eee;
}

.acceptance-criteria h5 {
    margin: 0 0 10px 0;
    font-size: 14px;
    color: #4a5568;
}

.acceptance-criteria ul {
    margin: 0;
    padding-left: 20px;
}

.acceptance-criteria li {
    font-size: 13px;
    line-height: 1.4;
    margin-bottom: 5px;
    color: #666;
}

.story-card-actions {
    padding: 15px;
    background: #f8f9fa;
    border-top: 1px solid #dee2e6;
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
}

.story-card-actions button {
    padding: 8px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 13px;
    font-weight: 500;
}

.btn-primary {
    background: #007bff;
    color: white;
}

.btn-primary:hover {
    background: #0056b3;
}

.btn-secondary {
    background: #6c757d;
    color: white;
}

.btn-secondary:hover {
    background: #545b62;
}

.btn-tertiary {
    background: #e9ecef;
    color: #495057;
    border: 1px solid #ced4da;
}

.btn-tertiary:hover {
    background: #dee2e6;
}

.btn-disabled {
    background: #e9ecef;
    color: #6c757d;
    cursor: not-allowed;
}

/* Page Footer */
.page-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 0;
    border-top: 1px solid #dee2e6;
}

.secondary-btn {
    background: #6c757d;
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 4px;
    cursor: pointer;
}

.primary-btn {
    background: #28a745;
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 4px;
    cursor: pointer;
}

.primary-btn:disabled {
    background: #6c757d;
    cursor: not-allowed;
}

/* Prompt Modal */
.prompt-modal {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0,0,0,0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
}

.prompt-modal-content {
    background: white;
    border-radius: 8px;
    width: 90%;
    max
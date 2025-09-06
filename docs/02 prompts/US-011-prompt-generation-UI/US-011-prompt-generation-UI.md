# Frontend Phase 4 Implementation: Multi-Prompt Generation Interface

## **Implementation Strategy**

### **Design Philosophy: Individual Story Cards with Progressive Disclosure**
Rather than overwhelming users with 14 simultaneous operations, implement a **progressive disclosure interface** where users can:
- View all stories in a clean card layout
- Generate prompts on-demand per story
- Track individual progress states
- Access generated prompts when ready

## **Comprehensive Implementation Plan**

### **1. Enhanced API Client Extensions**

**File**: `frontend/js/api.js`

Add these methods to the existing `window.APIClient`:

```javascript
// Phase 4: Prompt Generation API methods
async generatePrompt(storyGenerationId, storyIndex, preferences = {}) {
    return await this.post('/prompts/generate', {
        storyGenerationId,
        storyIndex, 
        technicalPreferences: preferences
    });
},

async getPromptStatus(promptId) {
    return await this.get(`/prompts/${promptId}/status`);
},

async canGeneratePrompt(storyGenerationId, storyIndex) {
    return await this.get(`/prompts/can-generate/${storyGenerationId}/${storyIndex}`);
},

async getPrompt(promptId) {
    return await this.get(`/prompts/${promptId}`);
},

// Retrieve approved stories for individual prompt generation
async getApprovedStories(storyGenerationId) {
    return await this.get(`/stories/${storyGenerationId}/approved`);
}
```

### **2. Enhanced Workflow Manager**

**File**: `frontend/js/workflow.js`

Extend the `WorkflowManager` class with story-level tracking:

```javascript
class WorkflowManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.storageKey = `workflow_${projectId}`;
        this.state = this.loadState() || {
            // Existing state properties...
            
            // Phase 4: Story-level prompt tracking
            storyPrompts: {}, // { storyIndex: { promptId, status, pending } }
            approvedStories: [] // Cache of approved story data
        };
    }
    
    // Phase 4: Story-level prompt management
    setStoryPromptId(storyIndex, promptId) {
        if (!this.state.storyPrompts[storyIndex]) {
            this.state.storyPrompts[storyIndex] = {};
        }
        this.state.storyPrompts[storyIndex].promptId = promptId;
        this.state.storyPrompts[storyIndex].pending = true;
        this.saveState();
    }
    
    setStoryPromptApproved(storyIndex, approved) {
        if (this.state.storyPrompts[storyIndex]) {
            this.state.storyPrompts[storyIndex].approved = approved;
            this.state.storyPrompts[storyIndex].pending = false;
            this.saveState();
        }
    }
    
    getStoryPromptStatus(storyIndex) {
        const prompt = this.state.storyPrompts[storyIndex];
        if (!prompt) return 'Not Started';
        if (prompt.pending) return 'Pending Review';
        if (prompt.approved) return 'Approved';
        return 'Rejected';
    }
    
    // Check approval status for all story prompts
    async checkStoryPromptApprovals() {
        for (const storyIndex in this.state.storyPrompts) {
            const prompt = this.state.storyPrompts[storyIndex];
            if (prompt.pending && prompt.promptId) {
                try {
                    const review = await window.APIClient.getReview(prompt.promptId);
                    if (review.status === 'Approved') {
                        this.setStoryPromptApproved(storyIndex, true);
                    } else if (review.status === 'Rejected') {
                        this.setStoryPromptApproved(storyIndex, false);
                    }
                } catch (error) {
                    console.log(`No review found for prompt ${prompt.promptId}`);
                }
            }
        }
    }
    
    // Enhanced UI update for Phase 4
    updateWorkflowUI() {
        // Existing Stage 1-3 logic...
        
        // Phase 4: Update story prompt interface
        this.updateStoryPromptUI();
    }
    
    updateStoryPromptUI() {
        const storiesContainer = document.getElementById('storiesContainer');
        if (!storiesContainer) return;
        
        this.state.approvedStories.forEach((story, index) => {
            const storyCard = document.getElementById(`story-${index}`);
            const promptBtn = document.getElementById(`generatePrompt-${index}`);
            const statusDiv = document.getElementById(`promptStatus-${index}`);
            
            if (promptBtn && statusDiv) {
                const status = this.getStoryPromptStatus(index);
                statusDiv.textContent = status;
                statusDiv.className = `prompt-status ${status.toLowerCase().replace(' ', '-')}`;
                
                // Update button state
                if (status === 'Not Started') {
                    promptBtn.disabled = false;
                    promptBtn.textContent = 'Generate Prompt';
                } else if (status === 'Pending Review') {
                    promptBtn.disabled = true;
                    promptBtn.textContent = 'Generating...';
                } else if (status === 'Approved') {
                    promptBtn.disabled = false;
                    promptBtn.textContent = 'View Prompt';
                    promptBtn.onclick = () => this.viewPrompt(index);
                }
            }
        });
    }
    
    async viewPrompt(storyIndex) {
        const prompt = this.state.storyPrompts[storyIndex];
        if (prompt && prompt.promptId) {
            try {
                const promptData = await window.APIClient.getPrompt(prompt.promptId);
                this.displayPromptModal(promptData);
            } catch (error) {
                alert('Error loading prompt: ' + error.message);
            }
        }
    }
    
    displayPromptModal(promptData) {
        // Create modal overlay for prompt display
        const modal = document.createElement('div');
        modal.className = 'prompt-modal';
        modal.innerHTML = `
            <div class="prompt-modal-content">
                <div class="prompt-header">
                    <h2>Generated Coding Prompt</h2>
                    <button onclick="this.closest('.prompt-modal').remove()">×</button>
                </div>
                <div class="prompt-body">
                    <pre class="prompt-content">${promptData.generatedPrompt}</pre>
                </div>
                <div class="prompt-actions">
                    <button onclick="navigator.clipboard.writeText('${promptData.generatedPrompt.replace(/'/g, "\\'")}')">
                        Copy to Clipboard
                    </button>
                    <button onclick="this.downloadPrompt('${promptData.generatedPrompt}', 'prompt-${promptData.promptId}.md')">
                        Download
                    </button>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    }
}
```

### **3. Enhanced Workflow Page**

**File**: `frontend/projects/workflow.html`

Add Phase 4 section after the existing stages:

```html
<!-- Existing Stage 1-3 sections... -->

<!-- Stage 4: Prompt Generation -->
<div class="workflow-stage" id="promptStage">
    <h3>Prompt Generation</h3>
    <div class="stage-status" id="promptStageStatus">Not Started</div>
    
    <!-- Stories Container for Individual Prompt Generation -->
    <div id="storiesContainer" class="stories-container" style="display: none;">
        <h4>Generate Prompts for Individual Stories</h4>
        <div id="storiesList" class="stories-list">
            <!-- Story cards populated dynamically -->
        </div>
        <div class="prompt-actions">
            <button id="generateAllPromptsBtn" onclick="generateAllPrompts()">
                Generate All Remaining Prompts
            </button>
            <button id="downloadAllPromptsBtn" onclick="downloadAllPrompts()">
                Download All Approved Prompts
            </button>
        </div>
    </div>
</div>

<script>
// Add to existing workflow.js functions:

// Enhanced initialization to load approved stories
window.addEventListener('DOMContentLoaded', async function() {
    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('projectId');
    
    workflowManager = new WorkflowManager(projectId);
    workflowManager.loadState();
    await workflowManager.checkApprovedReviews();
    await workflowManager.checkStoryPromptApprovals();
    
    // Load approved stories if available
    if (workflowManager.state.storiesApproved && workflowManager.state.storyGenerationId) {
        await loadApprovedStories();
    }
    
    workflowManager.updateWorkflowUI();
});

async function loadApprovedStories() {
    try {
        const stories = await window.APIClient.getApprovedStories(workflowManager.state.storyGenerationId);
        workflowManager.state.approvedStories = stories;
        workflowManager.saveState();
        displayStoriesForPromptGeneration(stories);
    } catch (error) {
        console.error('Error loading approved stories:', error);
    }
}

function displayStoriesForPromptGeneration(stories) {
    const storiesContainer = document.getElementById('storiesContainer');
    const storiesList = document.getElementById('storiesList');
    
    if (!storiesContainer || !storiesList) return;
    
    storiesContainer.style.display = 'block';
    
    storiesList.innerHTML = stories.map((story, index) => `
        <div class="story-card" id="story-${index}">
            <div class="story-header">
                <h5>Story ${index + 1}: ${story.title}</h5>
                <div class="prompt-status" id="promptStatus-${index}">Not Started</div>
            </div>
            <div class="story-content">
                <p><strong>As a</strong> ${story.userType}</p>
                <p><strong>I want</strong> ${story.wantStatement}</p>
                <p><strong>So that</strong> ${story.soThatStatement}</p>
            </div>
            <div class="story-actions">
                <button id="generatePrompt-${index}" 
                        onclick="generateStoryPrompt(${index})"
                        class="generate-prompt-btn">
                    Generate Prompt
                </button>
            </div>
        </div>
    `).join('');
    
    // Update UI states for existing prompts
    workflowManager.updateStoryPromptUI();
}

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
        
        const preferences = getPromptPreferences(); // Get from form if implemented
        const request = {
            storyGenerationId: workflowManager.state.storyGenerationId,
            storyIndex: storyIndex,
            technicalPreferences: preferences
        };
        
        const response = await window.APIClient.generatePrompt(
            request.storyGenerationId,
            request.storyIndex, 
            request.technicalPreferences
        );
        
        workflowManager.setStoryPromptId(storyIndex, response.promptId);
        workflowManager.updateStoryPromptUI();
        
        alert(`Prompt generation started for Story ${storyIndex + 1}. Check the review queue.`);
    } catch (error) {
        alert('Error generating prompt: ' + error.message);
    }
}

function getPromptPreferences() {
    // Basic implementation - can be enhanced with form inputs
    return {
        framework: 'ASP.NET Core',
        testingFramework: 'xUnit',
        architecture: 'Clean Architecture'
    };
}

async function generateAllPrompts() {
    const unprocessedStories = workflowManager.state.approvedStories
        .map((story, index) => ({ story, index }))
        .filter(({index}) => workflowManager.getStoryPromptStatus(index) === 'Not Started');
    
    if (unprocessedStories.length === 0) {
        alert('All prompts have already been generated.');
        return;
    }
    
    const confirmed = confirm(`Generate prompts for ${unprocessedStories.length} remaining stories?`);
    if (!confirmed) return;
    
    for (const {index} of unprocessedStories) {
        await generateStoryPrompt(index);
        // Small delay to prevent overwhelming the system
        await new Promise(resolve => setTimeout(resolve, 1000));
    }
}

async function downloadAllPrompts() {
    const approvedPrompts = [];
    
    for (const storyIndex in workflowManager.state.storyPrompts) {
        const prompt = workflowManager.state.storyPrompts[storyIndex];
        if (prompt.approved && prompt.promptId) {
            try {
                const promptData = await window.APIClient.getPrompt(prompt.promptId);
                approvedPrompts.push({
                    storyIndex: parseInt(storyIndex),
                    story: workflowManager.state.approvedStories[storyIndex],
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
    
    downloadPromptsAsZip(approvedPrompts);
}

function downloadPromptsAsZip(prompts) {
    // Simple implementation - creates individual files
    prompts.forEach(({storyIndex, story, prompt}) => {
        const blob = new Blob([prompt], { type: 'text/markdown' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `story-${storyIndex + 1}-prompt.md`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    });
}
</script>
```

### **4. Enhanced CSS Styling**

**File**: `frontend/css/styles.css`

Add styles for the new Phase 4 components:

```css
/* Phase 4: Prompt Generation Styles */
.stories-container {
    margin-top: 20px;
    padding: 20px;
    border: 1px solid #ddd;
    border-radius: 8px;
    background-color: #f9f9f9;
}

.stories-list {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
    gap: 20px;
    margin: 20px 0;
}

.story-card {
    border: 1px solid #ccc;
    border-radius: 8px;
    padding: 15px;
    background-color: white;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.story-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
    border-bottom: 1px solid #eee;
    padding-bottom: 8px;
}

.story-header h5 {
    margin: 0;
    color: #333;
    font-size: 14px;
}

.prompt-status {
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 12px;
    font-weight: bold;
}

.prompt-status.not-started {
    background-color: #f0f0f0;
    color: #666;
}

.prompt-status.pending-review {
    background-color: #fff3cd;
    color: #856404;
}

.prompt-status.approved {
    background-color: #d4edda;
    color: #155724;
}

.prompt-status.rejected {
    background-color: #f8d7da;
    color: #721c24;
}

.story-content {
    margin: 10px 0;
    font-size: 13px;
}

.story-content p {
    margin: 5px 0;
}

.story-actions {
    margin-top: 10px;
}

.generate-prompt-btn {
    background-color: #007bff;
    color: white;
    border: none;
    padding: 8px 16px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 12px;
}

.generate-prompt-btn:hover:not(:disabled) {
    background-color: #0056b3;
}

.generate-prompt-btn:disabled {
    background-color: #6c757d;
    cursor: not-allowed;
}

.prompt-actions {
    margin-top: 20px;
    display: flex;
    gap: 10px;
}

.prompt-actions button {
    padding: 10px 20px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: bold;
}

#generateAllPromptsBtn {
    background-color: #28a745;
    color: white;
}

#downloadAllPromptsBtn {
    background-color: #17a2b8;
    color: white;
}

/* Prompt Modal Styles */
.prompt-modal {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0,0,0,0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
}

.prompt-modal-content {
    background-color: white;
    border-radius: 8px;
    width: 90%;
    max-width: 800px;
    max-height: 80%;
    display: flex;
    flex-direction: column;
}

.prompt-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px;
    border-bottom: 1px solid #ddd;
}

.prompt-header button {
    background: none;
    border: none;
    font-size: 24px;
    cursor: pointer;
}

.prompt-body {
    flex: 1;
    overflow-y: auto;
    padding: 20px;
}

.prompt-content {
    white-space: pre-wrap;
    font-family: 'Courier New', monospace;
    font-size: 14px;
    line-height: 1.4;
    background-color: #f8f9fa;
    padding: 15px;
    border-radius: 4px;
    border: 1px solid #e9ecef;
}

.prompt-actions {
    padding: 20px;
    border-top: 1px solid #ddd;
    display: flex;
    gap: 10px;
}

.prompt-actions button {
    padding: 10px 20px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    background-color: #007bff;
    color: white;
}
```

## **Implementation Priority & Success Criteria**

### **Phase 1: Core Functionality** (Implement First)
1. **API Client extensions** for prompt generation endpoints
2. **Basic WorkflowManager** story-level tracking 
3. **Story card display** with individual prompt generation buttons
4. **Basic status tracking** and UI updates

### **Phase 2: Enhanced UX** (Implement Second)
1. **Prompt viewing modal** with copy/download functionality
2. **Batch operations** (generate all, download all)
3. **Enhanced styling** and visual polish
4. **Progress indicators** and loading states

### **Success Criteria**
- Users can see approved stories as individual cards
- Each story has independent prompt generation capability
- Status tracking works across browser sessions
- Generated prompts are viewable and downloadable
- Review queue properly handles prompt reviews
- Workflow maintains state consistency

This implementation provides a sophisticated interface for managing multiple concurrent prompt generations while maintaining the established frontend architecture patterns.
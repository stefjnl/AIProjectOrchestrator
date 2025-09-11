# Prompt Viewer Implementation Plan

## Executive Summary
Comprehensive architectural plan for implementing enhanced prompt viewing, analysis, editing, and copying functionality for the AI-powered project orchestration platform.

## Current State Analysis

### Frontend Flow (Completed Analysis)
- **Story Cards**: Generate Prompt button appears for approved stories without prompts
- **Story Modal**: Contains "Generate Prompt" button in footer (line 128-129 in Overview.cshtml)
- **State Management**: Tracks `story.hasPrompt` and `story.promptId` properties
- **Backend Integration**: Uses existing [`GetPrompt`](src/AIProjectOrchestrator.API/Controllers/PromptGenerationController.cs:114) endpoint

### Key Technical Assets
- ✅ Backend API: [`PromptGenerationController.GetPrompt()`](src/AIProjectOrchestrator.API/Controllers/PromptGenerationController.cs:114)
- ✅ Frontend State: [`story.hasPrompt`](src/AIProjectOrchestrator.API/wwwroot/js/stories-overview.js:386) boolean flag
- ✅ Modal System: Established CSS class-based modal system
- ✅ Status Utilities: [`window.StatusUtils.canGeneratePrompt()`](src/AIProjectOrchestrator.API/wwwroot/js/stories-overview.js:86)

## Enhanced UI/UX Design

### 1. Button State Management Strategy

```javascript
// Enhanced story card button logic (replacement for lines 123-127)
${canGeneratePrompt ? `
    <button class="btn btn-sm btn-primary" onclick="window.storiesOverviewManager.generatePromptForStory('${story.id}', ${index})">
        🤖 Generate Prompt
    </button>
` : ''}
${story.hasPrompt ? `
    <button class="btn btn-sm btn-info" onclick="window.storiesOverviewManager.viewPrompt('${story.promptId}')">
        👁️ View Prompt
    </button>
` : ''}
```

### 2. New Prompt Viewer Modal Architecture

#### HTML Structure (Add to Overview.cshtml after line 132)
```html
<div class="modal" id="prompt-viewer-modal" style="display: none;">
    <div class="modal-content prompt-viewer">
        <div class="modal-header">
            <h3 id="modal-prompt-title">Generated Prompt</h3>
            <div class="modal-header-actions">
                <button class="btn btn-sm btn-secondary" onclick="copyPrompt()">📋 Copy</button>
                <button class="btn btn-sm btn-primary" onclick="editPrompt()">✏️ Edit</button>
                <button class="modal-close" onclick="closePromptModal()">&times;</button>
            </div>
        </div>
        <div class="modal-body">
            <div class="prompt-metadata">
                <div class="metadata-item">
                    <span class="label">Story:</span>
                    <span class="value" id="modal-prompt-story-title"></span>
                </div>
                <div class="metadata-item">
                    <span class="label">Generated:</span>
                    <span class="value" id="modal-prompt-date"></span>
                </div>
                <div class="metadata-item">
                    <span class="label">Quality Score:</span>
                    <span class="value" id="modal-prompt-quality"></span>
                </div>
            </div>
            <div class="prompt-content-container">
                <div class="prompt-toolbar">
                    <button class="btn btn-sm" onclick="toggleSyntaxHighlighting()">🔤 Syntax</button>
                    <button class="btn btn-sm" onclick="toggleLineNumbers()">#️⃣ Lines</button>
                    <button class="btn btn-sm" onclick="toggleFullscreen()">⛶ Fullscreen</button>
                </div>
                <pre class="prompt-content" id="modal-prompt-content"></pre>
            </div>
        </div>
        <div class="modal-footer">
            <button class="btn btn-secondary" onclick="closePromptModal()">Close</button>
            <button class="btn btn-primary" onclick="copyPrompt()">📋 Copy to Clipboard</button>
            <button class="btn btn-success" onclick="exportPrompt()">📥 Export</button>
        </div>
    </div>
</div>
```

### 3. Enhanced JavaScript Implementation

#### New Methods for StoriesOverviewManager
```javascript
// Add to StoriesOverviewManager class (after line 705)
async viewPrompt(promptId) {
    if (!promptId) {
        window.App.showNotification('No prompt ID available.', 'error');
        return;
    }

    const loadingOverlay = showLoading('Loading prompt...');
    try {
        const response = await APIClient.getPrompt(promptId);
        if (!response || !response.generatedPrompt) {
            window.App.showNotification('Prompt not found or empty.', 'error');
            return;
        }

        this.showPromptModal(response);
    } catch (error) {
        console.error('Failed to load prompt:', error);
        window.App.showNotification('Failed to load prompt. Please try again.', 'error');
    } finally {
        hideLoading(loadingOverlay);
    }
}

showPromptModal(promptData) {
    const modal = document.getElementById('prompt-viewer-modal');
    if (!modal) {
        console.error('Prompt viewer modal not found');
        return;
    }

    // Populate modal content
    document.getElementById('modal-prompt-title').textContent = 
        `Generated Prompt - ${promptData.storyTitle || 'Untitled Story'}`;
    
    document.getElementById('modal-prompt-story-title').textContent = 
        promptData.storyTitle || 'Untitled Story';
    
    document.getElementById('modal-prompt-date').textContent = 
        new Date(promptData.createdAt).toLocaleString();
    
    document.getElementById('modal-prompt-quality').textContent = 
        this.calculateQualityScore(promptData.generatedPrompt);
    
    document.getElementById('modal-prompt-content').textContent = 
        promptData.generatedPrompt;

    // Show modal
    modal.classList.add('show');
    
    // Store current prompt for other operations
    this.currentPrompt = promptData;
}

calculateQualityScore(prompt) {
    // Simple quality scoring based on prompt characteristics
    let score = 0;
    if (prompt.includes('Context') || prompt.includes('Architecture')) score += 20;
    if (prompt.includes('Requirements') || prompt.includes('Deliverables')) score += 20;
    if (prompt.includes('Testing') || prompt.includes('Quality')) score += 15;
    if (prompt.length > 1000) score += 15;
    if (prompt.includes('Code') || prompt.includes('Implementation')) score += 10;
    return `${Math.min(score, 100)}%`;
}
```

### 4. Enhanced API Client Integration

#### New API Client Methods (Create js/prompt-api.js)
```javascript
class PromptAPIClient {
    static async getPrompt(promptId) {
        try {
            const response = await fetch(`/api/promptgeneration/${promptId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Failed to get prompt:', error);
            throw error;
        }
    }
}

// Extend existing APIClient
APIClient.getPrompt = PromptAPIClient.getPrompt;
```

### 5. Enhanced CSS Styling

```css
/* Add to styles.css */
.prompt-viewer {
    max-width: 90vw;
    max-height: 90vh;
    width: 1200px;
}

.prompt-viewer .modal-body {
    max-height: calc(90vh - 200px);
    overflow-y: auto;
}

.prompt-metadata {
    display: flex;
    gap: 20px;
    margin-bottom: 20px;
    padding: 15px;
    background: #f8f9fa;
    border-radius: 8px;
}

.prompt-content {
    background: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 6px;
    padding: 20px;
    font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
    font-size: 14px;
    line-height: 1.6;
    white-space: pre-wrap;
    word-wrap: break-word;
    max-height: 500px;
    overflow-y: auto;
}

.modal-header-actions {
    display: flex;
    gap: 10px;
    align-items: center;
}
```

## Implementation Strategy

### Phase 1: Core Functionality (Immediate)
1. **Add "View Prompt" button** to story cards with conditional rendering
2. **Create prompt viewer modal** with basic display capabilities
3. **Implement [`viewPrompt()`](src/AIProjectOrchestrator.API/wwwroot/js/stories-overview.js:706) method** with API integration
4. **Add [`GetPrompt`](src/AIProjectOrchestrator.API/Controllers/PromptGenerationController.cs:114) API client method**
5. **Implement copy functionality** with clipboard API

### Phase 2: Enhanced Features (Next Sprint)
1. **Inline editing capabilities** with contentEditable
2. **Syntax highlighting** for code sections
3. **Quality scoring algorithm** implementation
4. **Export functionality** (JSON, Markdown, Text)
5. **Responsive design** improvements

### Phase 3: Advanced Features (Future)
1. **Prompt analysis dashboard** with metrics
2. **Version history** and comparison
3. **AI-powered improvement suggestions**
4. **Integration with external editors** (VS Code)
5. **Collaborative editing** features

## Technical Requirements

### Backend Dependencies
- ✅ Existing [`GetPrompt`](src/AIProjectOrchestrator.API/Controllers/PromptGenerationController.cs:114) endpoint (already implemented)
- ✅ Prompt storage in database (already implemented)
- ✅ Response model with [`generatedPrompt`](src/AIProjectOrchestrator.API/Controllers/PromptGenerationController.cs:125) field

### Frontend Dependencies
- ✅ Modal system (already implemented)
- ✅ API client infrastructure (already implemented)
- ✅ Status utilities (already implemented)
- ✅ Clipboard API (modern browser support)

### Browser Requirements
- Modern browsers with ES6+ support
- Clipboard API compatibility
- CSS Grid/Flexbox support

## Success Metrics

### User Experience Metrics
- **Time to View Prompt**: < 2 seconds
- **Copy Success Rate**: > 95%
- **Modal Load Time**: < 1 second
- **Error Rate**: < 5%

### Technical Metrics
- **API Response Time**: < 500ms
- **Frontend Bundle Size**: < 50KB increase
- **Memory Usage**: < 10MB for large prompts
- **Accessibility Score**: > 95%

## Risk Mitigation

### Technical Risks
1. **Large Prompt Performance**: Implement virtual scrolling for prompts > 10KB
2. **Clipboard API Failures**: Fallback to manual selection/copy
3. **Modal Z-index Issues**: Use established modal stacking system
4. **Mobile Responsiveness**: Progressive enhancement approach

### User Experience Risks
1. **Cognitive Overload**: Progressive disclosure of advanced features
2. **Edit Confusion**: Clear visual indicators for edit mode
3. **Copy Feedback**: Immediate visual/audio feedback
4. **Error Recovery**: Graceful degradation with helpful messages

This implementation plan provides a comprehensive roadmap for creating an enterprise-grade prompt viewing and interaction system that enhances the user experience while maintaining system reliability and performance.
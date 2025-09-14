/**
 * ArtifactsPanel - Side panel for displaying generated artifacts
 * 
 * This service manages the collapsible side panel that displays all generated
 * artifacts (Requirements Analysis, Project Planning, User Stories, Generated Prompts)
 * for instant access without navigation disruption.
 */

class ArtifactsPanel {
    constructor(projectId) {
        this.projectId = projectId;
        this.isCollapsed = false;
        this.artifacts = {
            requirements: { status: 'Not Started', content: null, loading: false },
            planning: { status: 'Not Started', content: null, loading: false },
            stories: { status: 'Not Started', content: null, loading: false },
            prompts: { status: 'Not Started', content: null, loading: false }
        };
        
        this.panelElement = null;
        this.toggleButton = null;
        this.contentElement = null;
        
        console.log('ArtifactsPanel initialized for project:', projectId);
        
        this.initialize();
    }
    
    async initialize() {
        try {
            // Wait for DOM to be ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => this.setupPanel());
            } else {
                this.setupPanel();
            }
        } catch (error) {
            console.error('Failed to initialize ArtifactsPanel:', error);
        }
    }
    
    setupPanel() {
        // Get panel elements
        this.panelElement = document.getElementById('artifactsPanel');
        this.toggleButton = document.getElementById('panelToggle');
        this.contentElement = document.getElementById('panelContent');
        
        if (!this.panelElement || !this.toggleButton || !this.contentElement) {
            console.warn('ArtifactsPanel elements not found, skipping initialization');
            return;
        }
        
        // Set up event listeners
        this.setupEventListeners();
        
        // Load initial artifact data
        this.loadArtifactsData();
        
        // Set up state change observer if workflow manager is available
        if (window.workflowManager && window.workflowManager.stateManager) {
            this.setupWorkflowObserver();
        }
        
        console.log('ArtifactsPanel setup completed');
    }
    
    setupEventListeners() {
        // Panel toggle functionality
        this.toggleButton.addEventListener('click', () => this.togglePanel());
        
        // Artifact header click handlers for expansion
        const artifactHeaders = this.panelElement.querySelectorAll('.artifact-header');
        artifactHeaders.forEach(header => {
            header.addEventListener('click', (e) => {
                const artifactItem = header.closest('.artifact-item');
                const artifactType = artifactItem.dataset.artifact;
                this.toggleArtifactContent(artifactType);
            });
        });
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && !this.isCollapsed) {
                this.togglePanel();
            }
        });
    }
    
    setupWorkflowObserver() {
        // Subscribe to workflow state changes
        const unsubscribe = window.workflowManager.stateManager.subscribe((state) => {
            this.updateArtifactsFromWorkflow(state.workflow);
        }, 'workflowChanged');
        
        // Store unsubscribe function for cleanup
        this.unsubscribeWorkflow = unsubscribe;
    }
    
    async loadArtifactsData() {
        try {
            // Get current workflow state if available
            if (window.workflowManager && window.workflowManager.workflowState) {
                this.updateArtifactsFromWorkflow(window.workflowManager.workflowState);
                return;
            }
            
            // Fallback: load workflow status from API
            const workflowState = await APIClient.getWorkflowStatus(this.projectId);
            this.updateArtifactsFromWorkflow(workflowState);
        } catch (error) {
            console.warn('Failed to load artifacts data:', error);
            this.setAllArtifactsStatus('Unavailable');
        }
    }
    
    updateArtifactsFromWorkflow(workflowState) {
        if (!workflowState) return;
        
        console.log('updateArtifactsFromWorkflow received:', workflowState);
        
        // Update Requirements Analysis
        if (workflowState.requirementsAnalysis) {
            console.log('Requirements Analysis data:', workflowState.requirementsAnalysis);
            this.updateArtifactStatus('requirements', workflowState.requirementsAnalysis);
        }
        
        // Update Project Planning
        if (workflowState.projectPlanning) {
            console.log('Project Planning data:', workflowState.projectPlanning);
            this.updateArtifactStatus('planning', workflowState.projectPlanning);
        }
        
        // Update User Stories
        if (workflowState.storyGeneration) {
            console.log('Story Generation data:', workflowState.storyGeneration);
            this.updateArtifactStatus('stories', workflowState.storyGeneration);
        }
        
        // Update Prompts
        if (workflowState.promptGeneration) {
            console.log('Prompt Generation data:', workflowState.promptGeneration);
            this.updateArtifactStatus('prompts', workflowState.promptGeneration);
        }
    }
    
    updateArtifactStatus(artifactType, workflowData) {
        const statusElement = document.getElementById(`${artifactType}-status`);
        if (!statusElement) return;
        
        let status = 'Not Started';
        let statusClass = '';
        
        if (workflowData.isApproved) {
            status = 'Approved';
            statusClass = 'approved';
        } else if (workflowData.status === 'PendingReview') {
            status = 'Pending Review';
            statusClass = 'pending';
        } else if (workflowData.status === 'Processing') {
            status = 'Processing';
            statusClass = 'pending';
        } else if (workflowData.status === 'Rejected') {
            status = 'Rejected';
            statusClass = 'rejected';
        } else if (workflowData.status === 'Completed') {
            status = 'Completed';
            statusClass = 'approved';
        }
        
        statusElement.textContent = status;
        statusElement.className = `artifact-status ${statusClass}`;
        
        // Store status for content loading
        this.artifacts[artifactType].status = status;
        
        // Auto-load content if approved and not already loaded
        if (workflowData.isApproved && !this.artifacts[artifactType].content) {
            this.loadArtifactContent(artifactType, workflowData);
        }
    }
    
    setAllArtifactsStatus(status) {
        Object.keys(this.artifacts).forEach(type => {
            const statusElement = document.getElementById(`${type}-status`);
            if (statusElement) {
                statusElement.textContent = status;
                statusElement.className = 'artifact-status';
            }
            this.artifacts[type].status = status;
        });
    }
    
    async loadArtifactContent(artifactType, workflowData) {
        if (this.artifacts[artifactType].loading || this.artifacts[artifactType].content) {
            return; // Already loading or loaded
        }
        
        this.artifacts[artifactType].loading = true;
        this.showLoadingState(artifactType, true);
        
        try {
            let content = null;
            
            switch (artifactType) {
                case 'requirements':
                    if (workflowData.analysisId) {
                        console.log(`Loading requirements for analysisId: ${workflowData.analysisId}`);
                        const result = await APIClient.getRequirements(workflowData.analysisId);
                        console.log('Requirements API result:', result);
                        content = this.formatRequirementsContent(result);
                    }
                    break;
                    
                case 'planning':
                    if (workflowData.planningId) {
                        console.log(`Loading planning for planningId: ${workflowData.planningId}`);
                        const result = await APIClient.getProjectPlan(workflowData.planningId);
                        console.log('Planning API result:', result);
                        content = this.formatPlanningContent(result);
                    }
                    break;
                    
                case 'stories':
                    if (workflowData.generationId) {
                        console.log(`Loading stories for generationId: ${workflowData.generationId}`);
                        const result = await APIClient.getApprovedStories(workflowData.generationId);
                        console.log('Stories API result:', result);
                        content = this.formatStoriesContent(result);
                    }
                    break;
                    
                case 'prompts':
                    // Prompts would need a different approach - for now show placeholder
                    content = this.formatPromptsContent(workflowData);
                    break;
            }
            
            this.artifacts[artifactType].content = content;
            this.updateArtifactDetails(artifactType, content);
            
        } catch (error) {
            console.error(`Failed to load ${artifactType} content:`, error);
            this.showArtifactError(artifactType, `Failed to load content: ${error.message}`);
        } finally {
            this.artifacts[artifactType].loading = false;
            this.showLoadingState(artifactType, false);
        }
    }
    
    formatRequirementsContent(data) {
        console.log('formatRequirementsContent received data:', data);
        if (!data) return 'No requirements data available';
        
        let content = '';
        
        // Use analysisResult which contains the main generated content
        if (data.analysisResult) {
            content += `${data.analysisResult}\n\n`;
        }
        
        // Use projectDescription if available (might be empty)
        if (data.projectDescription) {
            content += `**Project Description:**\n${data.projectDescription}\n\n`;
        }
        
        // Add other fields if they exist
        if (data.keyFeatures && data.keyFeatures.length > 0) {
            content += `**Key Features:**\n${data.keyFeatures.map(f => `- ${f}`).join('\n')}\n\n`;
        }
        if (data.techStack) {
            content += `**Technology Stack:** ${data.techStack}\n\n`;
        }
        if (data.timeline) {
            content += `**Timeline:** ${data.timeline}\n\n`;
        }
        if (data.additionalNotes) {
            content += `**Additional Notes:**\n${data.additionalNotes}\n\n`;
        }
        
        // If no content was found, show the analysis result or a default message
        return content.trim() || data.analysisResult || 'Requirements analysis completed successfully';
    }
    
    formatPlanningContent(data) {
        console.log('formatPlanningContent received data:', data);
        if (!data) return 'No planning data available';
        
        let content = '';
        
        // Use projectRoadmap which contains the main generated content
        if (data.projectRoadmap) {
            content += `${data.projectRoadmap}\n\n`;
        }
        
        // Use architecturalDecisions if available
        if (data.architecturalDecisions) {
            content += `**Architectural Decisions:**\n${data.architecturalDecisions}\n\n`;
        }
        
        // Use milestones if available
        if (data.milestones) {
            content += `**Milestones:**\n${data.milestones}\n\n`;
        }
        
        // Add other fields if they exist
        if (data.techStack) {
            content += `**Technology Stack:** ${data.techStack}\n\n`;
        }
        if (data.projectStructure) {
            content += `**Project Structure:**\n${data.projectStructure}\n\n`;
        }
        if (data.dependencies) {
            content += `**Dependencies:**\n${data.dependencies}\n\n`;
        }
        if (data.deploymentStrategy) {
            content += `**Deployment Strategy:**\n${data.deploymentStrategy}\n\n`;
        }
        
        // If no content was found, show the project roadmap or a default message
        return content.trim() || data.projectRoadmap || 'Project planning completed successfully';
    }
    
    formatStoriesContent(stories) {
        console.log('formatStoriesContent received stories:', stories);
        if (!stories || stories.length === 0) return 'No approved stories available';
        
        let content = `**Approved User Stories (${stories.length}):**\n\n`;
        stories.forEach((story, index) => {
            const title = story.title || `Story ${index + 1}`;
            const description = story.description || 'No description available';
            
            content += `${index + 1}. **${title}**\n`;
            content += `   ${description}\n`;
            
            if (story.acceptanceCriteria && story.acceptanceCriteria.length > 0) {
                content += `   **Acceptance Criteria:** ${story.acceptanceCriteria.join(', ')}\n`;
            }
            
            if (story.priority) {
                content += `   **Priority:** ${story.priority}\n`;
            }
            
            if (story.storyPoints) {
                content += `   **Story Points:** ${story.storyPoints}\n`;
            }
            
            if (story.tags && story.tags.length > 0) {
                content += `   **Tags:** ${story.tags.join(', ')}\n`;
            }
            
            content += `\n`;
        });
        
        return content;
    }
    
    formatPromptsContent(workflowData) {
        if (!workflowData.completionPercentage || workflowData.completionPercentage === 0) {
            return 'Prompt generation not started yet';
        }
        
        if (workflowData.completionPercentage >= 100) {
            return `**Prompt Generation Completed!**\n\n` +
                   `All prompts have been generated and are ready for review.\n\n` +
                   `Completion: ${workflowData.completionPercentage}%`;
        }
        
        return `**Prompt Generation Progress:** ${workflowData.completionPercentage}%\n\n` +
               `Prompts will be available once generation is complete.`;
    }
    
    updateArtifactDetails(artifactType, content) {
        const detailsElement = document.getElementById(`${artifactType}-details`);
        if (detailsElement) {
            detailsElement.textContent = content;
        }
    }
    
    showLoadingState(artifactType, isLoading) {
        const loadingElement = document.getElementById(`${artifactType}-loading`);
        if (loadingElement) {
            loadingElement.style.display = isLoading ? 'block' : 'none';
        }
    }
    
    showArtifactError(artifactType, errorMessage) {
        const detailsElement = document.getElementById(`${artifactType}-details`);
        if (detailsElement) {
            detailsElement.textContent = errorMessage;
            detailsElement.style.color = 'var(--color-danger-500)';
        }
    }
    
    togglePanel() {
        this.isCollapsed = !this.isCollapsed;
        
        if (this.panelElement) {
            this.panelElement.classList.toggle('collapsed', this.isCollapsed);
        }
        
        // Update toggle button text
        if (this.toggleButton) {
            this.toggleButton.textContent = this.isCollapsed ? '⟩' : '⟨';
        }
        
        // Save state to localStorage
        try {
            localStorage.setItem(`artifacts_panel_collapsed_${this.projectId}`, this.isCollapsed);
        } catch (e) {
            console.warn('Failed to save panel state:', e);
        }
        
        console.log('ArtifactsPanel toggled:', this.isCollapsed ? 'collapsed' : 'expanded');
    }
    
    toggleArtifactContent(artifactType) {
        const contentElement = document.getElementById(`${artifactType}-content`);
        const artifactItem = document.querySelector(`[data-artifact="${artifactType}"]`);
        
        if (!contentElement || !artifactItem) return;
        
        const isVisible = contentElement.style.display !== 'none';
        contentElement.style.display = isVisible ? 'none' : 'block';
        
        // Toggle expanded state class
        artifactItem.classList.toggle('expanded', !isVisible);
        
        // Load content if not already loaded and becoming visible
        if (!isVisible && !this.artifacts[artifactType].content && !this.artifacts[artifactType].loading) {
            // Try to get workflow data and load content
            if (window.workflowManager && window.workflowManager.workflowState) {
                const workflowData = window.workflowManager.workflowState;
                let artifactData = null;
                
                switch (artifactType) {
                    case 'requirements':
                        artifactData = workflowData.requirementsAnalysis;
                        break;
                    case 'planning':
                        artifactData = workflowData.projectPlanning;
                        break;
                    case 'stories':
                        artifactData = workflowData.storyGeneration;
                        break;
                    case 'prompts':
                        artifactData = workflowData.promptGeneration;
                        break;
                }
                
                if (artifactData && artifactData.isApproved) {
                    this.loadArtifactContent(artifactType, artifactData);
                }
            }
        }
        
        console.log(`Artifact ${artifactType} content toggled:`, !isVisible ? 'visible' : 'hidden');
    }
    
    // Public API methods
    refreshArtifacts() {
        console.log('Refreshing artifacts...');
        this.loadArtifactsData();
    }
    
    updateArtifact(artifactType, status, content = null) {
        this.updateArtifactStatus(artifactType, { status, isApproved: status === 'Approved' });
        if (content) {
            this.artifacts[artifactType].content = content;
            this.updateArtifactDetails(artifactType, content);
        }
    }
    
    // Cleanup method
    dispose() {
        if (this.unsubscribeWorkflow) {
            this.unsubscribeWorkflow();
        }
        
        // Remove event listeners
        if (this.toggleButton) {
            this.toggleButton.removeEventListener('click', this.togglePanel);
        }
        
        console.log('ArtifactsPanel disposed');
    }
}

// Initialize the artifacts panel when the workflow is ready
let artifactsPanel = null;

function initializeArtifactsPanel(projectId) {
    if (artifactsPanel) {
        artifactsPanel.dispose();
    }
    
    artifactsPanel = new ArtifactsPanel(projectId);
    console.log('ArtifactsPanel instance created');
    
    // Make it globally accessible for debugging and external calls
    window.artifactsPanel = artifactsPanel;
    
    return artifactsPanel;
}

// Auto-initialize when workflow manager is ready
document.addEventListener('DOMContentLoaded', function() {
    // Wait for workflow manager to be ready
    const checkWorkflowReady = setInterval(() => {
        if (window.workflowManager && window.workflowManager.projectId) {
            clearInterval(checkWorkflowReady);
            initializeArtifactsPanel(window.workflowManager.projectId);
        }
    }, 100);
    
    // Timeout after 10 seconds
    setTimeout(() => {
        clearInterval(checkWorkflowReady);
        if (!window.artifactsPanel) {
            console.warn('Workflow manager not ready after 10 seconds, attempting manual initialization');
            const urlParams = new URLSearchParams(window.location.search);
            const projectId = urlParams.get('projectId');
            if (projectId) {
                initializeArtifactsPanel(projectId);
            }
        }
    }, 10000);
});

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ArtifactsPanel, initializeArtifactsPanel };
}
class ProjectOverviewManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.activeTab = this.getHashFromURL() || 'pipeline';
        this.contentCache = new Map();
        this.apiClient = window.APIClient || null;
        this.initElements();
    }

    init() {
        this.attachEventListeners();
        this.switchTab(this.activeTab);
        this.updateProjectInfo();
    }

    initElements() {
        this.projectTitleEl = document.getElementById('projectTitle');
        this.progressSummaryEl = document.getElementById('progressSummary');
        this.tabButtons = document.querySelectorAll('.tab-button');
        this.loadingSpinnerEl = document.getElementById('loadingSpinner');
        this.tabContentEl = document.getElementById('tabContent');
    }

    attachEventListeners() {
        this.tabButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const tabName = button.dataset.tab;
                this.switchTab(tabName);
            });
        });

        // Handle hash changes
        window.addEventListener('hashchange', () => {
            const newTab = this.getHashFromURL();
            if (newTab && newTab !== this.activeTab) {
                this.switchTab(newTab);
            }
        });
    }

    getHashFromURL() {
        return window.location.hash.replace('#', '') || null;
    }

    async switchTab(tabName) {
        if (this.activeTab === tabName) return;

        // Update active tab UI
        this.tabButtons.forEach(btn => btn.classList.remove('active'));
        document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');

        // Show loading
        this.showLoading(true);

        // Update URL hash
        if (window.location.hash !== `#${tabName}`) {
            window.location.hash = tabName;
        }

        // Load content
        const content = await this.loadTabContent(tabName);
        this.tabContentEl.innerHTML = content;

        // Hide loading
        this.showLoading(false);

        // Attach story-specific event listeners if stories tab
        if (tabName === 'stories') {
            this.attachStoryEventListeners();
        }

        this.activeTab = tabName;
    }

    async loadTabContent(tabName) {
        if (this.contentCache.has(tabName)) {
            return this.contentCache.get(tabName);
        }

        let content = '';
        try {
            switch (tabName) {
                case 'pipeline':
                    content = ContentRenderer.renderPipeline(await this.loadPipelineContent(), this.projectId);
                    break;
                case 'requirements':
                    content = await this.loadRequirementsContent();
                    break;
                case 'planning':
                    content = await this.loadPlanningContent();
                    break;
                case 'stories':
                    content = await this.loadStoriesContent();
                    break;
                case 'prompts':
                    content = await this.loadPromptsContent();
                    break;
                default:
                    content = '<p>Tab not found</p>';
            }
            this.contentCache.set(tabName, content);
        } catch (error) {
            console.error(`Error loading ${tabName} content:`, error);
            content = '<p>Error loading content. Please try again.</p>';
        }

        return content;
    }

    showLoading(show) {
        this.loadingSpinnerEl.style.display = show ? 'flex' : 'none';
        this.tabContentEl.style.opacity = show ? '0.5' : '1';
    }

    async updateProjectInfo() {
        if (!this.apiClient) {
            console.warn('APIClient not available');
            return;
        }

        try {
            const projectData = await this.apiClient.get(`/projects/${this.projectId}`);
            this.projectTitleEl.textContent = projectData.name || 'Project Overview';
            
            // Update progress summary (placeholder)
            const status = await this.apiClient.get(`/review/workflow-status/${this.projectId}`);
            this.progressSummaryEl.innerHTML = `<span>Status: ${status.status || 'Unknown'}</span>`;
        } catch (error) {
            console.error('Error updating project info:', error);
        }
    }

    // Placeholder methods for specific content loading
    async loadPipelineContent() {
        // Create a simplified pipeline view that shows workflow status
        if (!this.apiClient) return '<p>API not available</p>';

        try {
            const workflowStatus = await this.apiClient.getWorkflowStatus(this.projectId);

            return `
                <div class="pipeline-workflow-container" id="pipeline-workflow-container">
                    <h3>Project Pipeline Status</h3>
                    <div class="pipeline-stages">
                        <div class="pipeline-stage">
                            <h4>Requirements Analysis</h4>
                            <div class="stage-status" id="pipeline-requirementsStatus">
                                ${workflowStatus.requirementsAnalysis?.isApproved ? 'Approved' :
                                  workflowStatus.requirementsAnalysis?.isPending ? 'Pending Review' :
                                  workflowStatus.requirementsAnalysis?.analysisId ? 'Generated' : 'Not Started'}
                            </div>
                        </div>
                        <div class="pipeline-stage">
                            <h4>Project Planning</h4>
                            <div class="stage-status" id="pipeline-planningStatus">
                                ${workflowStatus.projectPlanning?.isApproved ? 'Approved' :
                                  workflowStatus.projectPlanning?.isPending ? 'Pending Review' :
                                  workflowStatus.projectPlanning?.planningId ? 'Generated' : 'Not Started'}
                            </div>
                        </div>
                        <div class="pipeline-stage">
                            <h4>User Stories</h4>
                            <div class="stage-status" id="pipeline-storiesStatus">
                                ${workflowStatus.storyGeneration?.isApproved ? 'Approved' :
                                  workflowStatus.storyGeneration?.isPending ? 'Pending Review' :
                                  workflowStatus.storyGeneration?.generationId ? 'Generated' : 'Not Started'}
                            </div>
                        </div>
                        <div class="pipeline-stage">
                            <h4>Prompt Generation</h4>
                            <div class="stage-status" id="pipeline-promptStatus">
                                ${workflowStatus.promptGeneration?.completionPercentage === 100 ? 'Complete' :
                                  workflowStatus.promptGeneration?.storyPrompts?.length > 0 ? 'In Progress' : 'Ready'}
                            </div>
                        </div>
                        <div class="pipeline-stage">
                            <h4>Code Generation</h4>
                            <div class="stage-status" id="pipeline-codeStatus">
                                ${workflowStatus.codeGeneration?.generationId ? 'Generated' : 'Not Started'}
                            </div>
                        </div>
                    </div>
                    <div class="pipeline-stats" id="pipeline-stats">
                        <span>Stories: ${workflowStatus.storyGeneration?.storyCount || 0}</span>
                        <span>Approved: ${workflowStatus.promptGeneration?.storyPrompts?.filter(p => p.isApproved).length || 0}</span>
                        <span>Progress: ${workflowStatus.promptGeneration?.completionPercentage || 0}%</span>
                    </div>
                    <div class="pipeline-actions">
                        <button class="view-prompts-btn">View Prompts</button>
                        <button class="download-results-btn">Download Results</button>
                    </div>
                </div>
            `;
        } catch (error) {
            console.error('Error loading pipeline content:', error);
            return `<p>Error loading pipeline status: ${error.message}</p>`;
        }
    }

    async loadRequirementsContent() {
        if (!this.apiClient) return '<p>API not available</p>';
        
        try {
            // Get workflow status to get the requirements analysis ID
            const workflowStatus = await this.apiClient.getWorkflowStatus(this.projectId);
            
            if (workflowStatus.requirementsAnalysis && workflowStatus.requirementsAnalysis.analysisId) {
                // Get the detailed requirements content using the analysis ID
                const requirementsData = await this.apiClient.getRequirements(workflowStatus.requirementsAnalysis.analysisId);
                return ContentRenderer.renderRequirements(requirementsData);
            } else {
                return '<p>No requirements analysis found for this project.</p>';
            }
        } catch (error) {
            console.error('Error loading requirements content:', error);
            return `<p>Error loading requirements content: ${error.message}</p>`;
        }
    }

    async loadPlanningContent() {
        if (!this.apiClient) return '<p>API not available</p>';
        
        try {
            // Get workflow status to get the project planning ID
            const workflowStatus = await this.apiClient.getWorkflowStatus(this.projectId);
            
            if (workflowStatus.projectPlanning && workflowStatus.projectPlanning.planningId) {
                // Get the detailed planning content using the planning ID
                const planningData = await this.apiClient.getProjectPlan(workflowStatus.projectPlanning.planningId);
                return ContentRenderer.renderPlanning(planningData);
            } else {
                return '<p>No project planning found for this project.</p>';
            }
        } catch (error) {
            console.error('Error loading planning content:', error);
            return `<p>Error loading planning content: ${error.message}</p>`;
        }
    }

    async loadStoriesContent() {
        if (!this.apiClient) return '<p>API not available</p>';
        
        try {
            // Get workflow status to get the story generation ID
            const workflowStatus = await this.apiClient.getWorkflowStatus(this.projectId);
            
            if (workflowStatus.storyGeneration && workflowStatus.storyGeneration.generationId) {
                // Get the stories using the generation ID
                const storiesData = await this.apiClient.getStories(workflowStatus.storyGeneration.generationId);
                return ContentRenderer.renderStories(storiesData);
            } else {
                return '<p>No stories generated for this project.</p>';
            }
        } catch (error) {
            console.error('Error loading stories content:', error);
            return `<p>Error loading stories content: ${error.message}</p>`;
        }
    }

    async loadPromptsContent() {
        if (!this.apiClient) return '<p>API not available</p>';

        try {
            // Get workflow status to get the story generation ID for prompts
            const workflowStatus = await this.apiClient.getWorkflowStatus(this.projectId);

            if (workflowStatus.storyGeneration && workflowStatus.storyGeneration.generationId) {
                // Get the stories (which include prompts) using the generation ID
                const storiesData = await this.apiClient.getStories(workflowStatus.storyGeneration.generationId);
                return ContentRenderer.renderPrompts(storiesData);
            } else {
                return '<p>No prompts generated for this project.</p>';
            }
        } catch (error) {
            console.error('Error loading prompts content:', error);
            return `<p>Error loading prompts content: ${error.message}</p>`;
        }
    }
    attachStoryEventListeners() {
        // Expand/collapse functionality
        this.tabContentEl.addEventListener('click', (e) => {
            const target = e.target;
            
            // Handle read more/less buttons
            if (target.classList.contains('read-more-btn')) {
                const storyCard = target.closest('.story-card');
                const fullContent = storyCard.querySelector('.full-content');
                const preview = storyCard.querySelector('.preview');
                
                fullContent.classList.remove('hidden');
                fullContent.classList.add('show');
                preview.style.display = 'none';
                target.textContent = 'Read Less';
                target.classList.remove('read-more-btn');
                target.classList.add('read-less-btn');
            } else if (target.classList.contains('read-less-btn')) {
                const storyCard = target.closest('.story-card');
                const fullContent = storyCard.querySelector('.full-content');
                const preview = storyCard.querySelector('.preview');
                
                fullContent.classList.remove('show');
                fullContent.classList.add('hidden');
                preview.style.display = 'block';
                target.textContent = 'Read More';
                target.classList.remove('read-less-btn');
                target.classList.add('read-more-btn');
            }
            
            // Handle edit button
            if (target.classList.contains('edit-btn')) {
                const storyCard = target.closest('.story-card');
                const editMode = storyCard.querySelector('.story-edit-mode');
                const storyContent = storyCard.querySelector('.story-content');
                const storyActions = storyCard.querySelector('.story-actions');
                
                // Toggle edit mode
                if (editMode.classList.contains('active')) {
                    editMode.classList.remove('active');
                    storyContent.style.display = 'block';
                    storyActions.style.display = 'flex';
                    target.textContent = 'Edit';
                } else {
                    editMode.classList.add('active');
                    storyContent.style.display = 'none';
                    storyActions.style.display = 'none';
                    target.textContent = 'Cancel';
                }
            }
            
            // Handle approve button
            if (target.classList.contains('approve-btn')) {
                const storyCard = target.closest('.story-card');
                const storyIndex = storyCard.dataset.storyIndex;
                const statusBadge = storyCard.querySelector('.status-badge');
                
                // Show loading state
                target.innerHTML = '<span class="loading"></span>Approving...';
                target.disabled = true;
                
                // Call API to approve story
                this.approveStory(storyIndex, storyCard);
            }
            
            // Handle reject button
            if (target.classList.contains('reject-btn')) {
                const storyCard = target.closest('.story-card');
                const storyIndex = storyCard.dataset.storyIndex;
                const statusBadge = storyCard.querySelector('.status-badge');
                
                // Show loading state
                target.innerHTML = '<span class="loading"></span>Rejecting...';
                target.disabled = true;
                
                // Call API to reject story
                this.rejectStory(storyIndex, storyCard);
            }
            
            // Handle save changes button
            if (target.classList.contains('save-btn')) {
                const storyCard = target.closest('.story-card');
                const storyIndex = storyCard.dataset.storyIndex;
                
                // Show loading
                target.innerHTML = '<span class="loading"></span>Saving...';
                target.disabled = true;
                
                // Call API to update story
                this.updateStory(storyIndex, storyCard);
            }
            
            // Handle cancel button
            if (target.classList.contains('cancel-btn')) {
                const storyCard = target.closest('.story-card');
                const editMode = storyCard.querySelector('.story-edit-mode');
                const storyContent = storyCard.querySelector('.story-content');
                const storyActions = storyCard.querySelector('.story-actions');
                const editBtn = storyCard.querySelector('.edit-btn');
                
                editMode.classList.remove('active');
                storyContent.style.display = 'block';
                storyActions.style.display = 'flex';
                editBtn.textContent = 'Edit';
            }
            
            // Handle generate prompt button
            if (target.classList.contains('generate-prompt-btn')) {
                const storyCard = target.closest('.story-card');
                const storyIndex = storyCard.dataset.storyIndex;
                
                this.generatePrompt(storyIndex);
            }
        });
    }

    async approveStory(storyIndex, storyCard) {
        try {
            // In a real implementation, we would call the API to approve the individual story
            // For now, we'll simulate the approval and update the UI
            
            // Update UI immediately for better UX
            const statusBadge = storyCard.querySelector('.status-badge');
            statusBadge.textContent = 'Approved';
            statusBadge.className = 'status-badge approved';
            
            const approveBtn = storyCard.querySelector('.approve-btn');
            approveBtn.classList.add('hidden');
            
            const rejectBtn = storyCard.querySelector('.reject-btn');
            rejectBtn.classList.remove('hidden');
            
            const generateBtn = storyCard.querySelector('.generate-prompt-btn');
            generateBtn.disabled = false;
            
            // Reset button text
            approveBtn.innerHTML = 'Approve';
            approveBtn.disabled = false;
            
            // Refresh stats
            // In a real implementation, we would call the API to get the updated state
            // this.refreshStoryStats();
            
            // Show success message
            this.showNotification('Story approved successfully!', 'success');
            
        } catch (error) {
            console.error('Failed to approve story:', error);
            
            // Reset button text
            const approveBtn = storyCard.querySelector('.approve-btn');
            approveBtn.innerHTML = 'Approve';
            approveBtn.disabled = false;
            
            // Show error message
            this.showNotification('Failed to approve story. Please try again.', 'error');
        }
    }

    async rejectStory(storyIndex, storyCard) {
        try {
            // In a real implementation, we would call the API to reject the individual story
            // For now, we'll simulate the rejection and update the UI
            
            // Update UI immediately for better UX
            const statusBadge = storyCard.querySelector('.status-badge');
            statusBadge.textContent = 'Rejected';
            statusBadge.className = 'status-badge rejected';
            
            const rejectBtn = storyCard.querySelector('.reject-btn');
            rejectBtn.classList.add('hidden');
            
            const approveBtn = storyCard.querySelector('.approve-btn');
            approveBtn.classList.remove('hidden');
            
            const generateBtn = storyCard.querySelector('.generate-prompt-btn');
            generateBtn.disabled = true;
            
            // Reset button text
            rejectBtn.innerHTML = 'Reject';
            rejectBtn.disabled = false;
            
            // Refresh stats
            // In a real implementation, we would call the API to get the updated state
            // this.refreshStoryStats();
            
            // Show success message
            this.showNotification('Story rejected successfully!', 'success');
            
        } catch (error) {
            console.error('Failed to reject story:', error);
            
            // Reset button text
            const rejectBtn = storyCard.querySelector('.reject-btn');
            rejectBtn.innerHTML = 'Reject';
            rejectBtn.disabled = false;
            
            // Show error message
            this.showNotification('Failed to reject story. Please try again.', 'error');
        }
    }

    async updateStory(storyIndex, storyCard) {
        try {
            const titleInput = storyCard.querySelector('.story-title-edit');
            const descriptionInput = storyCard.querySelector('.story-description-edit');
            const criteriaInput = storyCard.querySelector('.story-acceptance-criteria-edit');
            const editMode = storyCard.querySelector('.story-edit-mode');
            const storyContent = storyCard.querySelector('.story-content');
            const storyActions = storyCard.querySelector('.story-actions');
            const editBtn = storyCard.querySelector('.edit-btn');
            
            // Prepare updated story data
            const updatedStory = {
                Title: titleInput.value,
                Description: descriptionInput.value,
                AcceptanceCriteria: criteriaInput.value.split('\n').filter(c => c.trim())
            };
            
            // In a real implementation, we would call the API to update the story
            // For now, we'll simulate the update and update the UI
            
            // Update story header with new title
            const storyHeader = storyCard.querySelector('.story-header h3');
            storyHeader.textContent = updatedStory.Title;
            
            // Update preview text
            const preview = storyCard.querySelector('.preview p');
            const newPreview = updatedStory.Description.length > 100 ?
                updatedStory.Description.substring(0, 100) + '...' : updatedStory.Description;
            preview.textContent = newPreview;
            
            // Hide edit mode
            editMode.classList.remove('active');
            storyContent.style.display = 'block';
            storyActions.style.display = 'flex';
            editBtn.textContent = 'Edit';
            
            // Reset button text
            const saveBtn = storyCard.querySelector('.save-btn');
            saveBtn.innerHTML = 'Save Changes';
            saveBtn.disabled = false;
            
            // Show success notification
            this.showNotification('Story updated successfully!', 'success');
            
        } catch (error) {
            console.error('Failed to update story:', error);
            
            // Reset button text
            const saveBtn = storyCard.querySelector('.save-btn');
            saveBtn.innerHTML = 'Save Changes';
            saveBtn.disabled = false;
            
            // Show error toast
            this.showNotification('Failed to update story. Please try again.', 'error');
        }
    }

    async generatePrompt(storyIndex) {
        // This method would call the API to generate a prompt for the story
        // For now, we'll just show a notification
        this.showNotification(`Prompt generation for story ${storyIndex} would start here.`, 'info');
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification-toast ${type}`;
        notification.textContent = message;
        
        // Add to document
        document.body.appendChild(notification);
        
        // Remove after 3 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 3000);
    }

}

class ContentRenderer {
    static renderRequirements(data) {
        // Handle missing or invalid data
        if (!data) {
            return '<div class="requirements-content"><p>No requirements data available.</p></div>';
        }

        const status = data.status || 'Unknown';
        const content = data.content || 'No content available';
        const createdAt = data.createdAt || new Date().toISOString();
        const actions = Array.isArray(data.actions) ? data.actions : ['Regenerate', 'Export', 'Edit'];

        return `
            <div class="requirements-content">
                <h2>Requirements Analysis</h2>
                <div class="status-badge">${status}</div>
                <div class="content">${content}</div>
                <p>Created: ${new Date(createdAt).toLocaleDateString()}</p>
                <div class="actions">
                    ${actions.map(action => `<button>${action}</button>`).join('')}
                </div>
            </div>
        `;
    }

    static renderPlanning(data) {
        // Handle missing or invalid data
        if (!data) {
            return '<div class="planning-content"><p>No planning data available.</p></div>';
        }

        const roadmap = data.roadmap || 'No roadmap available';
        const architecture = data.architecture || 'No architecture details available';
        const status = data.status || 'Unknown';
        const actions = Array.isArray(data.actions) ? data.actions : ['Update', 'Export'];

        // Handle milestones - could be array or string
        let milestonesHtml = '';
        if (Array.isArray(data.milestones)) {
            milestonesHtml = data.milestones.map(m => `<li>${m}</li>`).join('');
        } else if (typeof data.milestones === 'string') {
            // If milestones is a string, split by newlines or treat as single item
            const milestonesList = data.milestones.split('\n').filter(m => m.trim());
            milestonesHtml = milestonesList.map(m => `<li>${m.trim()}</li>`).join('');
        } else {
            milestonesHtml = '<li>No milestones defined</li>';
        }

        return `
            <div class="planning-content">
                <h2>Project Planning</h2>
                <div class="status-badge">${status}</div>
                <h3>Roadmap</h3>
                <div class="roadmap">${roadmap}</div>
                <h3>Architecture</h3>
                <div class="architecture">${architecture}</div>
                <h3>Milestones</h3>
                <ul>${milestonesHtml}</ul>
                <div class="actions">
                    ${actions.map(action => `<button>${action}</button>`).join('')}
                </div>
            </div>
        `;
    }

    static renderPrompts(data) {
        // Handle missing or invalid data
        if (!data) {
            return '<div class="prompts-content"><p>No prompts data available.</p></div>';
        }

        // Ensure data is an array
        if (!Array.isArray(data)) {
            return '<div class="prompts-content"><p>Invalid prompts data format.</p></div>';
        }

        if (data.length === 0) {
            return '<div class="prompts-content"><p>No prompts generated yet.</p></div>';
        }

        let html = '<div class="prompts-content"><h2>Generated Prompts</h2>';

        data.forEach((story, index) => {
            // Handle different story data structures
            const storyTitle = story.Title || story.title || story.storyTitle || `Story ${index + 1}`;
            const storyPrompts = story.prompts || [];

            // Ensure prompts is an array
            const promptsArray = Array.isArray(storyPrompts) ? storyPrompts : [];

            if (promptsArray.length === 0) {
                html += `
                    <div class="story-section">
                        <h3>${storyTitle}</h3>
                        <p>No prompts generated for this story yet.</p>
                    </div>
                `;
            } else {
                html += `
                    <div class="story-section">
                        <h3>${storyTitle}</h3>
                        <div class="prompts-list">
                            ${promptsArray.map(prompt => {
                                const status = prompt.status || 'Unknown';
                                const content = prompt.content || prompt.generatedPrompt || 'No content';
                                const createdAt = prompt.createdAt || new Date().toISOString();
                                const actions = Array.isArray(prompt.actions) ? prompt.actions : ['Copy', 'Download', 'Regenerate'];

                                return `
                                    <div class="prompt-card">
                                        <div class="prompt-status">${status}</div>
                                        <div class="prompt-content">${content}</div>
                                        <p>Created: ${new Date(createdAt).toLocaleDateString()}</p>
                                        <div class="prompt-actions">
                                            ${actions.map(action => `<button>${action}</button>`).join('')}
                                        </div>
                                    </div>
                                `;
                            }).join('')}
                        </div>
                    </div>
                `;
            }
        });

        html += '</div>';
        return html;
    }

    // Additional render methods to be implemented
    static renderPipeline(data, projectId) {
        // The data is already the HTML content from loadPipelineContent
        return data;
    }

    static renderStories(data, projectId) {
        // Handle missing or invalid data
        if (!data) {
            return '<div class="stories-content"><p>No stories data available.</p></div>';
        }

        // Ensure data is an array
        if (!Array.isArray(data)) {
            return '<div class="stories-content"><p>Invalid stories data format.</p></div>';
        }

        if (data.length === 0) {
            return '<div class="stories-content"><p>No stories generated yet.</p></div>';
        }

        // Calculate statistics
        const totalStories = data.length;
        const approvedStories = data.filter(story => story.Status === 'Approved').length;
        const draftStories = data.filter(story => story.Status === 'Draft' || !story.Status).length;
        const rejectedStories = data.filter(story => story.Status === 'Rejected').length;

        // Render stories as enhanced HTML
        const storiesHtml = data.map((story, index) => {
            const title = story.Title || story.title || `Story ${index + 1}`;
            const description = story.Description || story.description || '';
            const acceptanceCriteria = story.AcceptanceCriteria || story.acceptanceCriteria || [];
            const status = story.Status || 'Draft';
            const storyId = story.Id || `story-${index}`; // Use actual ID if available
            const isApproved = status === 'Approved';

            // Create preview text (first 100 chars of description)
            const previewText = description.length > 100 ? description.substring(0, 100) + '...' : description;
            const fullDescription = description.replace(/\n/g, '<br>');

            // Format acceptance criteria for display
            const criteriaHtml = acceptanceCriteria.map(criteria => `<li>${criteria}</li>`).join('');

            return `
                <div class="story-card" data-story-id="${storyId}" data-story-index="${index}">
                    <div class="story-header">
                        <h3>${title}</h3>
                        <span class="status-badge ${status.toLowerCase()}">${status}</span>
                    </div>
                    
                    <div class="story-content expandable">
                        <div class="preview">
                            <p>${previewText}</p>
                            <button class="read-more-btn" data-toggle="expand">Read More</button>
                        </div>
                        <div class="full-content hidden">
                            <div class="story-description">${fullDescription}</div>
                            ${criteriaHtml ? `<div class="acceptance-criteria"><h4>Acceptance Criteria:</h4><ul>${criteriaHtml}</ul></div>` : ''}
                        </div>
                    </div>
                    
                    <div class="story-edit-mode">
                        <input type="text" class="story-title-edit" value="${title}" placeholder="Story Title">
                        <textarea class="story-description-edit" placeholder="Story description...">${description}</textarea>
                        <textarea class="story-acceptance-criteria-edit" placeholder="Acceptance criteria...">${acceptanceCriteria.join('\n')}</textarea>
                        <div class="edit-actions">
                            <button class="save-btn">Save Changes</button>
                            <button class="cancel-btn">Cancel</button>
                        </div>
                    </div>
                    
                    <div class="story-actions">
                        <button class="edit-btn">Edit</button>
                        <button class="approve-btn ${isApproved ? 'hidden' : ''}">Approve</button>
                        <button class="reject-btn ${status === 'Rejected' ? 'hidden' : ''}">Reject</button>
                        <button class="generate-prompt-btn" ${isApproved ? '' : 'disabled'}>Generate Prompt</button>
                    </div>
                </div>
            `;
        }).join('');

        return `
            <div class="stories-content">
                <h2>User Stories</h2>
                <div class="stories-summary">
                    <div class="progress-indicator">
                        <div class="progress-item">
                            <span class="progress-number">${totalStories}</span>
                            <span class="progress-label">Total Stories</span>
                        </div>
                        <div class="progress-item">
                            <span class="progress-number">${approvedStories}</span>
                            <span class="progress-label">Approved</span>
                        </div>
                        <div class="progress-item">
                            <span class="progress-number">${draftStories}</span>
                            <span class="progress-label">Draft</span>
                        </div>
                        <div class="progress-item">
                            <span class="progress-number">${rejectedStories}</span>
                            <span class="progress-label">Rejected</span>
                        </div>
                    </div>
                    <div style="margin-top: 15px; font-size: 0.9em; color: #6c757d;">
                        Progress: ${approvedStories} of ${totalStories} stories approved (${Math.round((approvedStories/totalStories)*100)}%)
                    </div>
                </div>
                <div class="stories-grid" id="stories-grid-container">
                    ${storiesHtml}
                </div>
                <div class="stories-actions">
                    <button id="refresh-stories-btn" class="primary-btn">Refresh Stories</button>
                    <button id="back-to-workflow-btn" class="secondary-btn">Back to Workflow</button>
                </div>
            </div>
        `;
    }
}

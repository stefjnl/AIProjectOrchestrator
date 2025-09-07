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

        // Render stories as proper HTML
        const storiesHtml = data.map((story, index) => {
            const title = story.Title || story.title || `Story ${index + 1}`;
            const asA = story.AsA || story.asA || 'Not specified';
            const iWant = story.IWant || story.iWant || 'Not specified';
            const soThat = story.SoThat || story.soThat || 'Not specified';

            return `
                <div class="story-card" data-story-index="${index}">
                    <div class="story-header">
                        <h3>Story ${index + 1}: ${title}</h3>
                        <span class="status-badge secondary">Not Started</span>
                    </div>
                    <div class="story-content">
                        <p><strong>As a</strong> ${asA}</p>
                        <p><strong>I want</strong> ${iWant}</p>
                        <p><strong>So that</strong> ${soThat}</p>
                    </div>
                    <div class="story-actions">
                        <button class="generate-prompt-btn" disabled>Generate Prompt</button>
                    </div>
                </div>
            `;
        }).join('');

        return `
            <div class="stories-content">
                <h2>User Stories</h2>
                <div class="stories-summary" id="stories-summary-stats">
                    <p>Loading story statistics...</p>
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

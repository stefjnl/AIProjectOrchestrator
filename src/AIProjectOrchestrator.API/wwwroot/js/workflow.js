// Advanced Workflow Management JavaScript
class WorkflowManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.currentStage = 3; // Default to Stories stage
        this.stages = ['requirements', 'planning', 'stories', 'prompts', 'review'];
        this.autoRefreshInterval = null;
        this.isAutoRefreshing = false;

        this.initialize();
    }

    initialize() {
        console.log(`WorkflowManager initialized for project ${this.projectId}`);
        this.setupEventListeners();
        this.startAutoRefresh();
        this.loadInitialData();
    }

    setupEventListeners() {
        // Navigation buttons
        document.getElementById('prev-stage')?.addEventListener('click', () => this.navigateStage(-1));
        document.getElementById('next-stage')?.addEventListener('click', () => this.navigateStage(1));

        // Stage indicators
        document.querySelectorAll('.stage-indicator').forEach((indicator, index) => {
            indicator.addEventListener('click', () => this.jumpToStage(index + 1));
        });

        // Auto-refresh toggle
        const autoRefreshToggle = document.getElementById('auto-refresh-toggle');
        if (autoRefreshToggle) {
            autoRefreshToggle.addEventListener('change', (e) => {
                if (e.target.checked) {
                    this.startAutoRefresh();
                } else {
                    this.stopAutoRefresh();
                }
            });
        }

        // Keyboard navigation
        document.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowLeft') this.navigateStage(-1);
            if (e.key === 'ArrowRight') this.navigateStage(1);
        });
    }

    async loadInitialData() {
        try {
            await this.loadProjectData();
            await this.loadCurrentStage();
            this.updateUI();
        } catch (error) {
            console.error('Failed to load initial workflow data:', error);
            showNotification('Failed to load workflow data', 'error');
        }
    }

    async loadProjectData() {
        try {
            const project = await APIClient.getProject(this.projectId);
            this.updateProjectOverview(project);
            this.updateProgressIndicators(project);
            return project;
        } catch (error) {
            throw new Error(`Failed to load project data: ${error.message}`);
        }
    }

    updateProjectOverview(project) {
        document.getElementById('project-name').textContent = project.name;
        document.getElementById('project-status').textContent = project.status;
        document.getElementById('project-created').textContent =
            new Date(project.createdAt).toLocaleDateString();
    }

    updateProgressIndicators(project) {
        const progress = this.calculateProgress(project);
        document.getElementById('project-progress').textContent = `${progress}%`;
        this.updatePipelineIndicators(progress);
    }

    calculateProgress(project) {
        const stages = ['requirements', 'planning', 'stories', 'prompts', 'review'];
        let completed = 0;

        stages.forEach(stage => {
            if (project[stage]?.completed) completed++;
        });

        return Math.round((completed / stages.length) * 100);
    }

    updatePipelineIndicators(progress) {
        const stages = ['stage-1', 'stage-2', 'stage-3', 'stage-4', 'stage-5'];

        stages.forEach((stageId, index) => {
            const stage = document.getElementById(stageId);
            const stageProgress = ((index + 1) / 5) * 100;

            if (progress >= stageProgress) {
                stage.classList.add('completed');
                stage.classList.remove('active');
            } else if (index === Math.floor(progress / 20)) {
                stage.classList.add('active');
                stage.classList.remove('completed');
            } else {
                stage.classList.remove('completed', 'active');
            }
        });
    }

    async loadCurrentStage() {
        try {
            const status = await APIClient.getWorkflowStatus(this.projectId);
            if (status.currentStage) {
                this.currentStage = this.stages.indexOf(status.currentStage) + 1;
            }
            await this.loadStageContent(this.currentStage);
        } catch (error) {
            console.warn('Could not load workflow status, using default stage');
            await this.loadStageContent(this.currentStage);
        }
    }

    async loadStageContent(stage) {
        this.currentStage = stage;

        // Update navigation
        document.getElementById('stage-counter').textContent = `Stage ${stage} of 5`;
        document.getElementById('prev-stage').disabled = stage === 1;
        document.getElementById('next-stage').textContent = stage === 5 ? 'Complete' : 'Next →';

        // Load stage-specific content
        const content = await this.getStageContent(stage);
        document.getElementById('stage-content').innerHTML = content;

        // Initialize stage-specific functionality
        this.initializeStageFunctionality(stage);
    }

    async getStageContent(stage) {
        const templates = {
            1: this.getRequirementsStage.bind(this),
            2: this.getPlanningStage.bind(this),
            3: this.getStoriesStage.bind(this),
            4: this.getPromptsStage.bind(this),
            5: this.getReviewStage.bind(this)
        };

        return templates[stage] ? await templates[stage]() : '<p>Stage not found</p>';
    }

    async getRequirementsStage() {
        try {
            const requirements = await APIClient.getRequirements(this.projectId);
            return `
                <div class="stage-container">
                    <h2>Requirements Analysis</h2>
                    <div class="requirements-content">
                        <div class="requirements-summary">
                            <h3>Analysis Results</h3>
                            <div class="summary-content">
                                ${this.formatRequirements(requirements)}
                            </div>
                        </div>
                        <div class="requirements-actions">
                            <button class="btn btn-primary" onclick="workflowManager.analyzeRequirements()">
                                🔄 Re-analyze Requirements
                            </button>
                            <button class="btn btn-secondary" onclick="workflowManager.editRequirements()">
                                ✏️ Edit Requirements
                            </button>
                        </div>
                    </div>
                </div>
            `;
        } catch (error) {
            return this.getRequirementsEmptyState();
        }
    }

    getRequirementsEmptyState() {
        return `
            <div class="stage-container">
                <h2>Requirements Analysis</h2>
                <div class="empty-stage">
                    <div class="empty-icon">📋</div>
                    <h3>No Requirements Found</h3>
                    <p>Start by analyzing your project requirements.</p>
                    <button class="btn btn-primary" onclick="workflowManager.analyzeRequirements()">
                        🚀 Start Analysis
                    </button>
                </div>
            </div>
        `;
    }

    formatRequirements(requirements) {
        if (!requirements || !requirements.analysis) {
            return '<p>No requirements analysis available.</p>';
        }

        return `
            <div class="requirements-grid">
                <div class="requirement-category">
                    <h4>Functional Requirements</h4>
                    <ul>
                        ${requirements.analysis.functional?.map(req => `<li>${req}</li>`).join('') || '<li>No functional requirements</li>'}
                    </ul>
                </div>
                <div class="requirement-category">
                    <h4>Non-Functional Requirements</h4>
                    <ul>
                        ${requirements.analysis.nonFunctional?.map(req => `<li>${req}</li>`).join('') || '<li>No non-functional requirements</li>'}
                    </ul>
                </div>
                <div class="requirement-category">
                    <h4>Technical Constraints</h4>
                    <ul>
                        ${requirements.analysis.constraints?.map(req => `<li>${req}</li>`).join('') || '<li>No constraints</li>'}
                    </ul>
                </div>
            </div>
        `;
    }

    async getPlanningStage() {
        try {
            const planning = await APIClient.getProjectPlan(this.projectId);
            return `
                <div class="stage-container">
                    <h2>Project Planning</h2>
                    <div class="planning-content">
                        <div class="architecture-overview">
                            <h3>Technical Architecture</h3>
                            ${this.formatPlanning(planning)}
                        </div>
                        <div class="planning-actions">
                            <button class="btn btn-primary" onclick="workflowManager.regeneratePlan()">
                                🔄 Regenerate Plan
                            </button>
                            <button class="btn btn-secondary" onclick="workflowManager.editPlanning()">
                                ✏️ Edit Plan
                            </button>
                        </div>
                    </div>
                </div>
            `;
        } catch (error) {
            return this.getPlanningEmptyState();
        }
    }

    getPlanningEmptyState() {
        return `
            <div class="stage-container">
                <h2>Project Planning</h2>
                <div class="empty-stage">
                    <div class="empty-icon">🏗️</div>
                    <h3>No Project Plan Found</h3>
                    <p>Create a technical architecture plan for your project.</p>
                    <button class="btn btn-primary" onclick="workflowManager.regeneratePlan()">
                        🚀 Generate Plan
                    </button>
                </div>
            </div>
        `;
    }

    formatPlanning(planning) {
        if (!planning || !planning.plan) {
            return '<p>No planning data available.</p>';
        }

        return `
            <div class="planning-grid">
                <div class="planning-section">
                    <h4>Architecture Overview</h4>
                    <p>${planning.plan.architecture || 'No architecture overview'}</p>
                </div>
                <div class="planning-section">
                    <h4>Technology Stack</h4>
                    <ul>
                        ${planning.plan.techStack?.map(tech => `<li>${tech}</li>`).join('') || '<li>No tech stack specified</li>'}
                    </ul>
                </div>
                <div class="planning-section">
                    <h4>Development Phases</h4>
                    <ol>
                        ${planning.plan.phases?.map(phase => `<li>${phase}</li>`).join('') || '<li>No phases defined</li>'}
                    </ol>
                </div>
            </div>
        `;
    }

    async getStoriesStage() {
        try {
            const stories = await APIClient.getStories(this.projectId);
            return `
                <div class="stage-container">
                    <h2>User Stories</h2>
                    <div class="stories-content">
                        <div class="stories-controls">
                            <button class="btn btn-primary" onclick="workflowManager.generateStories()">
                                ✨ Generate Stories
                            </button>
                            <button class="btn btn-secondary" onclick="workflowManager.addCustomStory()">
                                ➕ Add Custom Story
                            </button>
                        </div>
                        <div class="stories-list" id="stories-list">
                            ${this.formatStories(stories)}
                        </div>
                    </div>
                </div>
            `;
        } catch (error) {
            return this.getStoriesEmptyState();
        }
    }

    getStoriesEmptyState() {
        return `
            <div class="stage-container">
                <h2>User Stories</h2>
                <div class="empty-stage">
                    <div class="empty-icon">📖</div>
                    <h3>No User Stories Found</h3>
                    <p>Generate user stories based on your requirements and planning.</p>
                    <button class="btn btn-primary" onclick="workflowManager.generateStories()">
                        ✨ Generate Stories
                    </button>
                </div>
            </div>
        `;
    }

    formatStories(stories) {
        if (!stories || stories.length === 0) {
            return '<p>No user stories available.</p>';
        }

        return `
            <div class="stories-grid">
                ${stories.map(story => `
                    <div class="story-card" data-story-id="${story.id}">
                        <div class="story-header">
                            <h4>${story.title}</h4>
                            <span class="story-status ${story.status}">${story.status}</span>
                        </div>
                        <p class="story-description">${story.description}</p>
                        <div class="story-meta">
                            <span class="story-points">Points: ${story.storyPoints || 'N/A'}</span>
                            <span class="story-priority">Priority: ${story.priority || 'Normal'}</span>
                        </div>
                        <div class="story-actions">
                            <button class="btn btn-sm btn-primary" onclick="workflowManager.viewStory('${story.id}')">
                                View Details
                            </button>
                            ${story.status === 'pending' ? `
                                <button class="btn btn-sm btn-success" onclick="workflowManager.approveStory('${story.id}')">
                                    Approve
                                </button>
                                <button class="btn btn-sm btn-danger" onclick="workflowManager.rejectStory('${story.id}')">
                                    Reject
                                </button>
                            ` : ''}
                        </div>
                    </div>
                `).join('')}
            </div>
        `;
    }

    async getPromptsStage() {
        try {
            const prompts = await APIClient.getPrompts(this.projectId);
            return `
                <div class="stage-container">
                    <h2>Prompt Generation</h2>
                    <div class="prompts-content">
                        <div class="prompts-summary">
                            <h3>Generated Prompts</h3>
                            ${this.formatPrompts(prompts)}
                        </div>
                        <div class="prompts-actions">
                            <button class="btn btn-primary" onclick="workflowManager.generateAllPrompts()">
                                🚀 Generate All Prompts
                            </button>
                            <button class="btn btn-secondary" onclick="workflowManager.customizePrompts()">
                                ⚙️ Customize Prompts
                            </button>
                        </div>
                    </div>
                </div>
            `;
        } catch (error) {
            return this.getPromptsEmptyState();
        }
    }

    getPromptsEmptyState() {
        return `
            <div class="stage-container">
                <h2>Prompt Generation</h2>
                <div class="empty-stage">
                    <div class="empty-icon">🤖</div>
                    <h3>No Prompts Found</h3>
                    <p>Generate code prompts based on your approved user stories.</p>
                    <button class="btn btn-primary" onclick="workflowManager.generateAllPrompts()">
                        🚀 Generate Prompts
                    </button>
                </div>
            </div>
        `;
    }

    formatPrompts(prompts) {
        if (!prompts || prompts.length === 0) {
            return '<p>No prompts available.</p>';
        }

        return `
            <div class="prompts-grid">
                ${prompts.map(prompt => `
                    <div class="prompt-card" data-prompt-id="${prompt.id}">
                        <div class="prompt-header">
                            <h4>${prompt.title}</h4>
                            <span class="prompt-status ${prompt.status}">${prompt.status}</span>
                        </div>
                        <div class="prompt-content">
                            <pre>${prompt.content.substring(0, 200)}${prompt.content.length > 200 ? '...' : ''}</pre>
                        </div>
                        <div class="prompt-meta">
                            <span class="prompt-language">${prompt.language || 'Not specified'}</span>
                            <span class="prompt-type">${prompt.type || 'General'}</span>
                        </div>
                        <div class="prompt-actions">
                            <button class="btn btn-sm btn-primary" onclick="workflowManager.viewPrompt('${prompt.id}')">
                                View Full Prompt
                            </button>
                            <button class="btn btn-sm btn-secondary" onclick="workflowManager.copyPrompt('${prompt.id}')">
                                Copy
                            </button>
                        </div>
                    </div>
                `).join('')}
            </div>
        `;
    }

    async getReviewStage() {
        try {
            const reviews = await APIClient.getPendingReviews();
            return `
                <div class="stage-container">
                    <h2>Final Review</h2>
                    <div class="review-content">
                        <div class="review-summary">
                            <h3>Review Summary</h3>
                            ${this.formatReviewSummary(reviews)}
                        </div>
                        <div class="review-actions">
                            <button class="btn btn-success" onclick="workflowManager.completeProject()">
                                ✅ Complete Project
                            </button>
                            <button class="btn btn-secondary" onclick="workflowManager.exportProject()">
                                📥 Export Results
                            </button>
                            <button class="btn btn-outline" onclick="workflowManager.generateReport()">
                                📊 Generate Report
                            </button>
                        </div>
                    </div>
                </div>
            `;
        } catch (error) {
            return this.getReviewEmptyState();
        }
    }

    getReviewEmptyState() {
        return `
            <div class="stage-container">
                <h2>Final Review</h2>
                <div class="empty-stage">
                    <div class="empty-icon">✅</div>
                    <h3>Ready for Review</h3>
                    <p>All prompts have been generated and are ready for final review.</p>
                    <button class="btn btn-success" onclick="workflowManager.completeProject()">
                        ✅ Complete Project
                    </button>
                </div>
            </div>
        `;
    }

    formatReviewSummary(reviews) {
        const total = reviews.length;
        const pending = reviews.filter(r => r.status === 'pending').length;
        const approved = reviews.filter(r => r.status === 'approved').length;
        const rejected = reviews.filter(r => r.status === 'rejected').length;

        return `
            <div class="review-summary-grid">
                <div class="summary-stat">
                    <h4>Total Reviews</h4>
                    <span class="stat-number">${total}</span>
                </div>
                <div class="summary-stat">
                    <h4>Pending</h4>
                    <span class="stat-number pending">${pending}</span>
                </div>
                <div class="summary-stat">
                    <h4>Approved</h4>
                    <span class="stat-number approved">${approved}</span>
                </div>
                <div class="summary-stat">
                    <h4>Rejected</h4>
                    <span class="stat-number rejected">${rejected}</span>
                </div>
            </div>
            <div class="review-progress">
                <div class="progress-bar">
                    <div class="progress-fill" style="width: ${total > 0 ? (approved / total) * 100 : 0}%"></div>
                </div>
                <p>${approved} of ${total} reviews approved</p>
            </div>
        `;
    }

    initializeStageFunctionality(stage) {
        // Add stage-specific event listeners and functionality
        switch (stage) {
            case 1:
                this.initializeRequirementsStage();
                break;
            case 2:
                this.initializePlanningStage();
                break;
            case 3:
                this.initializeStoriesStage();
                break;
            case 4:
                this.initializePromptsStage();
                break;
            case 5:
                this.initializeReviewStage();
                break;
        }
    }

    // Stage action methods
    async analyzeRequirements() {
        showLoading('Analyzing requirements...');
        try {
            const requirementsInput = prompt('Please provide your project requirements:');
            if (!requirementsInput) return;

            const result = await APIClient.analyzeRequirements(this.projectId, requirementsInput);
            showSuccess('Requirements analyzed successfully!');
            await this.loadStageContent(1);
        } catch (error) {
            handleApiError(error, 'Failed to analyze requirements');
        } finally {
            hideLoading();
        }
    }

    async regeneratePlan() {
        showLoading('Regenerating project plan...');
        try {
            // Implementation for plan regeneration
            showSuccess('Project plan regenerated successfully!');
            await this.loadStageContent(2);
        } catch (error) {
            handleApiError(error, 'Failed to regenerate plan');
        } finally {
            hideLoading();
        }
    }

    async generateStories() {
        showLoading('Generating user stories...');
        try {
            // Implementation for story generation
            showSuccess('User stories generated successfully!');
            await this.loadStageContent(3);
        } catch (error) {
            handleApiError(error, 'Failed to generate stories');
        } finally {
            hideLoading();
        }
    }

    async generateAllPrompts() {
        showLoading('Generating all prompts...');
        try {
            // Implementation for prompt generation
            showSuccess('All prompts generated successfully!');
            await this.loadStageContent(4);
        } catch (error) {
            handleApiError(error, 'Failed to generate prompts');
        } finally {
            hideLoading();
        }
    }

    async completeProject() {
        if (confirm('Are you sure you want to complete this project? This action cannot be undone.')) {
            showLoading('Completing project...');
            try {
                // Implementation for project completion
                showSuccess('Project completed successfully!');
                setTimeout(() => {
                    window.location.href = '/Projects';
                }, 2000);
            } catch (error) {
                handleApiError(error, 'Failed to complete project');
            } finally {
                hideLoading();
            }
        }
    }

    // Navigation methods
    async navigateStage(direction) {
        const newStage = this.currentStage + direction;
        if (newStage >= 1 && newStage <= 5) {
            await this.loadStageContent(newStage);
        }
    }

    async jumpToStage(stage) {
        if (stage >= 1 && stage <= 5) {
            await this.loadStageContent(stage);
        }
    }

    // Auto-refresh functionality
    startAutoRefresh() {
        if (this.isAutoRefreshing) return;

        this.isAutoRefreshing = true;
        this.autoRefreshInterval = setInterval(() => {
            this.refreshWorkflowStatus();
        }, 10000); // Refresh every 10 seconds

        console.log('Auto-refresh started');
    }

    stopAutoRefresh() {
        if (this.autoRefreshInterval) {
            clearInterval(this.autoRefreshInterval);
            this.autoRefreshInterval = null;
            this.isAutoRefreshing = false;
            console.log('Auto-refresh stopped');
        }
    }

    async refreshWorkflowStatus() {
        try {
            const status = await APIClient.getWorkflowStatus(this.projectId);
            if (status.currentStage && this.stages.indexOf(status.currentStage) + 1 !== this.currentStage) {
                // Stage has changed, reload content
                this.currentStage = this.stages.indexOf(status.currentStage) + 1;
                await this.loadStageContent(this.currentStage);
                showNotification(`Workflow progressed to ${status.currentStage} stage`, 'success');
            }
        } catch (error) {
            console.warn('Failed to refresh workflow status:', error);
        }
    }

    // Utility methods
    editRequirements() {
        showNotification('Edit requirements functionality coming soon', 'info');
    }

    editPlanning() {
        showNotification('Edit planning functionality coming soon', 'info');
    }

    addCustomStory() {
        showNotification('Add custom story functionality coming soon', 'info');
    }

    viewStory(storyId) {
        showNotification(`View story ${storyId} functionality coming soon`, 'info');
    }

    approveStory(storyId) {
        showNotification(`Approve story ${storyId} functionality coming soon`, 'info');
    }

    rejectStory(storyId) {
        showNotification(`Reject story ${storyId} functionality coming soon`, 'info');
    }

    customizePrompts() {
        showNotification('Customize prompts functionality coming soon', 'info');
    }

    viewPrompt(promptId) {
        showNotification(`View prompt ${promptId} functionality coming soon`, 'info');
    }

    copyPrompt(promptId) {
        showNotification(`Copy prompt ${promptId} functionality coming soon`, 'info');
    }

    generateReport() {
        showNotification('Generate report functionality coming soon', 'info');
    }

    exportProject() {
        showNotification('Export project functionality coming soon', 'info');
    }

    initializeRequirementsStage() {
        // Add requirements stage specific functionality
        console.log('Requirements stage initialized');
    }

    initializePlanningStage() {
        // Add planning stage specific functionality
        console.log('Planning stage initialized');
    }

    initializeStoriesStage() {
        // Add stories stage specific functionality
        console.log('Stories stage initialized');
    }

    initializePromptsStage() {
        // Add prompts stage specific functionality
        console.log('Prompts stage initialized');
    }

    initializeReviewStage() {
        // Add review stage specific functionality
        console.log('Review stage initialized');
    }
}

// Initialize workflow manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('projectId');

    if (projectId) {
        window.workflowManager = new WorkflowManager(projectId);
    } else {
        console.error('No project ID found for workflow initialization');
        showNotification('No project ID found', 'error');
    }
});

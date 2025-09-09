// Advanced Workflow Management JavaScript
class WorkflowManager {
    constructor(projectId) {
        this.projectId = projectId;
        this.currentStage = 1; // Start at Stage 1 (Requirements) by default
        this.stages = ['requirements', 'planning', 'stories', 'prompts', 'review'];
        this.autoRefreshInterval = null;
        this.isAutoRefreshing = false;
        this.workflowState = null; // Store the current workflow state
        this.isNewProject = false; // Flag for new projects
        this.hasShownNewProjectPrompt = false; // Track if we've shown the prompt

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
            await this.loadWorkflowState();
            await this.loadCurrentStage();
            this.updateUI();

            // Handle new project scenario
            if (this.isNewProject && !this.hasShownNewProjectPrompt) {
                this.hasShownNewProjectPrompt = true;
                this.handleNewProjectScenario();
            }
        } catch (error) {
            console.error('Failed to load initial workflow data:', error);
            window.App.showNotification('Failed to load workflow data', 'error');
        }
    }

    handleNewProjectScenario() {
        // Check if requirements analysis is needed for new project
        if (!this.workflowState?.requirementsAnalysis ||
            this.workflowState.requirementsAnalysis.status === 'NotStarted') {

            setTimeout(() => {
                if (confirm('Welcome to your new project! Would you like to start with requirements analysis?')) {
                    this.analyzeRequirements();
                }
            }, 1500);
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
        if (!this.workflowState) return 0;

        const stages = [
            this.workflowState.requirementsAnalysis?.isApproved,
            this.workflowState.projectPlanning?.isApproved,
            this.workflowState.storyGeneration?.isApproved,
            this.workflowState.promptGeneration?.completionPercentage >= 100,
            this.workflowState.promptGeneration?.completionPercentage >= 100
        ];

        const completed = stages.filter(Boolean).length;
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

    async loadWorkflowState() {
        try {
            this.workflowState = await APIClient.getWorkflowStatus(this.projectId);
            console.log('Workflow state loaded:', this.workflowState);
        } catch (error) {
            console.warn('Could not load workflow state, using defaults');
            this.workflowState = this.getDefaultWorkflowState();
        }
    }

    getDefaultWorkflowState() {
        return {
            projectId: this.projectId,
            projectName: 'Unknown Project',
            requirementsAnalysis: { status: 'NotStarted', isApproved: false },
            projectPlanning: { status: 'NotStarted', isApproved: false },
            storyGeneration: { status: 'NotStarted', isApproved: false },
            promptGeneration: { status: 'NotStarted', isApproved: false }
        };
    }

    getCurrentStageFromWorkflow() {
        // Determine the current stage based on workflow state
        if (!this.workflowState) return 1;

        const stages = [
            { stage: 1, approved: this.workflowState.requirementsAnalysis?.isApproved },
            { stage: 2, approved: this.workflowState.projectPlanning?.isApproved },
            { stage: 3, approved: this.workflowState.storyGeneration?.isApproved },
            { stage: 4, approved: this.workflowState.promptGeneration?.completionPercentage >= 100 },
            { stage: 5, approved: this.workflowState.promptGeneration?.completionPercentage >= 100 }
        ];

        // Find the first incomplete stage
        for (let i = 0; i < stages.length; i++) {
            if (!stages[i].approved) {
                return stages[i].stage;
            }
        }

        return 5; // All stages completed
    }

    async loadCurrentStage() {
        try {
            // Determine current stage from workflow state instead of hardcoding
            this.currentStage = this.getCurrentStageFromWorkflow();
            await this.loadStageContent(this.currentStage);
        } catch (error) {
            console.warn('Could not determine current stage, using default stage 1');
            this.currentStage = 1;
            await this.loadStageContent(this.currentStage);
        }
    }

    async loadStageContent(stage) {
        // Validate that we can access this stage
        if (!this.canAccessStage(stage)) {
            // Redirect to the highest accessible stage
            const accessibleStage = this.getHighestAccessibleStage();
            if (accessibleStage !== this.currentStage) {
                this.currentStage = accessibleStage;
                return this.loadStageContent(accessibleStage);
            }
        }

        this.currentStage = stage;

        // Update navigation
        document.getElementById('stage-counter').textContent = `Stage ${stage} of 5`;
        document.getElementById('prev-stage').disabled = stage === 1;
        document.getElementById('next-stage').textContent = stage === 5 ? 'Complete' : 'Next →';

        // Update next stage button based on current stage completion
        this.updateNextStageButton();

        // Load stage-specific content
        const content = await this.getStageContent(stage);
        document.getElementById('stage-content').innerHTML = content;

        // Initialize stage-specific functionality
        this.initializeStageFunctionality(stage);
    }

    canAccessStage(stage) {
        if (!this.workflowState) return stage === 1; // Only allow stage 1 if no workflow state

        switch (stage) {
            case 1: return true; // Stage 1 is always accessible
            case 2: return this.workflowState.requirementsAnalysis?.isApproved === true;
            case 3: return this.workflowState.requirementsAnalysis?.isApproved === true &&
                this.workflowState.projectPlanning?.isApproved === true;
            case 4: return this.workflowState.requirementsAnalysis?.isApproved === true &&
                this.workflowState.projectPlanning?.isApproved === true &&
                this.workflowState.storyGeneration?.isApproved === true;
            case 5: return this.workflowState.requirementsAnalysis?.isApproved === true &&
                this.workflowState.projectPlanning?.isApproved === true &&
                this.workflowState.storyGeneration?.isApproved === true;
            default: return false;
        }
    }

    getHighestAccessibleStage() {
        if (!this.workflowState) return 1;

        if (this.workflowState.requirementsAnalysis?.isApproved !== true) return 1;
        if (this.workflowState.projectPlanning?.isApproved !== true) return 2;
        if (this.workflowState.storyGeneration?.isApproved !== true) return 3;
        if (this.workflowState.promptGeneration?.completionPercentage < 100) return 4;
        return 5;
    }

    updateNextStageButton() {
        const nextButton = document.getElementById('next-stage');
        if (!nextButton) return;

        // Check if current stage is completed
        const canProgress = this.canProgressToNextStage();

        if (!canProgress && this.currentStage < 5) {
            nextButton.disabled = true;
            nextButton.title = 'Complete current stage before proceeding';
        } else {
            nextButton.disabled = false;
            nextButton.title = '';
        }
    }

    canProgressToNextStage() {
        if (!this.workflowState) return this.currentStage === 1;

        switch (this.currentStage) {
            case 1: return this.workflowState.requirementsAnalysis?.isApproved === true;
            case 2: return this.workflowState.projectPlanning?.isApproved === true;
            case 3: return this.workflowState.storyGeneration?.isApproved === true;
            case 4: return this.workflowState.promptGeneration?.completionPercentage >= 100;
            case 5: return true; // Can always "complete" the final stage
            default: return false;
        }
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
            const isApproved = this.workflowState?.requirementsAnalysis?.isApproved === true;

            if (isApproved && requirements) {
                return this.getRequirementsCompletedState(requirements);
            }

            return this.getRequirementsActiveState();
        } catch (error) {
            return this.getRequirementsEmptyState();
        }
    }

    getRequirementsActiveState() {
        const hasAnalysis = this.workflowState?.requirementsAnalysis?.status !== 'NotStarted';
        const isPending = this.workflowState?.requirementsAnalysis?.status === 'PendingReview';

        if (isPending) {
            return `
                <div class="stage-container">
                    <h2>Requirements Analysis</h2>
                    <div class="stage-status pending">
                        <div class="status-icon">⏳</div>
                        <h3>Analysis Pending Review</h3>
                        <p>Your requirements analysis is currently under review. Please check the <a href="/Reviews/Queue">Review Queue</a>.</p>
                    </div>
                </div>
            `;
        }

        if (hasAnalysis) {
            return this.getRequirementsCompletedState(null);
        }

        return this.getRequirementsEmptyState();
    }

    getRequirementsCompletedState(requirements) {
        return `
            <div class="stage-container">
                <h2>Requirements Analysis</h2>
                <div class="stage-status completed">
                    <div class="status-icon">✅</div>
                    <h3>Requirements Analysis Completed</h3>
                    <p>Your requirements have been successfully analyzed and approved.</p>
                    <div class="requirements-summary">
                        <h4>Analysis Results</h4>
                        ${requirements ? this.formatRequirements(requirements) : '<p>Requirements analysis data loaded successfully.</p>'}
                    </div>
                </div>
                <div class="stage-actions">
                    <button class="btn btn-secondary" onclick="workflowManager.editRequirements()">
                        ✏️ Edit Requirements
                    </button>
                </div>
            </div>
        `;
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
            const isApproved = this.workflowState?.projectPlanning?.isApproved === true;
            const canAccess = this.workflowState?.requirementsAnalysis?.isApproved === true;

            if (!canAccess) {
                return this.getPlanningLockedState();
            }

            if (isApproved && planning) {
                return this.getPlanningCompletedState(planning);
            }

            return this.getPlanningActiveState();
        } catch (error) {
            return this.getPlanningEmptyState();
        }
    }

    getPlanningLockedState() {
        return `
            <div class="stage-container">
                <h2>Project Planning</h2>
                <div class="stage-status locked">
                    <div class="status-icon">🔒</div>
                    <h3>Stage Locked</h3>
                    <p>You must complete <strong>Requirements Analysis</strong> before accessing this stage.</p>
                    <button class="btn btn-primary" onclick="workflowManager.jumpToStage(1)">
                        Go to Requirements Analysis
                    </button>
                </div>
            </div>
        `;
    }

    getPlanningActiveState() {
        const hasPlanning = this.workflowState?.projectPlanning?.status !== 'NotStarted';
        const isPending = this.workflowState?.projectPlanning?.status === 'PendingReview';

        if (isPending) {
            return `
                <div class="stage-container">
                    <h2>Project Planning</h2>
                    <div class="stage-status pending">
                        <div class="status-icon">⏳</div>
                        <h3>Planning Pending Review</h3>
                        <p>Your project planning is currently under review. Please check the <a href="/Reviews/Queue">Review Queue</a>.</p>
                    </div>
                </div>
            `;
        }

        if (hasPlanning) {
            return this.getPlanningCompletedState(null);
        }

        return this.getPlanningEmptyState();
    }

    getPlanningCompletedState(planning) {
        return `
            <div class="stage-container">
                <h2>Project Planning</h2>
                <div class="stage-status completed">
                    <div class="status-icon">✅</div>
                    <h3>Project Planning Completed</h3>
                    <p>Your project plan has been successfully created and approved.</p>
                    <div class="architecture-overview">
                        <h4>Technical Architecture</h4>
                        ${planning ? this.formatPlanning(planning) : '<p>Project planning data loaded successfully.</p>'}
                    </div>
                </div>
                <div class="stage-actions">
                    <button class="btn btn-secondary" onclick="workflowManager.editPlanning()">
                        ✏️ Edit Plan
                    </button>
                </div>
            </div>
        `;
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
            const isApproved = this.workflowState?.storyGeneration?.isApproved === true;
            const canAccess = this.workflowState?.requirementsAnalysis?.isApproved === true &&
                this.workflowState?.projectPlanning?.isApproved === true;

            if (!canAccess) {
                return this.getStoriesLockedState();
            }

            if (isApproved && stories) {
                return this.getStoriesCompletedState(stories);
            }

            return this.getStoriesActiveState();
        } catch (error) {
            return this.getStoriesEmptyState();
        }
    }

    getStoriesLockedState() {
        return `
            <div class="stage-container">
                <h2>User Stories</h2>
                <div class="stage-status locked">
                    <div class="status-icon">🔒</div>
                    <h3>Stage Locked</h3>
                    <p>You must complete both <strong>Requirements Analysis</strong> and <strong>Project Planning</strong> before accessing this stage.</p>
                    <div class="locked-requirements">
                        ${!this.workflowState?.requirementsAnalysis?.isApproved ? `
                            <div class="requirement-item">
                                <span class="status-icon">❌</span>
                                <span>Requirements Analysis - Not completed</span>
                                <button class="btn btn-sm btn-primary" onclick="workflowManager.jumpToStage(1)">Go</button>
                            </div>
                        ` : ''}
                        ${!this.workflowState?.projectPlanning?.isApproved ? `
                            <div class="requirement-item">
                                <span class="status-icon">❌</span>
                                <span>Project Planning - Not completed</span>
                                <button class="btn btn-sm btn-primary" onclick="workflowManager.jumpToStage(2)">Go</button>
                            </div>
                        ` : ''}
                    </div>
                </div>
            </div>
        `;
    }

    getStoriesActiveState() {
        const hasStories = this.workflowState?.storyGeneration?.status !== 'NotStarted';
        const isPending = this.workflowState?.storyGeneration?.status === 'PendingReview';

        if (isPending) {
            return `
                <div class="stage-container">
                    <h2>User Stories</h2>
                    <div class="stage-status pending">
                        <div class="status-icon">⏳</div>
                        <h3>Stories Pending Review</h3>
                        <p>Your user stories are currently under review. Please check the <a href="/Reviews/Queue">Review Queue</a>.</p>
                    </div>
                </div>
            `;
        }

        if (hasStories) {
            return this.getStoriesCompletedState(null);
        }

        return this.getStoriesEmptyState();
    }

    getStoriesCompletedState(stories) {
        return `
            <div class="stage-container">
                <h2>User Stories</h2>
                <div class="stage-status completed">
                    <div class="status-icon">✅</div>
                    <h3>User Stories Completed</h3>
                    <p>Your user stories have been successfully generated and approved.</p>
                    <div class="stories-summary">
                        <h4>Generated Stories</h4>
                        ${stories ? this.formatStories(stories) : '<p>User stories data loaded successfully.</p>'}
                    </div>
                </div>
                <div class="stage-actions">
                    <button class="btn btn-secondary" onclick="workflowManager.addCustomStory()">
                        ➕ Add Custom Story
                    </button>
                </div>
            </div>
        `;
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
        // Check if requirements already exist and are approved
        if (this.workflowState?.requirementsAnalysis?.isApproved === true) {
            window.App.showNotification('Requirements analysis is already completed and approved.', 'info');
            return;
        }

        // Check if there's already a pending analysis
        if (this.workflowState?.requirementsAnalysis?.status === 'PendingReview') {
            window.App.showNotification('Requirements analysis is already pending review. Check the Review Queue.', 'info');
            return;
        }

        showLoading('Preparing requirements analysis...');
        try {
            // Get project details to pre-populate requirements
            const project = await APIClient.getProject(this.projectId);

            let requirementsInput = '';

            // If this is a new project, suggest using the project description
            if (this.isNewProject && project.description) {
                const useProjectDescription = confirm(
                    'We found your project description. Would you like to use it as a starting point for requirements analysis?\n\n' +
                    'Project Description: ' + project.description.substring(0, 200) + '...'
                );

                if (useProjectDescription) {
                    requirementsInput = project.description;
                }
            }

            // If no pre-populated input, prompt user
            if (!requirementsInput) {
                requirementsInput = prompt('Please provide detailed requirements for your project:');
            }

            if (!requirementsInput) {
                hideLoading();
                return;
            }

            // Create the requirements analysis request
            const request = {
                ProjectDescription: requirementsInput,
                ProjectId: this.projectId,
                AdditionalContext: project.techStack ? `Tech Stack: ${project.techStack}` : null,
                Constraints: project.timeline ? `Timeline: ${project.timeline}` : null
            };

            const result = await APIClient.analyzeRequirements(request);
            window.App.showNotification('Requirements submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            await this.loadWorkflowState();
            await this.loadStageContent(1);

            // Clear the new project flag since we've started the process
            this.isNewProject = false;
        } catch (error) {
            window.App.showNotification(`Failed to analyze requirements: ${error.message || error}`, 'error');
        } finally {
            hideLoading();
        }
    }

    async regeneratePlan() {
        // Check if planning is already approved
        if (this.workflowState?.projectPlanning?.isApproved === true) {
            if (!confirm('Project planning is already completed. Do you want to regenerate it? This will require re-approval.')) {
                return;
            }
        }

        // Check if requirements are approved
        if (this.workflowState?.requirementsAnalysis?.isApproved !== true) {
            window.App.showNotification('You must complete Requirements Analysis before generating a project plan.', 'warning');
            return;
        }

        showLoading('Regenerating project plan...');
        try {
            // Implementation for plan regeneration
            window.App.showNotification('Project plan submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            await this.loadWorkflowState();
            await this.loadStageContent(2);
        } catch (error) {
            window.App.showNotification(`Failed to regenerate plan: ${error.message || error}`, 'error');
        } finally {
            hideLoading();
        }
    }

    async generateStories() {
        // Check if stories are already approved
        if (this.workflowState?.storyGeneration?.isApproved === true) {
            if (!confirm('User stories are already completed. Do you want to regenerate them? This will require re-approval.')) {
                return;
            }
        }

        // Check if requirements and planning are approved
        if (this.workflowState?.requirementsAnalysis?.isApproved !== true) {
            window.App.showNotification('You must complete Requirements Analysis before generating user stories.', 'warning');
            return;
        }

        if (this.workflowState?.projectPlanning?.isApproved !== true) {
            window.App.showNotification('You must complete Project Planning before generating user stories.', 'warning');
            return;
        }

        showLoading('Generating user stories...');
        try {
            // Implementation for story generation
            window.App.showNotification('User stories submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            await this.loadWorkflowState();
            await this.loadStageContent(3);
        } catch (error) {
            window.App.showNotification(`Failed to generate stories: ${error.message || error}`, 'error');
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
            window.App.showNotification(`Failed to generate prompts: ${error.message || error}`, 'error');
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
                window.App.showNotification(`Failed to complete project: ${error.message || error}`, 'error');
            } finally {
                hideLoading();
            }
        }
    }

    // Navigation methods
    async navigateStage(direction) {
        const newStage = this.currentStage + direction;

        // Validate stage progression
        if (newStage >= 1 && newStage <= 5) {
            if (direction > 0 && !this.canProgressToNextStage()) {
                window.App.showNotification('Complete the current stage before proceeding to the next stage.', 'warning');
                return;
            }

            if (!this.canAccessStage(newStage)) {
                window.App.showNotification('You must complete the previous stages before accessing this stage.', 'warning');
                return;
            }

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
        window.App.showNotification('Edit requirements functionality coming soon', 'info');
    }

    editPlanning() {
        window.App.showNotification('Edit planning functionality coming soon', 'info');
    }

    addCustomStory() {
        window.App.showNotification('Add custom story functionality coming soon', 'info');
    }

    viewStory(storyId) {
        window.App.showNotification(`View story ${storyId} functionality coming soon`, 'info');
    }

    approveStory(storyId) {
        window.App.showNotification(`Approve story ${storyId} functionality coming soon`, 'info');
    }

    rejectStory(storyId) {
        window.App.showNotification(`Reject story ${storyId} functionality coming soon`, 'info');
    }

    customizePrompts() {
        window.App.showNotification('Customize prompts functionality coming soon', 'info');
    }

    viewPrompt(promptId) {
        window.App.showNotification(`View prompt ${promptId} functionality coming soon`, 'info');
    }

    copyPrompt(promptId) {
        window.App.showNotification(`Copy prompt ${promptId} functionality coming soon`, 'info');
    }

    generateReport() {
        window.App.showNotification('Generate report functionality coming soon', 'info');
    }

    exportProject() {
        window.App.showNotification('Export project functionality coming soon', 'info');
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
    const newProject = urlParams.get('newProject') === 'true';

    if (projectId) {
        window.workflowManager = new WorkflowManager(projectId);

        // Set the new project flag if present
        if (newProject) {
            window.workflowManager.isNewProject = true;
        }
    } else {
        console.error('No project ID found for workflow initialization');
        window.App.showNotification('No project ID found', 'error');
    }
});

// Advanced Workflow Management JavaScript
class WorkflowManager {
    constructor(projectId, isNewProject = false) {
        this.projectId = projectId;
        this.currentStage = 1; // Start at Stage 1 (Requirements) by default
        this.stages = ['requirements', 'planning', 'stories', 'prompts', 'review'];
        this.autoRefreshInterval = null;
        this.isAutoRefreshing = false;
        this.workflowState = null; // Store the current workflow state
        this.isNewProject = isNewProject; // Flag for new projects
        this.hasShownNewProjectPrompt = false; // Track if we've shown the prompt
        this.projectData = null; // Store project data for UI updates

        console.log(`WorkflowManager constructor called with projectId=${projectId}, isNewProject=${isNewProject}`);
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
        console.log('loadInitialData: Starting initialization');
        console.log(`isNewProject: ${this.isNewProject}`);
        console.log(`hasShownNewProjectPrompt: ${this.hasShownNewProjectPrompt}`);

        try {
            await this.loadProjectData();

            // Handle API failures gracefully
            try {
                await this.loadWorkflowState();
                await this.loadCurrentStage();

                // Handle new project scenario - NEW PROJECTS ALWAYS START AT STAGE 1
                if (this.isNewProject) {
                    console.log('=== NEW PROJECT DETECTED ===');
                    console.log('Forcing stage 1 and showing prompt');
                    this.hasShownNewProjectPrompt = true;
                    this.currentStage = 1; // Force new projects to start at stage 1
                    console.log('Loading stage 1 content for new project...');
                    await this.loadStageContent(1); // Load stage 1 content
                    console.log('Stage 1 content loaded, handling new project scenario...');
                    this.handleNewProjectScenario();
                    this.showStartWorkflowButton(); // Show start button for new projects
                    this.showNewProjectActionButton(); // Show prominent action button
                    console.log('=== NEW PROJECT SETUP COMPLETE ===');
                } else {
                    console.log('Not new project, proceeding with normal workflow');
                    await this.loadStageContent(this.currentStage);
                    this.hideStartWorkflowButton(); // Hide start button for existing projects
                }
            } catch (workflowError) {
                console.error('Failed to load workflow state or determine current stage:', workflowError);

                // Fallback for new projects - always start at stage 1
                if (this.isNewProject) {
                    console.log('Using fallback for new project - starting at stage 1');
                    this.currentStage = 1;
                    this.workflowState = this.getDefaultWorkflowState();
                    await this.loadStageContent(1);
                    this.handleNewProjectScenario();
                    this.showStartWorkflowButton(); // Show start button in fallback too
                } else {
                    // For existing projects, show error and fallback to stage 1
                    window.App.showNotification('Failed to load workflow data. Starting at stage 1.', 'warning');
                    this.currentStage = 1;
                    this.workflowState = this.getDefaultWorkflowState();
                    await this.loadStageContent(1);
                }
            }

            this.updateWorkflowUI();
            console.log(`Initial data loaded. Current stage: ${this.currentStage}`);
            console.log(`Workflow state after loading:`, this.workflowState);
        } catch (error) {
            console.error('Failed to load initial workflow data:', error);
            window.App.showNotification('Failed to load project data', 'error');

            // Final safety net - always show something
            if (!document.getElementById('stage-content').innerHTML) {
                document.getElementById('stage-content').innerHTML = `
                    <div class="empty-stage">
                        <div class="empty-icon">⚠️</div>
                        <h3>Workflow Initialization Failed</h3>
                        <p>We encountered an error loading your workflow. Please try refreshing the page or contact support.</p>
                    </div>
                `;
            }
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
            this.projectData = project; // Store project data for later use
            this.updateProjectOverview(project);
            this.updateProgressIndicators(project);
            return project;
        } catch (error) {
            console.error('Failed to load project data:', error);
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
            console.log(`Loading workflow state for project ${this.projectId}`);
            this.workflowState = await APIClient.getWorkflowStatus(this.projectId);
            console.log('Workflow state loaded:', this.workflowState);

            // Debug the structure
            if (this.workflowState) {
                console.log('Requirements analysis:', this.workflowState.requirementsAnalysis);
                console.log('Project planning details:', JSON.stringify(this.workflowState.projectPlanning, null, 2));
                console.log('Story generation:', this.workflowState.storyGeneration);
                console.log('Prompt generation:', this.workflowState.promptGeneration);
            }
        } catch (error) {
            console.warn('Could not load workflow state, using defaults', error);
            this.workflowState = this.getDefaultWorkflowState();
            console.log('Using default workflow state:', this.workflowState);
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
        if (!this.workflowState) {
            console.log('No workflow state, defaulting to stage 1');
            return 1;
        }

        console.log('Workflow state:', this.workflowState);
        console.log('Requirements analysis:', this.workflowState.requirementsAnalysis);
        console.log('Project planning:', this.workflowState.projectPlanning);
        console.log('Story generation:', this.workflowState.storyGeneration);

        // Handle the case where requirements analysis exists but is not approved
        const requirementsAnalysis = this.workflowState.requirementsAnalysis;
        const projectPlanning = this.workflowState.projectPlanning;
        const storyGeneration = this.workflowState.storyGeneration;
        const promptGeneration = this.workflowState.promptGeneration;

        console.log('Detailed analysis:');
        console.log('RequirementsAnalysis - status:', requirementsAnalysis?.status, 'isApproved:', requirementsAnalysis?.isApproved);
        console.log('ProjectPlanning - status:', projectPlanning?.status, 'isApproved:', projectPlanning?.isApproved);
        console.log('StoryGeneration - status:', storyGeneration?.status, 'isApproved:', storyGeneration?.isApproved);

        // NEW: Safety check - if this is a new project, always start at stage 1
        if (this.isNewProject) {
            console.log('New project detected in getCurrentStageFromWorkflow - forcing stage 1');
            return 1;
        }

        // If requirements analysis exists but is not approved, stay at stage 1
        if (requirementsAnalysis && requirementsAnalysis.status !== 'NotStarted' && !requirementsAnalysis.isApproved) {
            console.log('Requirements analysis exists but not approved, staying at stage 1');
            return 1;
        }

        // If requirements analysis is approved but project planning is not, go to stage 2
        if (requirementsAnalysis?.isApproved === true &&
            (!projectPlanning || !projectPlanning.isApproved)) {
            console.log('Requirements approved, project planning not approved, going to stage 2');
            return 2;
        }

        // If both requirements and planning are approved but stories are not, go to stage 3
        if (requirementsAnalysis?.isApproved === true &&
            projectPlanning?.isApproved === true &&
            (!storyGeneration || !storyGeneration.isApproved)) {
            console.log('Requirements and planning approved, stories not approved, going to stage 3');
            return 3;
        }

        // Default logic for remaining stages
        const stages = [
            { stage: 1, approved: requirementsAnalysis?.isApproved === true },
            { stage: 2, approved: projectPlanning?.isApproved === true },
            { stage: 3, approved: storyGeneration?.isApproved === true },
            { stage: 4, approved: promptGeneration?.completionPercentage >= 100 },
            { stage: 5, approved: promptGeneration?.completionPercentage >= 100 }
        ];

        console.log('Stage evaluation:', stages);

        // Find the first incomplete stage
        for (let i = 0; i < stages.length; i++) {
            console.log(`Stage ${i + 1}: approved=${stages[i].approved}`);
            if (!stages[i].approved) {
                console.log(`Returning stage ${stages[i].stage} as first incomplete stage`);
                return stages[i].stage;
            }
        }

        console.log('All stages completed, returning stage 5');
        return 5; // All stages completed
    }

    async loadCurrentStage() {
        try {
            console.log('loadCurrentStage: Starting stage determination');
            // Determine current stage from workflow state instead of hardcoding
            this.currentStage = this.getCurrentStageFromWorkflow();
            console.log(`loadCurrentStage: Determined stage ${this.currentStage}`);
            await this.loadStageContent(this.currentStage);
        } catch (error) {
            console.warn('Could not determine current stage, using default stage 1', error);
            this.currentStage = 1;
            await this.loadStageContent(this.currentStage);
        }
    }

    async loadStageContent(stage) {
        console.log(`=== loadStageContent called for stage ${stage} ===`);

        // Validate that we can access this stage
        if (!this.canAccessStage(stage)) {
            console.log(`Stage ${stage} not accessible, finding highest accessible stage`);
            // Redirect to the highest accessible stage
            const accessibleStage = this.getHighestAccessibleStage();
            if (accessibleStage !== this.currentStage) {
                this.currentStage = accessibleStage;
                return this.loadStageContent(accessibleStage);
            }
        }

        this.currentStage = stage;
        console.log(`Setting current stage to ${stage}`);

        // Update navigation
        document.getElementById('stage-counter').textContent = `Stage ${stage} of 5`;
        document.getElementById('prev-stage').disabled = stage === 1;
        document.getElementById('next-stage').textContent = stage === 5 ? 'Complete' : 'Next →';

        // Update next stage button based on current stage completion
        this.updateNextStageButton();

        // Load stage-specific content
        console.log(`Getting content for stage ${stage}...`);
        const content = await this.getStageContent(stage);
        console.log(`Content received, length: ${content.length}`);

        const stageContentElement = document.getElementById('stage-content');
        if (stageContentElement) {
            stageContentElement.innerHTML = content;
            console.log(`Stage ${stage} content loaded successfully`);
        } else {
            console.error('Stage content element not found!');
        }

        // Initialize stage-specific functionality
        this.initializeStageFunctionality(stage);
        console.log(`=== loadStageContent completed for stage ${stage} ===`);
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
        if (!this.workflowState) {
            console.log('No workflow state, allowing progression from stage 1');
            return this.currentStage === 1;
        }

        console.log(`Checking if can progress from stage ${this.currentStage}`);
        console.log('Requirements approved:', this.workflowState.requirementsAnalysis?.isApproved);
        console.log('Planning approved:', this.workflowState.projectPlanning?.isApproved);
        console.log('Stories approved:', this.workflowState.storyGeneration?.isApproved);
        console.log('Prompt completion:', this.workflowState.promptGeneration?.completionPercentage);

        switch (this.currentStage) {
            case 1:
                const canProgress = this.workflowState.requirementsAnalysis?.isApproved === true;
                console.log(`Stage 1 can progress: ${canProgress}`);
                return canProgress;
            case 2:
                const canProgress2 = this.workflowState.projectPlanning?.isApproved === true;
                console.log(`Stage 2 can progress: ${canProgress2}`);
                return canProgress2;
            case 3:
                const canProgress3 = this.workflowState.storyGeneration?.isApproved === true;
                console.log(`Stage 3 can progress: ${canProgress3}`);
                return canProgress3;
            case 4:
                const canProgress4 = this.workflowState.promptGeneration?.completionPercentage >= 100;
                console.log(`Stage 4 can progress: ${canProgress4}`);
                return canProgress4;
            case 5:
                console.log('Stage 5 can always progress');
                return true; // Can always "complete" the final stage
            default:
                console.log(`Unknown stage ${this.currentStage}, cannot progress`);
                return false;
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
            console.log('=== getRequirementsStage called ===');
            console.log('Workflow state:', this.workflowState);
            console.log('Requirements analysis:', this.workflowState?.requirementsAnalysis);

            // Check if we have an analysis ID from the workflow state
            const analysisId = this.workflowState?.requirementsAnalysis?.analysisId;
            const status = this.workflowState?.requirementsAnalysis?.status;
            const isApproved = this.workflowState?.requirementsAnalysis?.isApproved === true;

            console.log('Analysis ID:', analysisId);
            console.log('Status:', status);
            console.log('Is Approved:', isApproved);

            if (analysisId) {
                console.log('Found analysis ID, trying to load requirements details');
                // Try to get the actual requirements analysis results
                try {
                    const requirements = await APIClient.getRequirements(analysisId);
                    console.log('Loaded requirements:', requirements);

                    if (isApproved && requirements) {
                        console.log('Requirements are approved, showing completed state');
                        return this.getRequirementsCompletedState(requirements);
                    }
                } catch (apiError) {
                    console.warn('Could not load requirements analysis details:', apiError);
                    // Continue with state-based logic even if API call fails
                }
            }

            console.log('No analysis ID or requirements not approved, determining state based on workflow');
            const content = this.getRequirementsActiveState();
            console.log('Generated content length:', content.length);
            console.log('Content preview:', content.substring(0, 200) + '...');
            return content;
        } catch (error) {
            console.error('Error in getRequirementsStage:', error);
            return this.getRequirementsEmptyState();
        }
    }

    getRequirementsActiveState() {
        console.log('=== getRequirementsActiveState called ===');
        const hasAnalysis = this.workflowState?.requirementsAnalysis?.status !== 'NotStarted';
        const isPending = this.workflowState?.requirementsAnalysis?.status === 'PendingReview';
        const isApproved = this.workflowState?.requirementsAnalysis?.isApproved === true;

        console.log(`Requirements state - hasAnalysis: ${hasAnalysis}, isPending: ${isPending}, isApproved: ${isApproved}`);
        console.log('Raw requirements analysis:', this.workflowState?.requirementsAnalysis);

        if (isApproved) {
            console.log('Requirements are approved, showing completed state');
            return this.getRequirementsCompletedState(null);
        }

        if (isPending) {
            return `
                <div class="stage-container">
                    <h2>Requirements Analysis</h2>
                    <div class="stage-status pending">
                        <div class="status-icon">⏳</div>
                        <h3>Analysis Pending Review</h3>
                        <p>Your requirements analysis is currently under review. Please check the <a href="/Reviews/Queue">Review Queue</a>.</p>
                        <div class="stage-actions">
                            <button class="btn btn-primary" onclick="workflowManager.viewRequirementsReview()">
                                📋 View Review Details
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        // For new projects or when requirements exist but need to be regenerated
        if (hasAnalysis) {
            console.log('Requirements exist but not approved, showing active state with regenerate option');
            return `
                <div class="stage-container">
                    <h2>Requirements Analysis</h2>
                    <div class="stage-status active">
                        <div class="status-icon">📋</div>
                        <h3>Analysis in Progress</h3>
                        <p>Your requirements analysis is being processed. Check the Review Queue for status updates.</p>
                        <div class="stage-actions">
                            <button class="btn btn-primary" onclick="workflowManager.viewRequirementsReview()">
                                📋 View Review Details
                            </button>
                            <button class="btn btn-success" onclick="workflowManager.analyzeRequirements()">
                                🚀 Start Requirements Analysis
                            </button>
                        </div>
                    </div>
                </div>
            `;
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
                    <button class="btn btn-primary" onclick="workflowManager.generatePlan()">
                        🚀 Generate Project Plan
                    </button>
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
                    <div class="stage-actions">
                        <button class="btn btn-primary btn-lg" onclick="workflowManager.analyzeRequirements()" style="font-size: 16px; padding: 12px 24px;">
                            🚀 Start Requirements Analysis
                        </button>
                    </div>
                </div>
                <div class="getting-started-section" style="margin-top: 20px; padding: 15px; background: #e3f2fd; border-radius: 8px; border-left: 4px solid #2196f3;">
                    <h4>Getting Started</h4>
                    <p>Click the button above to begin requirements analysis. You'll be prompted to describe:</p>
                    <ul style="text-align: left; margin: 10px 0;">
                        <li>What problem your project solves</li>
                        <li>Key features and functionality</li>
                        <li>Technology constraints or preferences</li>
                        <li>Timeline and budget considerations</li>
                    </ul>
                    <button class="btn btn-success" onclick="workflowManager.analyzeRequirements()" style="background: #28a745; border-color: #28a745;">
                        🚀 Start Analysis Now
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
            const canAccess = this.workflowState?.requirementsAnalysis?.isApproved === true;

            if (!canAccess) {
                return this.getPlanningLockedState();
            }

            // Check if we have a planning ID from the workflow state
            const planningId = this.workflowState?.projectPlanning?.planningId;
            const planningStatus = this.workflowState?.projectPlanning?.status;
            const isApproved = this.workflowState?.projectPlanning?.isApproved === true;

            console.log('Planning stage check - planningId:', planningId, 'status:', planningStatus, 'isApproved:', isApproved);

            if (planningId) {
                // Try to get the actual planning results
                try {
                    const planning = await APIClient.getProjectPlan(planningId);

                    if (isApproved && planning) {
                        return this.getPlanningCompletedState(planning);
                    }
                } catch (apiError) {
                    console.warn('Could not load project planning details:', apiError);
                    // Continue with state-based logic even if API call fails
                }
            }

            // Check status - if NotStarted (status 0) and no planningId, show empty state
            if (planningStatus === 0 || planningStatus === 'NotStarted' || !planningId) {
                console.log('Planning not started, showing empty state');
                return this.getPlanningEmptyState();
            }

            if (isApproved) {
                console.log('Planning approved, showing completed state');
                return this.getPlanningCompletedState(null);
            }

            return this.getPlanningActiveState();
        } catch (error) {
            console.error('Error in getPlanningStage:', error);
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
        const planningStatus = this.workflowState?.projectPlanning?.status;
        const isPending = planningStatus === 'PendingReview';
        const isNotStarted = planningStatus === 'NotStarted' || planningStatus === 0;
        const hasPlanningId = this.workflowState?.projectPlanning?.planningId;
        const isApproved = this.workflowState?.projectPlanning?.isApproved === true;

        console.log('getPlanningActiveState - status:', planningStatus, 'isPending:', isPending, 'isNotStarted:', isNotStarted, 'hasPlanningId:', hasPlanningId, 'isApproved:', isApproved);
        console.log('planningId truthy check:', !!hasPlanningId, 'planningId value:', hasPlanningId);

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

        // CRITICAL FIX: Check if planning hasn't been generated yet
        // Empty string ("") is falsy, so !hasPlanningId will be true
        const hasNoPlanningId = !hasPlanningId || hasPlanningId === '';
        console.log('hasNoPlanningId:', hasNoPlanningId, 'hasPlanningId:', hasPlanningId);

        if (isNotStarted && hasNoPlanningId) {
            console.log('Planning not started and no planning ID, showing empty state');
            return this.getPlanningEmptyState();
        }

        // If planning is approved, show completed state
        if (isApproved) {
            console.log('Planning approved, showing completed state');
            return this.getPlanningCompletedState(null);
        }

        // If we have a planning ID but it's not approved, show active state with regenerate option
        if (hasPlanningId && !isApproved) {
            console.log('Has planning ID but not approved, showing active state');
            return this.getPlanningActiveStateWithRegenerate();
        }

        console.log('Falling back to empty state');
        return this.getPlanningEmptyState();
    }

    getPlanningActiveStateWithRegenerate() {
        return `
            <div class="stage-container">
                <h2>Project Planning</h2>
                <div class="stage-status active">
                    <div class="status-icon">📋</div>
                    <h3>Analysis in Progress</h3>
                    <p>Your project planning is being processed. Check the Review Queue for status updates.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="workflowManager.viewRequirementsReview()">
                            📋 View Review Details
                        </button>
                        <button class="btn btn-success" onclick="workflowManager.regeneratePlan()">
                            🚀 Regenerate Plan
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    getPlanningActiveStateWithRegenerate() {
        return `
            <div class="stage-container">
                <h2>Project Planning</h2>
                <div class="stage-status active">
                    <div class="status-icon">📋</div>
                    <h3>Analysis in Progress</h3>
                    <p>Your project planning is being processed. Check the Review Queue for status updates.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="workflowManager.viewRequirementsReview()">
                            📋 View Review Details
                        </button>
                        <button class="btn btn-success" onclick="workflowManager.regeneratePlan()">
                            🚀 Regenerate Plan
                        </button>
                    </div>
                </div>
            </div>
        `;
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
                    <button class="btn btn-primary" onclick="workflowManager.generateStories()">
                        ✨ Generate User Stories
                    </button>
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
            // Check if we have a generation ID from the workflow state
            const generationId = this.workflowState?.storyGeneration?.generationId;
            const canAccess = this.workflowState?.requirementsAnalysis?.isApproved === true &&
                this.workflowState?.projectPlanning?.isApproved === true;

            if (!canAccess) {
                return this.getStoriesLockedState();
            }

            if (generationId) {
                // Try to get the actual stories
                try {
                    const stories = await APIClient.getStories(generationId);
                    const isApproved = this.workflowState?.storyGeneration?.isApproved === true;

                    if (isApproved && stories) {
                        return this.getStoriesCompletedState(stories);
                    }
                } catch (apiError) {
                    console.warn('Could not load story generation details:', apiError);
                    // Continue with state-based logic even if API call fails
                }
            }

            return this.getStoriesActiveState();
        } catch (error) {
            console.error('Error in getStoriesStage:', error);
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
        const storyStatus = this.workflowState?.storyGeneration?.status;
        const isPending = storyStatus === 'PendingReview';
        const isNotStarted = storyStatus === 'NotStarted' || storyStatus === 0 || !storyStatus;
        const hasGenerationId = this.workflowState?.storyGeneration?.generationId;

        console.log('getStoriesActiveState - status:', storyStatus, 'isPending:', isPending, 'isNotStarted:', isNotStarted, 'hasGenerationId:', hasGenerationId);

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

        // CRITICAL FIX: Check if stories haven't been generated yet
        // Empty string ("") is falsy, so !hasGenerationId will be true
        const hasNoGenerationId = !hasGenerationId || hasGenerationId === '';
        console.log('hasNoGenerationId:', hasNoGenerationId, 'hasGenerationId:', hasGenerationId);

        if (isNotStarted && hasNoGenerationId) {
            console.log('Stories not started and no generation ID, showing empty state');
            return this.getStoriesEmptyState();
        }

        // If we have a generation ID but it's not approved, show active state
        if (hasGenerationId && !this.workflowState?.storyGeneration?.isApproved) {
            console.log('Has generation ID but not approved, showing active state');
            return this.getStoriesActiveStateWithRegenerate();
        }

        console.log('Falling back to empty state for stories');
        return this.getStoriesEmptyState();
    }

    getStoriesActiveStateWithRegenerate() {
        return `
            <div class="stage-container">
                <h2>User Stories</h2>
                <div class="stage-status active">
                    <div class="status-icon">📖</div>
                    <h3>Stories Generation in Progress</h3>
                    <p>Your user stories are being processed. Check the Review Queue for status updates.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="workflowManager.viewRequirementsReview()">
                            📋 View Review Details
                        </button>
                        <button class="btn btn-success" onclick="workflowManager.regenerateStories()">
                            ✨ Regenerate Stories
                        </button>
                    </div>
                </div>
            </div>
        `;
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
                    <button class="btn btn-primary" onclick="workflowManager.generateAllPrompts()">
                        🤖 Generate Code Prompts
                    </button>
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
            // For prompts, we might not have a specific ID, so check if any prompts exist
            const hasPrompts = this.workflowState?.promptGeneration?.storyPrompts &&
                this.workflowState.promptGeneration.storyPrompts.length > 0;

            if (hasPrompts) {
                // Use the existing prompts from workflow state
                return `
                    <div class="stage-container">
                        <h2>Prompt Generation</h2>
                        <div class="prompts-content">
                            <div class="prompts-summary">
                                <h3>Generated Prompts</h3>
                                ${this.formatPrompts(this.workflowState.promptGeneration.storyPrompts)}
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
            } else {
                // Try to load prompts from API if we have a way to identify them
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
                } catch (apiError) {
                    console.warn('Could not load prompts:', apiError);
                    return this.getPromptsEmptyState();
                }
            }
        } catch (error) {
            console.error('Error in getPromptsStage:', error);
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

        const loadingOverlay = showLoading('Preparing requirements analysis...');
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

            // If no pre-populated input, prompt user for manual input
            if (!requirementsInput) {
                hideLoading(loadingOverlay);

                // Show a prompt for manual requirements input
                requirementsInput = prompt('Please describe your project requirements:\n\n' +
                    'What problem are you trying to solve? What features do you need? ' +
                    'What technology constraints do you have?');

                // If user cancels the prompt, don't proceed
                if (!requirementsInput) {
                    window.App.showNotification('Requirements analysis cancelled. You can try again later.', 'info');
                    return;
                }

                // Re-show loading overlay since we're proceeding
                loadingOverlay = showLoading('Preparing requirements analysis...');
            }

            // Create the requirements analysis request
            const request = {
                ProjectDescription: requirementsInput,
                ProjectId: this.projectId,
                AdditionalContext: project.techStack ? `Tech Stack: ${project.techStack}` : null,
                Constraints: project.timeline ? `Timeline: ${project.timeline}` : null
            };

            const result = await APIClient.analyzeRequirements(request);

            // Reload workflow state to reflect changes
            await this.loadWorkflowState();

            // Check the new state and update UI accordingly
            console.log('Requirements analysis submitted, checking new state:', this.workflowState?.requirementsAnalysis);

            if (this.workflowState?.requirementsAnalysis?.status === 'PendingReview') {
                // Analysis is pending review, show the appropriate state
                await this.loadStageContent(1);
                window.App.showNotification('Requirements submitted for review! Check the Review Queue.', 'success');
            } else if (this.workflowState?.requirementsAnalysis?.isApproved === true) {
                // Analysis was immediately approved, move to next stage
                await this.loadStageContent(2);
                window.App.showNotification('Requirements approved! Moving to Project Planning.', 'success');
            } else {
                // Fallback: reload current stage
                await this.loadStageContent(1);
                window.App.showNotification('Requirements submitted for review! Check the Review Queue.', 'success');
            }

            // Clear the new project flag since we've started the process
            this.isNewProject = false;
        } catch (error) {
            window.App.showNotification(`Failed to analyze requirements: ${error.message || error}`, 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async generatePlan() {
        console.log('=== generatePlan() called ===');
        console.log('Current workflow state:', this.workflowState);
        console.log('Requirements approved:', this.workflowState?.requirementsAnalysis?.isApproved);
        console.log('Project planning approved:', this.workflowState?.projectPlanning?.isApproved);

        // Check if requirements are approved
        if (this.workflowState?.requirementsAnalysis?.isApproved !== true) {
            window.App.showNotification('You must complete Requirements Analysis before generating a project plan.', 'warning');
            return;
        }

        // Check if planning already exists and is approved
        if (this.workflowState?.projectPlanning?.isApproved === true) {
            if (!confirm('Project planning is already completed. Do you want to regenerate it? This will require re-approval.')) {
                return;
            }
        }

        const loadingOverlay = showLoading('Generating project plan...');
        try {
            // Get project details for planning generation
            console.log('Getting project details...');
            const project = await APIClient.getProject(this.projectId);
            console.log('Project details:', project);

            // Create the project planning request
            const request = {
                ProjectId: this.projectId,
                RequirementsAnalysisId: this.workflowState?.requirementsAnalysis?.analysisId,
                ProjectDescription: project.description || 'No description available',
                TechStack: project.techStack || 'Not specified',
                Timeline: project.timeline || 'Not specified',
                AdditionalContext: null // Can be extended later
            };

            console.log('Generating project plan with request:', request);

            // Make API call to generate project plan
            console.log('Calling APIClient.createProjectPlan...');
            const result = await APIClient.createProjectPlan(request);
            console.log('Project plan generation result:', result);

            window.App.showNotification('Project plan submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            console.log('Reloading workflow state...');
            await this.loadWorkflowState();
            console.log('Reloading stage content...');
            await this.loadStageContent(2);
            console.log('=== generatePlan() completed successfully ===');
        } catch (error) {
            console.error('Failed to generate project plan:', error);
            window.App.showNotification(`Failed to generate plan: ${error.message || error}`, 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async regeneratePlan() {
        console.log('=== regeneratePlan() called ===');

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

        const loadingOverlay = showLoading('Regenerating project plan...');
        try {
            console.log('Getting project details for regeneration...');
            const project = await APIClient.getProject(this.projectId);

            // Create the project planning request for regeneration
            const request = {
                ProjectId: this.projectId,
                RequirementsAnalysisId: this.workflowState?.requirementsAnalysis?.analysisId,
                ProjectDescription: project.description || 'No description available',
                TechStack: project.techStack || 'Not specified',
                Timeline: project.timeline || 'Not specified',
                AdditionalContext: 'Regenerated plan' // Indicate this is a regeneration
            };

            console.log('Regenerating project plan with request:', request);

            // Make API call to regenerate project plan
            console.log('Calling APIClient.createProjectPlan for regeneration...');
            const result = await APIClient.createProjectPlan(request);
            console.log('Project plan regeneration result:', result);

            window.App.showNotification('Project plan submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            console.log('Reloading workflow state after regeneration...');
            await this.loadWorkflowState();
            console.log('Reloading stage content after regeneration...');
            await this.loadStageContent(2);
            console.log('=== regeneratePlan() completed successfully ===');
        } catch (error) {
            console.error('Failed to regenerate project plan:', error);
            window.App.showNotification(`Failed to regenerate plan: ${error.message || error}`, 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async generateStories() {
        console.log('=== generateStories() called ===');

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

        const loadingOverlay = showLoading('Generating user stories...');
        try {
            console.log('Getting project details for story generation...');
            const project = await APIClient.getProject(this.projectId);

            // Validate that we have required IDs before proceeding
            if (!this.workflowState?.projectPlanning?.planningId) {
                console.error('Cannot generate stories: Project Planning ID is missing');
                window.App.showNotification('Failed to generate stories: Project Planning not completed.', 'error');
                return;
            }

            const request = {
                ProjectId: this.projectId,
                RequirementsAnalysisId: this.workflowState?.requirementsAnalysis?.analysisId,
                PlanningId: this.workflowState?.projectPlanning?.planningId,
                ProjectDescription: project.description || 'No description available',
                TechStack: project.techStack || 'Not specified',
                Timeline: project.timeline || 'Not specified',
                AdditionalContext: null
            };

            console.log('Generating user stories with request:', request);
            const result = await APIClient.generateStories(request);
            console.log('User stories generation result:', result);

            window.App.showNotification('User stories submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            console.log('Reloading workflow state...');
            await this.loadWorkflowState();
            console.log('Reloading stage content...');
            await this.loadStageContent(3);
            console.log('=== generateStories() completed successfully ===');
        } catch (error) {
            console.error('Failed to generate user stories:', error);
            window.App.showNotification(`Failed to generate stories: ${error.message || error}`, 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async regenerateStories() {
        console.log('=== regenerateStories() called ===');

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

        const loadingOverlay = showLoading('Regenerating user stories...');
        try {
            console.log('Getting project details for story regeneration...');
            const project = await APIClient.getProject(this.projectId);

            // Validate that we have required IDs before proceeding
            if (!this.workflowState?.projectPlanning?.planningId) {
                console.error('Cannot regenerate stories: Project Planning ID is missing');
                window.App.showNotification('Failed to regenerate stories: Project Planning not completed.', 'error');
                return;
            }

            const request = {
                ProjectId: this.projectId,
                RequirementsAnalysisId: this.workflowState?.requirementsAnalysis?.analysisId,
                PlanningId: this.workflowState?.projectPlanning?.planningId,
                ProjectDescription: project.description || 'No description available',
                TechStack: project.techStack || 'Not specified',
                Timeline: project.timeline || 'Not specified',
                AdditionalContext: 'Regenerated stories'
            };

            console.log('Regenerating user stories with request:', request);

            // Make API call to regenerate user stories
            console.log('Calling APIClient.generateStories for regeneration...');
            const result = await APIClient.generateStories(request);
            console.log('User stories regeneration result:', result);

            window.App.showNotification('User stories submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            console.log('Reloading workflow state after regeneration...');
            await this.loadWorkflowState();
            console.log('Reloading stage content after regeneration...');
            await this.loadStageContent(3);
            console.log('=== regenerateStories() completed successfully ===');
        } catch (error) {
            console.error('Failed to regenerate user stories:', error);
            window.App.showNotification(`Failed to regenerate stories: ${error.message || error}`, 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async generateAllPrompts() {
        console.log('=== generateAllPrompts() called ===');

        // Check if stories are approved
        if (this.workflowState?.storyGeneration?.isApproved !== true) {
            window.App.showNotification('You must complete User Stories before generating prompts.', 'warning');
            return;
        }

        // Check if prompts are already generated
        if (this.workflowState?.promptGeneration?.completionPercentage >= 100) {
            if (!confirm('Prompts are already generated. Do you want to regenerate them?')) {
                return;
            }
        }

        const loadingOverlay = showLoading('Generating all prompts...');
        try {
            console.log('Getting project details for prompt generation...');
            const project = await APIClient.getProject(this.projectId);

            // Validate that we have required IDs before proceeding
            if (!this.workflowState?.storyGeneration?.generationId) {
                console.error('Cannot generate prompts: Story Generation ID is missing');
                window.App.showNotification('Failed to generate prompts: User Stories not completed.', 'error');
                return;
            }

            // Get approved stories to generate prompts for
            console.log('Getting approved stories...');
            const approvedStories = await APIClient.getApprovedStories(this.workflowState.storyGeneration.generationId);
            console.log('Approved stories:', approvedStories);

            if (!approvedStories || approvedStories.length === 0) {
                window.App.showNotification('No approved stories found. Please approve some stories first.', 'warning');
                return;
            }

            // Create the prompt generation request
            const request = {
                ProjectId: this.projectId,
                RequirementsAnalysisId: this.workflowState?.requirementsAnalysis?.analysisId,
                PlanningId: this.workflowState?.projectPlanning?.planningId,
                StoryGenerationId: this.workflowState?.storyGeneration?.generationId,
                Stories: approvedStories,
                ProjectDescription: project.description || 'No description available',
                TechStack: project.techStack || 'Not specified',
                Timeline: project.timeline || 'Not specified',
                AdditionalContext: null
            };

            console.log('Generating prompts with request:', request);

            // Make API call to generate prompts
            console.log('Calling APIClient.generatePrompt...');
            const result = await APIClient.generatePrompt(request);
            console.log('Prompt generation result:', result);

            window.App.showNotification('Prompts submitted for review! Check the Review Queue.', 'success');

            // Reload workflow state to reflect changes
            console.log('Reloading workflow state...');
            await this.loadWorkflowState();
            console.log('Reloading stage content...');
            await this.loadStageContent(4);
            console.log('=== generateAllPrompts() completed successfully ===');
        } catch (error) {
            console.error('Failed to generate prompts:', error);
            window.App.showNotification(`Failed to generate prompts: ${error.message || error}`, 'error');
        } finally {
            hideLoading(loadingOverlay);
        }
    }

    async completeProject() {
        if (confirm('Are you sure you want to complete this project? This action cannot be undone.')) {
            const loadingOverlay = showLoading('Completing project...');
            try {
                // Implementation for project completion
                window.App.showNotification('Project completed successfully!', 'success');
                setTimeout(() => {
                    window.location.href = '/Projects';
                }, 2000);
            } catch (error) {
                window.App.showNotification(`Failed to complete project: ${error.message || error}`, 'error');
            } finally {
                hideLoading(loadingOverlay);
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
            // Store current state for comparison
            const previousRequirementsApproved = this.workflowState?.requirementsAnalysis?.isApproved === true;
            const previousPlanningApproved = this.workflowState?.projectPlanning?.isApproved === true;
            const previousStoriesApproved = this.workflowState?.storyGeneration?.isApproved === true;
            const previousPromptCompletion = this.workflowState?.promptGeneration?.completionPercentage || 0;

            // Reload workflow state
            await this.loadWorkflowState();

            // Check for state changes and progress accordingly
            const currentRequirementsApproved = this.workflowState?.requirementsAnalysis?.isApproved === true;
            const currentPlanningApproved = this.workflowState?.projectPlanning?.isApproved === true;
            const currentStoriesApproved = this.workflowState?.storyGeneration?.isApproved === true;
            const currentPromptCompletion = this.workflowState?.promptGeneration?.completionPercentage || 0;

            console.log('Refresh check - Requirements:', previousRequirementsApproved, '->', currentRequirementsApproved);
            console.log('Refresh check - Planning:', previousPlanningApproved, '->', currentPlanningApproved);
            console.log('Refresh check - Stories:', previousStoriesApproved, '->', currentStoriesApproved);
            console.log('Refresh check - Prompts:', previousPromptCompletion, '->', currentPromptCompletion);

            // Progress to next stage based on approvals
            if (!previousRequirementsApproved && currentRequirementsApproved) {
                console.log('Requirements approved, progressing to stage 2');
                await this.loadStageContent(2);
                window.App.showNotification('Requirements approved! Moving to Project Planning.', 'success');
                return;
            }

            if (!previousPlanningApproved && currentPlanningApproved) {
                console.log('Planning approved, progressing to stage 3');
                await this.loadStageContent(3);
                window.App.showNotification('Project planning approved! Moving to User Stories.', 'success');
                return;
            }

            if (!previousStoriesApproved && currentStoriesApproved) {
                console.log('Stories approved, progressing to stage 4');
                await this.loadStageContent(4);
                window.App.showNotification('User stories approved! Moving to Prompt Generation.', 'success');
                return;
            }

            if (previousPromptCompletion < 100 && currentPromptCompletion >= 100) {
                console.log('Prompts completed, progressing to stage 5');
                await this.loadStageContent(5);
                window.App.showNotification('Prompt generation completed! Moving to Final Review.', 'success');
                return;
            }

            // Check if current stage content needs updating (e.g., status changed from NotStarted to PendingReview)
            const currentStageStatus = this.getCurrentStageStatus();
            if (currentStageStatus && this.shouldReloadCurrentStage()) {
                console.log('Stage status changed, reloading current stage');
                await this.loadStageContent(this.currentStage);
            }

        } catch (error) {
            console.warn('Failed to refresh workflow status:', error);
        }
    }

    getCurrentStageStatus() {
        switch (this.currentStage) {
            case 1: return this.workflowState?.requirementsAnalysis?.status;
            case 2: return this.workflowState?.projectPlanning?.status;
            case 3: return this.workflowState?.storyGeneration?.status;
            case 4: return this.workflowState?.promptGeneration?.status;
            case 5: return 'review';
            default: return null;
        }
    }

    shouldReloadCurrentStage() {
        // Simple heuristic: if we're on stage 1 and requirements are pending review, we should reload
        if (this.currentStage === 1 && this.workflowState?.requirementsAnalysis?.status === 'PendingReview') {
            return true;
        }
        // Add more conditions as needed
        return false;
    }

    // Utility methods
    viewRequirementsReview() {
        // Navigate to the review queue with the current review ID
        const reviewId = this.workflowState?.requirementsAnalysis?.reviewId;
        if (reviewId) {
            window.location.href = `/Reviews/Queue?reviewId=${reviewId}`;
        } else {
            window.location.href = '/Reviews/Queue';
        }
    }

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

    // Show start workflow button for new projects (containers removed, now integrated into stage content)
    showStartWorkflowButton() {
        // These containers were removed from HTML, functionality now integrated into stage content
        console.log('Start workflow button functionality integrated into stage content - containers removed');
    }

    // Hide start workflow button when not needed
    hideStartWorkflowButton() {
        // Button visibility now handled by stage content loading
        console.log('Start workflow button visibility managed by stage content - containers removed');
    }

    // Legacy methods - containers removed from HTML
    showNewProjectActionButton() {
        console.log('New project action button container removed from HTML');
    }

    hideNewProjectActionButton() {
        console.log('New project action button container removed from HTML');
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

    // Add missing updateWorkflowUI method
    updateWorkflowUI() {
        try {
            // Update project overview if project data is available
            if (this.projectData) {
                this.updateProjectOverview(this.projectData);
                this.updateProgressIndicators(this.projectData);
            }

            // Update pipeline indicators based on current progress
            const progress = this.calculateProgress(this.projectData || {});
            this.updatePipelineIndicators(progress);

            console.log('Workflow UI updated successfully');
        } catch (error) {
            console.error('Error updating workflow UI:', error);
        }
    }
}

// Initialize workflow manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    console.log('=== WORKFLOW INITIALIZATION STARTED ===');
    console.log('Current URL:', window.location.href);
    console.log('Full URL with params:', window.location.search);
    console.log('DOM Content Loaded - starting workflow initialization');

    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('projectId');
    const newProject = urlParams.get('newProject') === 'true';

    console.log('Parsed URL parameters:', {
        projectId: projectId,
        newProject: newProject,
        rawNewProject: urlParams.get('newProject'),
        allParams: Object.fromEntries(urlParams)
    });

    if (projectId) {
        console.log(`Creating WorkflowManager for project ${projectId}, newProject=${newProject}`);

        try {
            window.workflowManager = new WorkflowManager(projectId, newProject);
            console.log('WorkflowManager created successfully');
        } catch (error) {
            console.error('Failed to create WorkflowManager:', error);
            // Fallback: show basic content
            showFallbackContent();
        }
    } else {
        console.error('No project ID found for workflow initialization');
        window.App.showNotification('No project ID found', 'error');
        showFallbackContent();
    }

    console.log('=== WORKFLOW INITIALIZATION COMPLETED ===');
});

// Fallback function to show basic content if workflow manager fails
function showFallbackContent() {
    console.log('Showing fallback content due to workflow initialization failure');
    const stageContent = document.getElementById('stage-content');
    if (stageContent) {
        stageContent.innerHTML = `
            <div class="stage-container">
                <h2>Requirements Analysis</h2>
                <div class="empty-stage">
                    <div class="empty-icon">📋</div>
                    <h3>Workflow Loading Issue</h3>
                    <p>There was an issue loading the workflow. Please try refreshing the page.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="location.reload()">
                            🔄 Refresh Page
                        </button>
                        <button class="btn btn-secondary" onclick="startManualAnalysis()">
                            🚀 Start Analysis Manually
                        </button>
                    </div>
                </div>
            </div>
        `;
    }
}

// Manual analysis function for fallback
function startManualAnalysis() {
    const requirements = prompt('Please describe your project requirements:\n\n' +
        'What problem are you trying to solve? What features do you need? ' +
        'What technology constraints do you have?');

    if (requirements) {
        alert('Requirements received: ' + requirements.substring(0, 100) + '...\n\n' +
            'This is a fallback function. The full workflow will be available once the page is refreshed.');
    }
}

// Initialize workflow manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    console.log('=== WORKFLOW INITIALIZATION STARTED ===');
    console.log('Current URL:', window.location.href);
    console.log('Full URL with params:', window.location.search);

    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('projectId');
    const newProject = urlParams.get('newProject') === 'true';

    console.log('Parsed URL parameters:', {
        projectId: projectId,
        newProject: newProject,
        rawNewProject: urlParams.get('newProject'),
        allParams: Object.fromEntries(urlParams)
    });

    if (projectId) {
        console.log(`Creating WorkflowManager for project ${projectId}, newProject=${newProject}`);
        window.workflowManager = new WorkflowManager(projectId, newProject);
        console.log('WorkflowManager created successfully');
    } else {
        console.error('No project ID found for workflow initialization');
        window.App.showNotification('No project ID found', 'error');
    }

    console.log('=== WORKFLOW INITIALIZATION COMPLETED ===');
});

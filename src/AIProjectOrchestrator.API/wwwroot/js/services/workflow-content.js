/**
 * WorkflowContentService - Handles all stage content generation for the workflow manager
 * Following Single Responsibility Principle by separating content generation from business logic
 */
class WorkflowContentService {
    /**
     * Initialize the WorkflowContentService with dependencies
     * @param {object} workflowManager - Reference to the main workflow manager
     * @param {object} apiClient - API client for making requests
     */
    constructor(workflowManager, apiClient) {
        if (!workflowManager) {
            throw new Error('WorkflowManager is required');
        }
        if (!apiClient) {
            throw new Error('APIClient is required');
        }

        this.workflowManager = workflowManager;
        this.apiClient = apiClient;
        this.isInitialized = false;
    }

    /**
     * Get content for a specific stage
     * @param {number} stage - Stage number (1-5)
     * @returns {Promise<string>} HTML content for the stage
     */
    async getStageContent(stage) {
        try {
            console.log(`WorkflowContentService: Getting content for stage ${stage}`);

            const templates = {
                1: this.getRequirementsStage.bind(this),
                2: this.getPlanningStage.bind(this),
                3: this.getStoriesStage.bind(this),
                4: this.getPromptsStage.bind(this),
                5: this.getReviewStage.bind(this)
            };

            const content = templates[stage] ? await templates[stage]() : '<p>Stage not found</p>';
            console.log(`WorkflowContentService: Generated content for stage ${stage}, length: ${content.length}`);
            return content;
        } catch (error) {
            console.error(`WorkflowContentService: Error generating content for stage ${stage}:`, error);
            throw new Error(`Failed to generate content for stage ${stage}: ${error.message}`);
        }
    }

    /**
     * Get requirements stage content
     * @returns {Promise<string>} HTML content for requirements stage
     */
    async getRequirementsStage() {
        try {
            console.log('=== WorkflowContentService.getRequirementsStage called ===');
            const workflowState = this.workflowManager.workflowState;
            console.log('Workflow state:', workflowState);
            console.log('Requirements analysis:', workflowState?.requirementsAnalysis);

            // Check if we have an analysis ID from the workflow state
            const analysisId = workflowState?.requirementsAnalysis?.analysisId;
            const status = workflowState?.requirementsAnalysis?.status;
            const isApproved = workflowState?.requirementsAnalysis?.isApproved === true;

            console.log('Analysis ID:', analysisId);
            console.log('Status:', status);
            console.log('Is Approved:', isApproved);

            if (analysisId) {
                console.log('Found analysis ID, trying to load requirements details');
                // Try to get the actual requirements analysis results
                try {
                    const requirements = await this.apiClient.getRequirements(analysisId);
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
            console.error('Error in WorkflowContentService.getRequirementsStage:', error);
            return this.getRequirementsEmptyState();
        }
    }

    getRequirementsActiveState() {
        console.log('=== WorkflowContentService.getRequirementsActiveState called ===');
        const workflowState = this.workflowManager.workflowState;
        const hasAnalysis = workflowState?.requirementsAnalysis?.status !== 'NotStarted';
        const isPending = workflowState?.requirementsAnalysis?.status === 'PendingReview';
        const isApproved = workflowState?.requirementsAnalysis?.isApproved === true;

        console.log(`Requirements state - hasAnalysis: ${hasAnalysis}, isPending: ${isPending}, isApproved: ${isApproved}`);
        console.log('Raw requirements analysis:', workflowState?.requirementsAnalysis);

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
            const canAccess = this.workflowManager.workflowState?.requirementsAnalysis?.isApproved === true;

            if (!canAccess) {
                return this.getPlanningLockedState();
            }

            // Check if we have a planning ID from the workflow state
            const planningId = this.workflowManager.workflowState?.projectPlanning?.planningId;
            const planningStatus = this.workflowManager.workflowState?.projectPlanning?.status;
            const isApproved = this.workflowManager.workflowState?.projectPlanning?.isApproved === true;

            console.log('Planning stage check - planningId:', planningId, 'status:', planningStatus, 'isApproved:', isApproved);

            if (planningId) {
                // Try to get the actual planning results
                try {
                    const planning = await this.apiClient.getProjectPlan(planningId);

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
            console.error('Error in WorkflowContentService.getPlanningStage:', error);
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
        const workflowState = this.workflowManager.workflowState;
        const planningStatus = workflowState?.projectPlanning?.status;
        const isPending = planningStatus === 'PendingReview';
        const isNotStarted = planningStatus === 'NotStarted' || planningStatus === 0;
        const hasPlanningId = workflowState?.projectPlanning?.planningId;
        const isApproved = workflowState?.projectPlanning?.isApproved === true;

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
                            🚀 Generate Project Plan
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
                        🚀 Generate Project Plan
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
            const generationId = this.workflowManager.workflowState?.storyGeneration?.generationId;
            const canAccess = this.workflowManager.workflowState?.requirementsAnalysis?.isApproved === true &&
                this.workflowManager.workflowState?.projectPlanning?.isApproved === true;

            if (!canAccess) {
                return this.getStoriesLockedState();
            }

            if (generationId) {
                // Try to get the actual stories
                try {
                    const stories = await this.apiClient.getStories(generationId);
                    const isApproved = this.workflowManager.workflowState?.storyGeneration?.isApproved === true;

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
            console.error('Error in WorkflowContentService.getStoriesStage:', error);
            return this.getStoriesEmptyState();
        }
    }

    getStoriesLockedState() {
        const workflowState = this.workflowManager.workflowState;
        return `
            <div class="stage-container">
                <h2>User Stories</h2>
                <div class="stage-status locked">
                    <div class="status-icon">🔒</div>
                    <h3>Stage Locked</h3>
                    <p>You must complete both <strong>Requirements Analysis</strong> and <strong>Project Planning</strong> before accessing this stage.</p>
                    <div class="locked-requirements">
                        ${!workflowState?.requirementsAnalysis?.isApproved ? `
                            <div class="requirement-item">
                                <span class="status-icon">❌</span>
                                <span>Requirements Analysis - Not completed</span>
                                <button class="btn btn-sm btn-primary" onclick="workflowManager.jumpToStage(1)">Go</button>
                            </div>
                        ` : ''}
                        ${!workflowState?.projectPlanning?.isApproved ? `
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
        const workflowState = this.workflowManager.workflowState;
        const storyStatus = workflowState?.storyGeneration?.status;
        const isPending = storyStatus === 'PendingReview';
        const isNotStarted = storyStatus === 'NotStarted' || storyStatus === 0 || !storyStatus;
        const hasGenerationId = workflowState?.storyGeneration?.generationId;

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
        if (hasGenerationId && !workflowState?.storyGeneration?.isApproved) {
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
                            ✨ Generate Stories
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
            // Check if we have prompts from the workflow state or API
            let prompts = [];
            let hasPrompts = false;
            const workflowState = this.workflowManager.workflowState;

            // First try to get prompts from workflow state
            if (workflowState?.promptGeneration?.storyPrompts && workflowState.promptGeneration.storyPrompts.length > 0) {
                prompts = workflowState.promptGeneration.storyPrompts;
                hasPrompts = true;
            } else {
                // Try to load prompts from API
                try {
                    prompts = await this.apiClient.getPrompts(this.workflowManager.projectId);
                    hasPrompts = prompts && prompts.length > 0;
                } catch (apiError) {
                    console.warn('Could not load prompts from API:', apiError);
                }
            }

            if (hasPrompts) {
                // Show prompt review interface
                return this.getPromptsReviewState(prompts);
            } else {
                // Check if we can get prompts from story generation
                const generationId = workflowState?.storyGeneration?.generationId;
                if (generationId) {
                    try {
                        const approvedStories = await this.apiClient.getApprovedStories(generationId);
                        if (approvedStories && approvedStories.length > 0) {
                            return this.getPromptsReadyState(approvedStories);
                        }
                    } catch (apiError) {
                        console.warn('Could not load approved stories:', apiError);
                    }
                }
                return this.getPromptsEmptyState();
            }
        } catch (error) {
            console.error('Error in WorkflowContentService.getPromptsStage:', error);
            return this.getPromptsEmptyState();
        }
    }

    getPromptsReviewState(prompts) {
        return `
            <div class="stage-container">
                <h2>Prompt Review</h2>
                <div class="prompts-content">
                    <div class="prompts-summary">
                        <h3>Generated Prompts</h3>
                        ${this.formatPrompts(prompts)}
                    </div>
                    <div class="prompts-actions">
                        <button class="btn btn-success" onclick="workflowManager.navigateToStage5()">
                            ✅ Continue to Final Review
                        </button>
                        <button class="btn btn-secondary" onclick="workflowManager.exportPrompts()">
                            📥 Export Prompts
                        </button>
                        <button class="btn btn-outline" onclick="workflowManager.regeneratePrompts()">
                            🔄 Regenerate All
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    getPromptsReadyState(approvedStories) {
        return `
            <div class="stage-container">
                <h2>Prompt Review</h2>
                <div class="stage-status ready">
                    <div class="status-icon">✅</div>
                    <h3>Ready for Prompt Generation</h3>
                    <p>${approvedStories.length} approved stories are ready for prompt generation.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="workflowManager.generateAllPrompts()">
                            🤖 Generate Prompts for ${approvedStories.length} Stories
                        </button>
                        <button class="btn btn-secondary" onclick="workflowManager.navigateToStoriesOverview()">
                            📋 Manage Individual Stories
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    getPromptsEmptyState() {
        return `
            <div class="stage-container">
                <h2>Prompt Review</h2>
                <div class="empty-stage">
                    <div class="empty-icon">🤖</div>
                    <h3>No Prompts Available</h3>
                    <p>No prompts have been generated yet. Please ensure you have approved stories and generate prompts first.</p>
                    <div class="stage-actions">
                        <button class="btn btn-primary" onclick="workflowManager.navigateToStoriesOverview()">
                            📋 Go to Stories Overview
                        </button>
                    </div>
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
            const reviews = await this.apiClient.getPendingReviews();
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

    /**
     * Check if the service is properly initialized
     * @returns {boolean} True if initialized, false otherwise
     */
    isServiceInitialized() {
        return this.isInitialized && this.workflowManager && this.apiClient;
    }

    // Action methods that the orchestrator will call
    async analyzeRequirements() {
        try {
            console.log('=== WorkflowContentService.analyzeRequirements called ===');

            // Check if requirements already exist and are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved === true) {
                window.App.showNotification('Requirements analysis is already completed and approved.', 'info');
                return;
            }

            // Check if there's already a pending analysis
            if (this.workflowManager.workflowState?.requirementsAnalysis?.status === 'PendingReview') {
                window.App.showNotification('Requirements analysis is already pending review. Check the Review Queue.', 'info');
                return;
            }

            const loadingOverlay = showLoading('Preparing requirements analysis...');
            try {
                // Get project details to pre-populate requirements
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                let requirementsInput = '';

                // If this is a new project, suggest using the project description
                if (this.workflowManager.isNewProject && project.description) {
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
                    ProjectId: this.workflowManager.projectId,
                    AdditionalContext: project.techStack ? `Tech Stack: ${project.techStack}` : null,
                    Constraints: project.timeline ? `Timeline: ${project.timeline}` : null
                };

                const result = await this.apiClient.analyzeRequirements(request);

                window.App.showNotification('Requirements submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(1);

            } finally {
                hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to analyze requirements:', error);
            window.App.showNotification(`Failed to analyze requirements: ${error.message || error}`, 'error');
        }
    }

    async generatePlan() {
        try {
            console.log('=== WorkflowContentService.generatePlan called ===');

            // Check if requirements are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating a project plan.', 'warning');
                return;
            }

            // Check if planning already exists and is approved
            if (this.workflowManager.workflowState?.projectPlanning?.isApproved === true) {
                if (!confirm('Project planning is already completed. Do you want to regenerate it? This will require re-approval.')) {
                    return;
                }
            }

            const loadingOverlay = showLoading('Generating project plan...');
            try {
                // Get project details for planning generation
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Create the project planning request
                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: null
                };

                const result = await this.apiClient.createProjectPlan(request);

                window.App.showNotification('Project plan submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(2);

            } finally {
                hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to generate project plan:', error);
            window.App.showNotification(`Failed to generate plan: ${error.message || error}`, 'error');
        }
    }

    async regeneratePlan() {
        try {
            console.log('=== WorkflowContentService.regeneratePlan called ===');

            // Check if planning is already approved
            if (this.workflowManager.workflowState?.projectPlanning?.isApproved === true) {
                if (!confirm('Project planning is already completed. Do you want to regenerate it? This will require re-approval.')) {
                    return;
                }
            }

            // Check if requirements are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating a project plan.', 'warning');
                return;
            }

            const loadingOverlay = showLoading('Generating project plan...');
            try {
                // Get project details for regeneration
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Create the project planning request for regeneration
                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: 'Regenerated plan'
                };

                const result = await this.apiClient.createProjectPlan(request);

                window.App.showNotification('Project plan submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(2);

            } finally {
                hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to regenerate project plan:', error);
            window.App.showNotification(`Failed to regenerate plan: ${error.message || error}`, 'error');
        }
    }

    async generateStories() {
        try {
            console.log('=== WorkflowContentService.generateStories called ===');

            // Check if stories are already approved
            if (this.workflowManager.workflowState?.storyGeneration?.isApproved === true) {
                if (!confirm('User stories are already completed. Do you want to regenerate them? This will require re-approval.')) {
                    return;
                }
            }

            // Check if requirements and planning are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating user stories.', 'warning');
                return;
            }

            if (this.workflowManager.workflowState?.projectPlanning?.isApproved !== true) {
                window.App.showNotification('You must complete Project Planning before generating user stories.', 'warning');
                return;
            }

            const loadingOverlay = showLoading('Generating user stories...');
            try {
                // Get project details for story generation
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Validate that we have required IDs before proceeding
                if (!this.workflowManager.workflowState?.projectPlanning?.planningId) {
                    console.error('Cannot generate stories: Project Planning ID is missing');
                    window.App.showNotification('Failed to generate stories: Project Planning not completed.', 'error');
                    return;
                }

                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    PlanningId: this.workflowManager.workflowState?.projectPlanning?.planningId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: null
                };

                const result = await this.apiClient.generateStories(request);

                window.App.showNotification('User stories submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(3);

            } finally {
                hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to generate user stories:', error);
            window.App.showNotification(`Failed to generate stories: ${error.message || error}`, 'error');
        }
    }

    async regenerateStories() {
        try {
            console.log('=== WorkflowContentService.regenerateStories called ===');

            // Check if stories are already approved
            if (this.workflowManager.workflowState?.storyGeneration?.isApproved === true) {
                if (!confirm('User stories are already completed. Do you want to regenerate them? This will require re-approval.')) {
                    return;
                }
            }

            // Check if requirements and planning are approved
            if (this.workflowManager.workflowState?.requirementsAnalysis?.isApproved !== true) {
                window.App.showNotification('You must complete Requirements Analysis before generating user stories.', 'warning');
                return;
            }

            if (this.workflowManager.workflowState?.projectPlanning?.isApproved !== true) {
                window.App.showNotification('You must complete Project Planning before generating user stories.', 'warning');
                return;
            }

            const loadingOverlay = showLoading('Generating user stories...');
            try {
                // Get project details for story regeneration
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Validate that we have required IDs before proceeding
                if (!this.workflowManager.workflowState?.projectPlanning?.planningId) {
                    console.error('Cannot regenerate stories: Project Planning ID is missing');
                    window.App.showNotification('Failed to regenerate stories: Project Planning not completed.', 'error');
                    return;
                }

                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    PlanningId: this.workflowManager.workflowState?.projectPlanning?.planningId,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: 'Regenerated stories'
                };

                const result = await this.apiClient.generateStories(request);

                window.App.showNotification('User stories submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(3);

            } finally {
                hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to regenerate user stories:', error);
            window.App.showNotification(`Failed to regenerate stories: ${error.message || error}`, 'error');
        }
    }

    async generateAllPrompts() {
        try {
            console.log('=== WorkflowContentService.generateAllPrompts called ===');

            // Check if stories are approved
            if (this.workflowManager.workflowState?.storyGeneration?.isApproved !== true) {
                window.App.showNotification('You must complete User Stories before generating prompts.', 'warning');
                return;
            }

            // Check if prompts are already generated
            if (this.workflowManager.workflowState?.promptGeneration?.completionPercentage >= 100) {
                if (!confirm('Prompts are already generated. Do you want to regenerate them?')) {
                    return;
                }
            }

            const loadingOverlay = showLoading('Generating all prompts...');
            try {
                // Get project details for prompt generation
                const project = await this.apiClient.getProject(this.workflowManager.projectId);

                // Validate that we have required IDs before proceeding
                if (!this.workflowManager.workflowState?.storyGeneration?.generationId) {
                    console.error('Cannot generate prompts: Story Generation ID is missing');
                    window.App.showNotification('Failed to generate prompts: User Stories not completed.', 'error');
                    return;
                }

                // Get approved stories to generate prompts for
                console.log('Getting approved stories...');
                const approvedStories = await this.apiClient.getApprovedStories(this.workflowManager.workflowState.storyGeneration.generationId);
                console.log('Approved stories:', approvedStories);

                if (!approvedStories || approvedStories.length === 0) {
                    window.App.showNotification('No approved stories found. Please approve some stories first.', 'warning');
                    return;
                }

                // Create the prompt generation request
                const request = {
                    ProjectId: this.workflowManager.projectId,
                    RequirementsAnalysisId: this.workflowManager.workflowState?.requirementsAnalysis?.analysisId,
                    PlanningId: this.workflowManager.workflowState?.projectPlanning?.planningId,
                    StoryGenerationId: this.workflowManager.workflowState?.storyGeneration?.generationId,
                    Stories: approvedStories,
                    ProjectDescription: project.description || 'No description available',
                    TechStack: project.techStack || 'Not specified',
                    Timeline: project.timeline || 'Not specified',
                    AdditionalContext: null
                };

                console.log('Generating prompts with request:', request);

                const result = await this.apiClient.generatePrompt(request);

                window.App.showNotification('Prompts submitted for review! Check the Review Queue.', 'success');

                // Reload workflow state to reflect changes
                await this.workflowManager.loadWorkflowState();
                await this.workflowManager.loadStageContent(4);

            } finally {
                hideLoading(loadingOverlay);
            }
        } catch (error) {
            console.error('Failed to generate prompts:', error);
            window.App.showNotification(`Failed to generate prompts: ${error.message || error}`, 'error');
        }
    }

    async completeProject() {
        try {
            console.log('=== WorkflowContentService.completeProject called ===');

            if (confirm('Are you sure you want to complete this project? This action cannot be undone.')) {
                const loadingOverlay = showLoading('Completing project...');
                try {
                    // Implementation for project completion
                    window.App.showNotification('Project completed successfully!', 'success');
                    setTimeout(() => {
                        window.location.href = '/Projects';
                    }, 2000);
                } finally {
                    hideLoading(loadingOverlay);
                }
            }
        } catch (error) {
            console.error('Failed to complete project:', error);
            window.App.showNotification(`Failed to complete project: ${error.message || error}`, 'error');
        }
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WorkflowContentService;
} else if (typeof window !== 'undefined') {
    window.WorkflowContentService = WorkflowContentService;
}
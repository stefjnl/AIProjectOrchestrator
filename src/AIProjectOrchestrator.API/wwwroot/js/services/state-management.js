/**
 * StateManagementService - Centralized workflow state management
 * 
 * This service provides a single source of truth for all workflow-related state,
 * including workflow data, navigation state, project information, and UI state.
 * It implements the Observer pattern for reactive updates and provides
 * comprehensive state validation and error handling.
 */

class StateManagementService {
    constructor(workflowManager) {
        this.workflowManager = workflowManager;
        this.projectId = workflowManager.projectId;

        // Initialize state with default values
        this.state = this.getDefaultState();

        // Observer pattern for reactive updates
        this.subscribers = new Map();
        this.subscriberId = 0;

        // State history for undo/redo functionality (future enhancement)
        this.stateHistory = [];
        this.historyIndex = -1;
        this.maxHistorySize = 50;

        // State persistence settings
        this.autoSaveEnabled = true;
        this.autoSaveInterval = null;
        this.autoSaveDelay = 5000; // 5 seconds

        console.log('StateManagementService initialized for project:', this.projectId);
    }

    /**
     * Get default state structure
     */
    getDefaultState() {
        return {
            // Workflow state from API
            workflow: {
                projectId: this.projectId,
                projectName: 'Unknown Project',
                requirementsAnalysis: {
                    status: 'NotStarted',
                    isApproved: false,
                    analysisId: null,
                    reviewId: null
                },
                projectPlanning: {
                    status: 'NotStarted',
                    isApproved: false,
                    planningId: null
                },
                storyGeneration: {
                    status: 'NotStarted',
                    isApproved: false,
                    generationId: null
                },
                promptGeneration: {
                    status: 'NotStarted',
                    isApproved: false,
                    completionPercentage: 0
                }
            },

            // Navigation state
            navigation: {
                currentStage: 1,
                stages: ['requirements', 'planning', 'stories', 'prompts', 'review'],
                isAutoRefreshing: false,
                autoRefreshInterval: null
            },

            // Project data
            project: {
                data: null,
                name: '',
                status: '',
                createdAt: null,
                description: '',
                techStack: '',
                timeline: ''
            },

            // UI state
            ui: {
                isNewProject: false,
                hasShownNewProjectPrompt: false,
                loadingState: 'idle', // idle, loading, error, success
                errorMessage: null,
                notifications: []
            },

            // Caching and optimization
            cache: {
                lastUpdated: null,
                etag: null,
                isStale: false
            }
        };
    }

    /**
     * Get current state (immutable copy)
     */
    getState() {
        return JSON.parse(JSON.stringify(this.state));
    }

    /**
     * Get specific state section
     */
    getWorkflowState() {
        return JSON.parse(JSON.stringify(this.state.workflow));
    }

    getNavigationState() {
        return JSON.parse(JSON.stringify(this.state.navigation));
    }

    getProjectState() {
        return JSON.parse(JSON.stringify(this.state.project));
    }

    getUIState() {
        return JSON.parse(JSON.stringify(this.state.ui));
    }

    /**
     * Set complete state (with validation and history)
     */
    setState(newState, options = {}) {
        try {
            // Validate new state
            this.validateState(newState);

            // Save to history before changing
            if (!options.skipHistory) {
                this.saveToHistory();
            }

            // Merge with current state to preserve structure
            this.state = this.deepMerge(this.getDefaultState(), newState);

            // Update cache timestamp
            this.state.cache.lastUpdated = new Date().toISOString();

            // Notify subscribers
            if (!options.silent) {
                this.notifySubscribers('stateChanged', this.getState());
            }

            // Auto-save if enabled
            if (this.autoSaveEnabled && !options.skipAutoSave) {
                this.scheduleAutoSave();
            }

            console.log('State updated successfully:', {
                hasWorkflowChanges: JSON.stringify(newState.workflow) !== JSON.stringify(this.state.workflow),
                hasNavigationChanges: JSON.stringify(newState.navigation) !== JSON.stringify(this.state.navigation),
                hasProjectChanges: JSON.stringify(newState.project) !== JSON.stringify(this.state.project),
                hasUIChanges: JSON.stringify(newState.ui) !== JSON.stringify(this.state.ui)
            });

            return true;
        } catch (error) {
            console.error('Failed to set state:', error);
            this.setError(`State update failed: ${error.message}`);
            return false;
        }
    }

    /**
     * Update specific state section
     */
    updateWorkflowState(workflowUpdates, options = {}) {
        return this.updateStateSection('workflow', workflowUpdates, options);
    }

    updateNavigationState(navigationUpdates, options = {}) {
        return this.updateStateSection('navigation', navigationUpdates, options);
    }

    updateProjectState(projectUpdates, options = {}) {
        return this.updateStateSection('project', projectUpdates, options);
    }

    updateUIState(uiUpdates, options = {}) {
        return this.updateStateSection('ui', uiUpdates, options);
    }

    /**
     * Generic state section update
     */
    updateStateSection(section, updates, options = {}) {
        try {
            if (!this.state[section]) {
                throw new Error(`Invalid state section: ${section}`);
            }

            // Create updated section
            const updatedSection = { ...this.state[section], ...updates };

            // Validate the updated section
            this.validateStateSection(section, updatedSection);

            // Save to history
            if (!options.skipHistory) {
                this.saveToHistory();
            }

            // Update the section
            this.state[section] = updatedSection;

            // Update cache timestamp
            this.state.cache.lastUpdated = new Date().toISOString();

            // Update stage indicators when workflow state changes
            if (section === 'workflow') {
                this.updateStageIndicators();
            }

            // Also update project overview when project state changes
            if (section === 'project') {
                this.updateProjectOverviewUI();
            }

            // Notify subscribers
            if (!options.silent) {
                this.notifySubscribers(`${section}Changed`, {
                    section: section,
                    previousState: this.state[section],
                    currentState: updatedSection,
                    changes: updates
                });
            }

            // Auto-save if enabled
            if (this.autoSaveEnabled && !options.skipAutoSave) {
                this.scheduleAutoSave();
            }

            console.log(`${section} state updated:`, updates);
            return true;
        } catch (error) {
            console.error(`Failed to update ${section} state:`, error);
            this.setError(`${section} update failed: ${error.message}`);
            return false;
        }
    }

    /**
     * Stage navigation and progression logic
     */
    getCurrentStage() {
        return this.state.navigation.currentStage;
    }

    setCurrentStage(stage, options = {}) {
        if (stage < 1 || stage > 5) {
            throw new Error(`Invalid stage: ${stage}. Must be between 1 and 5.`);
        }

        return this.updateNavigationState({ currentStage: stage }, options);
    }

    canAccessStage(stage) {
        if (!this.state.workflow) return stage === 1;

        switch (stage) {
            case 1: return true; // Stage 1 is always accessible
            case 2: return this.state.workflow.requirementsAnalysis?.isApproved === true;
            case 3: return this.state.workflow.requirementsAnalysis?.isApproved === true &&
                this.state.workflow.projectPlanning?.isApproved === true;
            case 4: return this.state.workflow.requirementsAnalysis?.isApproved === true &&
                this.state.workflow.projectPlanning?.isApproved === true &&
                this.state.workflow.storyGeneration?.isApproved === true;
            case 5: return this.state.workflow.requirementsAnalysis?.isApproved === true &&
                this.state.workflow.projectPlanning?.isApproved === true &&
                this.state.workflow.storyGeneration?.isApproved === true;
            default: return false;
        }
    }

    canProgressToNextStage() {
        if (!this.state.workflow) {
            return this.state.navigation.currentStage === 1;
        }

        const currentStage = this.state.navigation.currentStage;

        switch (currentStage) {
            case 1:
                return this.state.workflow.requirementsAnalysis?.isApproved === true;
            case 2:
                return this.state.workflow.projectPlanning?.isApproved === true;
            case 3:
                return this.state.workflow.storyGeneration?.isApproved === true;
            case 4:
                return this.state.workflow.promptGeneration?.completionPercentage >= 100;
            case 5:
                return true; // Can always "complete" the final stage
            default:
                return false;
        }
    }

    getHighestAccessibleStage() {
        if (!this.state.workflow) return 1;

        if (this.state.workflow.requirementsAnalysis?.isApproved !== true) return 1;
        if (this.state.workflow.projectPlanning?.isApproved !== true) return 2;
        if (this.state.workflow.storyGeneration?.isApproved !== true) return 3;
        if (this.state.workflow.promptGeneration?.completionPercentage < 100) return 4;
        return 5;
    }

    getCurrentStageFromWorkflow() {
        // Handle new project scenario
        if (this.state.ui.isNewProject) {
            console.log('New project detected in getCurrentStageFromWorkflow - forcing stage 1');
            return 1;
        }

        if (!this.state.workflow) {
            console.log('No workflow state, defaulting to stage 1');
            return 1;
        }

        const workflow = this.state.workflow;

        // If requirements analysis exists but is not approved, stay at stage 1
        if (workflow.requirementsAnalysis &&
            workflow.requirementsAnalysis.status !== 'NotStarted' &&
            !workflow.requirementsAnalysis.isApproved) {
            console.log('Requirements analysis exists but not approved, staying at stage 1');
            return 1;
        }

        // If requirements analysis is approved but project planning is not, go to stage 2
        if (workflow.requirementsAnalysis?.isApproved === true &&
            (!workflow.projectPlanning || !workflow.projectPlanning.isApproved)) {
            console.log('Requirements approved, project planning not approved, going to stage 2');
            return 2;
        }

        // If both requirements and planning are approved but stories are not, go to stage 3
        if (workflow.requirementsAnalysis?.isApproved === true &&
            workflow.projectPlanning?.isApproved === true &&
            (!workflow.storyGeneration || !workflow.storyGeneration.isApproved)) {
            console.log('Requirements and planning approved, stories not approved, going to stage 3');
            return 3;
        }

        // Default logic for remaining stages
        const stages = [
            { stage: 1, approved: workflow.requirementsAnalysis?.isApproved === true },
            { stage: 2, approved: workflow.projectPlanning?.isApproved === true },
            { stage: 3, approved: workflow.storyGeneration?.isApproved === true },
            { stage: 4, approved: workflow.promptGeneration?.completionPercentage >= 100 },
            { stage: 5, approved: workflow.promptGeneration?.completionPercentage >= 100 }
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
        return 5;
    }

    /**
     * Progress calculation
     */
    calculateProgress() {
        if (!this.state.workflow) return 0;

        const stages = [
            this.state.workflow.requirementsAnalysis?.isApproved,
            this.state.workflow.projectPlanning?.isApproved,
            this.state.workflow.storyGeneration?.isApproved,
            this.state.workflow.promptGeneration?.completionPercentage >= 100,
            this.state.workflow.promptGeneration?.completionPercentage >= 100
        ];

        const completed = stages.filter(Boolean).length;
        return Math.round((completed / stages.length) * 100);
    }

    /**
     * Project data management
     */
    setProjectData(projectData, options = {}) {
        return this.updateProjectState({ data: projectData }, options);
    }

    getProjectData() {
        return this.state.project.data;
    }

    updateProjectOverview(project) {
        const updates = {
            name: project.name || '',
            status: project.status || '',
            createdAt: project.createdAt || null,
            description: project.description || '',
            techStack: project.techStack || '',
            timeline: project.timeline || ''
        };

        return this.updateProjectState(updates);
    }

    /**
     * Update pipeline stage indicators based on workflow state
     */
    updateStageIndicators() {
        // Update both stage indicators and project overview
        this.updateProjectOverviewUI();
        this.updateStageIndicatorClasses();
    }

    /**
     * Update project overview UI with current project data
     */
    updateProjectOverviewUI() {
        const project = this.state.project;
        if (!project) return;

        // Update project name
        const projectNameElement = document.getElementById('project-name');
        if (projectNameElement) {
            projectNameElement.textContent = project.name || 'Unknown Project';
        }

        // Update project status
        const projectStatusElement = document.getElementById('project-status');
        if (projectStatusElement) {
            projectStatusElement.textContent = project.status || 'Not Started';
            projectStatusElement.className = `project-status status-${(project.status || 'not-started').toLowerCase().replace(/\s+/g, '-')}`;
        }

        // Update project created date
        const projectCreatedElement = document.getElementById('project-created');
        if (projectCreatedElement) {
            const createdDate = project.createdAt ? new Date(project.createdAt) : new Date();
            projectCreatedElement.textContent = createdDate.toLocaleDateString();
        }

        // Update project progress
        const projectProgressElement = document.getElementById('project-progress');
        if (projectProgressElement) {
            const progress = this.calculateProgress();
            projectProgressElement.textContent = `${progress}%`;

            // Update progress bar
            const progressBar = document.querySelector('.progress-bar .progress-fill');
            if (progressBar) {
                progressBar.style.width = `${progress}%`;
            }
        }

        // Update loading state
        const overviewContent = document.getElementById('overview-content');
        if (overviewContent && overviewContent.innerHTML.includes('Loading...')) {
            overviewContent.classList.remove('loading');
        }
    }

    /**
     * Update stage indicator CSS classes based on workflow state
     */
    updateStageIndicatorClasses() {
        const workflow = this.state.workflow;
        if (!workflow) return;

        console.log('updateStageIndicatorClasses called with workflow:', {
            requirementsAnalysis: workflow.requirementsAnalysis,
            projectPlanning: workflow.projectPlanning,
            storyGeneration: workflow.storyGeneration,
            promptGeneration: workflow.promptGeneration
        });

        // Clear all existing classes first to ensure clean state
        for (let i = 1; i <= 5; i++) {
            const element = document.getElementById(`stage-${i}`);
            if (element) {
                element.classList.remove('completed', 'active', 'available');
                console.log(`Cleared classes for stage-${i}`);
            }
        }

        // Update stage 1 (requirements)
        const stage1Element = document.getElementById('stage-1');
        if (stage1Element) {
            console.log('Stage 1 analysis:', {
                isApproved: workflow.requirementsAnalysis?.isApproved,
                status: workflow.requirementsAnalysis?.status
            });

            if (workflow.requirementsAnalysis?.isApproved) {
                stage1Element.classList.add('completed');
                console.log('Stage 1: added completed class');
            } else if (workflow.requirementsAnalysis?.status === 'Processing' ||
                workflow.requirementsAnalysis?.status === 'PendingReview') {
                stage1Element.classList.add('active');
                console.log('Stage 1: added active class for status:', workflow.requirementsAnalysis?.status);
            } else {
                console.log('Stage 1: no class added, status:', workflow.requirementsAnalysis?.status);
            }
        }

        // Update stage 2 (planning)
        const stage2Element = document.getElementById('stage-2');
        if (stage2Element) {
            console.log('Stage 2 analysis:', {
                isApproved: workflow.projectPlanning?.isApproved,
                status: workflow.projectPlanning?.status,
                requirementsApproved: workflow.requirementsAnalysis?.isApproved
            });

            if (workflow.projectPlanning?.isApproved) {
                stage2Element.classList.add('completed');
                console.log('Stage 2: added completed class');
            } else if (workflow.projectPlanning?.status === 'Processing' ||
                workflow.projectPlanning?.status === 'PendingReview') {
                stage2Element.classList.add('active');
                console.log('Stage 2: added active class for status:', workflow.projectPlanning?.status);
            } else if (workflow.requirementsAnalysis?.isApproved === true) {
                // Stage 2 should be active if requirements are approved but planning is not started/processing
                stage2Element.classList.add('active');
                console.log('Stage 2: added active class because requirements are approved');
            } else {
                console.log('Stage 2: no class added, status:', workflow.projectPlanning?.status);
            }
        }

        // Update stage 3 (stories)
        const stage3Element = document.getElementById('stage-3');
        if (stage3Element) {
            console.log('Stage 3 analysis:', {
                isApproved: workflow.storyGeneration?.isApproved,
                status: workflow.storyGeneration?.status,
                planningApproved: workflow.projectPlanning?.isApproved
            });

            if (workflow.storyGeneration?.isApproved) {
                stage3Element.classList.add('completed');
                console.log('Stage 3: added completed class');
            } else if (workflow.storyGeneration?.status === 'Processing' ||
                workflow.storyGeneration?.status === 'PendingReview') {
                stage3Element.classList.add('active');
                console.log('Stage 3: added active class for status:', workflow.storyGeneration?.status);
            } else if (workflow.projectPlanning?.isApproved === true) {
                // Stage 3 should be active if planning is approved but stories are not started/processing
                stage3Element.classList.add('active');
                console.log('Stage 3: added active class because planning is approved');
            } else {
                console.log('Stage 3: no class added, status:', workflow.storyGeneration?.status);
            }
        }

        // Update stage 4 (prompts)
        const stage4Element = document.getElementById('stage-4');
        if (stage4Element) {
            const completion = workflow.promptGeneration?.completionPercentage || 0;
            console.log('Stage 4 analysis:', {
                completionPercentage: completion,
                isApproved: workflow.promptGeneration?.isApproved,
                storiesApproved: workflow.storyGeneration?.isApproved
            });

            if (completion >= 100) {
                stage4Element.classList.add('completed');
                console.log('Stage 4: added completed class');
            } else if (completion > 0) {
                stage4Element.classList.add('active');
                console.log('Stage 4: added active class');
            } else if (workflow.storyGeneration?.isApproved === true) {
                // Stage 4 should be active if stories are approved but prompts are not started
                stage4Element.classList.add('active');
                console.log('Stage 4: added active class because stories are approved');
            } else {
                console.log('Stage 4: no class added');
            }
        }

        // Update stage 5 (review)
        const stage5Element = document.getElementById('stage-5');
        if (stage5Element) {
            console.log('Stage 5 analysis:', {
                completionPercentage: workflow.promptGeneration?.completionPercentage
            });

            if (workflow.promptGeneration?.completionPercentage >= 100) {
                stage5Element.classList.add('available');
                console.log('Stage 5: added available class');
            } else {
                console.log('Stage 5: no class added');
            }
        }
    }

    /**
     * UI state management
     */
    setLoadingState(state, message = null) {
        return this.updateUIState({
            loadingState: state,
            errorMessage: state === 'error' ? message : null
        });
    }

    setError(message) {
        return this.updateUIState({
            loadingState: 'error',
            errorMessage: message
        });
    }

    clearError() {
        return this.updateUIState({
            loadingState: 'idle',
            errorMessage: null
        });
    }

    setNewProjectFlag(isNew, options = {}) {
        return this.updateUIState({ isNewProject: isNew }, options);
    }

    setNewProjectPromptShown(shown, options = {}) {
        return this.updateUIState({ hasShownNewProjectPrompt: shown }, options);
    }

    /**
     * Auto-refresh state management
     */
    setAutoRefreshState(isRefreshing, interval = null) {
        return this.updateNavigationState({
            isAutoRefreshing: isRefreshing,
            autoRefreshInterval: interval
        });
    }

    isAutoRefreshing() {
        return this.state.navigation.isAutoRefreshing;
    }

    /**
     * Observer pattern for reactive updates
     */
    subscribe(callback, eventType = 'stateChanged') {
        const id = ++this.subscriberId;

        if (!this.subscribers.has(eventType)) {
            this.subscribers.set(eventType, new Map());
        }

        this.subscribers.get(eventType).set(id, callback);

        console.log(`Subscriber ${id} added for event: ${eventType}`);

        // Return unsubscribe function
        return () => {
            if (this.subscribers.has(eventType)) {
                this.subscribers.get(eventType).delete(id);
                console.log(`Subscriber ${id} removed for event: ${eventType}`);
            }
        };
    }

    notifySubscribers(eventType, data) {
        if (!this.subscribers.has(eventType)) {
            return;
        }

        const subscribers = this.subscribers.get(eventType);
        console.log(`Notifying ${subscribers.size} subscribers for event: ${eventType}`);

        subscribers.forEach((callback, id) => {
            try {
                callback(data);
            } catch (error) {
                console.error(`Error in subscriber ${id} for event ${eventType}:`, error);
            }
        });
    }

    /**
     * State persistence and auto-save
     */
    enableAutoSave(enable = true) {
        this.autoSaveEnabled = enable;
        if (enable) {
            this.scheduleAutoSave();
        } else {
            this.cancelAutoSave();
        }
        console.log(`Auto-save ${enable ? 'enabled' : 'disabled'}`);
    }

    scheduleAutoSave() {
        this.cancelAutoSave();

        this.autoSaveInterval = setTimeout(() => {
            this.saveState();
        }, this.autoSaveDelay);
    }

    cancelAutoSave() {
        if (this.autoSaveInterval) {
            clearTimeout(this.autoSaveInterval);
            this.autoSaveInterval = null;
        }
    }

    async saveState() {
        try {
            // In a real implementation, this would save to localStorage, IndexedDB, or API
            const stateToSave = this.getState();

            // Simulate API save (would be replaced with actual API call)
            if (typeof APIClient !== 'undefined' && APIClient.saveWorkflowState) {
                await APIClient.saveWorkflowState(this.projectId, stateToSave);
                console.log('State saved to API successfully');
            } else {
                // Fallback to localStorage for development
                if (typeof localStorage !== 'undefined') {
                    localStorage.setItem(`workflow_state_${this.projectId}`, JSON.stringify(stateToSave));
                    console.log('State saved to localStorage successfully');
                }
            }

            return true;
        } catch (error) {
            console.error('Failed to save state:', error);
            return false;
        }
    }

    async loadState() {
        try {
            // Try to load from API first
            if (typeof APIClient !== 'undefined' && APIClient.getWorkflowState) {
                const savedState = await APIClient.getWorkflowState(this.projectId);
                if (savedState) {
                    this.setState(savedState, { skipHistory: true, silent: true });
                    console.log('State loaded from API successfully');
                    return true;
                }
            }

            // Fallback to localStorage
            if (typeof localStorage !== 'undefined') {
                const savedState = localStorage.getItem(`workflow_state_${this.projectId}`);
                if (savedState) {
                    const parsedState = JSON.parse(savedState);
                    this.setState(parsedState, { skipHistory: true, silent: true });
                    console.log('State loaded from localStorage successfully');
                    return true;
                }
            }

            console.log('No saved state found, using defaults');
            return false;
        } catch (error) {
            console.warn('Failed to load saved state, using defaults:', error);
            return false;
        }
    }

    /**
     * State history for undo/redo (future enhancement)
     */
    saveToHistory() {
        if (this.historyIndex < this.stateHistory.length - 1) {
            // Remove future history if we're not at the end
            this.stateHistory = this.stateHistory.slice(0, this.historyIndex + 1);
        }

        this.stateHistory.push(this.getState());
        this.historyIndex++;

        // Limit history size
        if (this.stateHistory.length > this.maxHistorySize) {
            this.stateHistory.shift();
            this.historyIndex--;
        }
    }

    canUndo() {
        return this.historyIndex > 0;
    }

    canRedo() {
        return this.historyIndex < this.stateHistory.length - 1;
    }

    undo() {
        if (this.canUndo()) {
            this.historyIndex--;
            const previousState = this.stateHistory[this.historyIndex];
            this.setState(previousState, { skipHistory: true });
            return true;
        }
        return false;
    }

    redo() {
        if (this.canRedo()) {
            this.historyIndex++;
            const nextState = this.stateHistory[this.historyIndex];
            this.setState(nextState, { skipHistory: true });
            return true;
        }
        return false;
    }

    /**
     * Validation methods
     */
    validateState(state) {
        if (!state || typeof state !== 'object') {
            throw new Error('State must be an object');
        }

        // Validate required sections exist
        const requiredSections = ['workflow', 'navigation', 'project', 'ui', 'cache'];
        for (const section of requiredSections) {
            if (!state[section]) {
                throw new Error(`Missing required state section: ${section}`);
            }
        }

        // Validate workflow section
        this.validateWorkflowState(state.workflow);

        // Validate navigation section
        this.validateNavigationState(state.navigation);

        // Validate project section
        this.validateProjectState(state.project);

        // Validate UI section
        this.validateUIState(state.ui);

        return true;
    }

    validateStateSection(section, sectionState) {
        switch (section) {
            case 'workflow':
                return this.validateWorkflowState(sectionState);
            case 'navigation':
                return this.validateNavigationState(sectionState);
            case 'project':
                return this.validateProjectState(sectionState);
            case 'ui':
                return this.validateUIState(sectionState);
            default:
                throw new Error(`Unknown state section: ${section}`);
        }
    }

    validateWorkflowState(workflow) {
        if (!workflow || typeof workflow !== 'object') {
            throw new Error('Workflow state must be an object');
        }

        const required = ['projectId', 'requirementsAnalysis', 'projectPlanning', 'storyGeneration', 'promptGeneration'];
        for (const field of required) {
            if (!workflow.hasOwnProperty(field)) {
                throw new Error(`Missing required workflow field: ${field}`);
            }
        }

        // Validate stage approvals are boolean
        const approvalFields = ['requirementsAnalysis', 'projectPlanning', 'storyGeneration'];
        for (const field of approvalFields) {
            if (workflow[field].hasOwnProperty('isApproved') && typeof workflow[field].isApproved !== 'boolean') {
                throw new Error(`Workflow.${field}.isApproved must be a boolean`);
            }
        }

        return true;
    }

    validateNavigationState(navigation) {
        if (!navigation || typeof navigation !== 'object') {
            throw new Error('Navigation state must be an object');
        }

        if (typeof navigation.currentStage !== 'number' || navigation.currentStage < 1 || navigation.currentStage > 5) {
            throw new Error('Navigation.currentStage must be a number between 1 and 5');
        }

        if (!Array.isArray(navigation.stages) || navigation.stages.length !== 5) {
            throw new Error('Navigation.stages must be an array with 5 elements');
        }

        if (typeof navigation.isAutoRefreshing !== 'boolean') {
            throw new Error('Navigation.isAutoRefreshing must be a boolean');
        }

        return true;
    }

    validateProjectState(project) {
        if (!project || typeof project !== 'object') {
            throw new Error('Project state must be an object');
        }

        // All fields are optional but must be correct types if present
        const stringFields = ['name', 'status', 'description', 'techStack', 'timeline'];
        for (const field of stringFields) {
            if (project.hasOwnProperty(field) && project[field] !== null && typeof project[field] !== 'string') {
                throw new Error(`Project.${field} must be a string or null`);
            }
        }

        if (project.hasOwnProperty('createdAt') && project.createdAt !== null && !(project.createdAt instanceof Date) && typeof project.createdAt !== 'string') {
            throw new Error('Project.createdAt must be a Date, string, or null');
        }

        return true;
    }

    validateUIState(ui) {
        if (!ui || typeof ui !== 'object') {
            throw new Error('UI state must be an object');
        }

        const booleanFields = ['isNewProject', 'hasShownNewProjectPrompt'];
        for (const field of booleanFields) {
            if (typeof ui[field] !== 'boolean') {
                throw new Error(`UI.${field} must be a boolean`);
            }
        }

        const validLoadingStates = ['idle', 'loading', 'error', 'success'];
        if (!validLoadingStates.includes(ui.loadingState)) {
            throw new Error(`UI.loadingState must be one of: ${validLoadingStates.join(', ')}`);
        }

        if (ui.hasOwnProperty('errorMessage') && ui.errorMessage !== null && typeof ui.errorMessage !== 'string') {
            throw new Error('UI.errorMessage must be a string or null');
        }

        if (!Array.isArray(ui.notifications)) {
            throw new Error('UI.notifications must be an array');
        }

        return true;
    }

    /**
     * Utility methods
     */
    deepMerge(target, source) {
        const result = { ...target };

        for (const key in source) {
            if (source.hasOwnProperty(key)) {
                if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
                    result[key] = this.deepMerge(result[key] || {}, source[key]);
                } else {
                    result[key] = source[key];
                }
            }
        }

        return result;
    }

    /**
     * Reset state to defaults
     */
    resetState(options = {}) {
        const defaultState = this.getDefaultState();
        defaultState.projectId = this.projectId; // Preserve project ID

        return this.setState(defaultState, { ...options, skipHistory: true });
    }

    /**
     * Update pipeline stage indicators based on workflow state
     */
    // REMOVED: Duplicate implementation - use the one at line 425 instead

    /**
     * Get state summary for debugging
     */
    getStateSummary() {
        return {
            projectId: this.projectId,
            currentStage: this.getCurrentStage(),
            progress: this.calculateProgress(),
            projectName: this.state.project.name || 'Unknown',
            projectStatus: this.state.project.status || 'Not Started',
            workflowStatus: {
                requirements: this.state.workflow.requirementsAnalysis?.status,
                planning: this.state.workflow.projectPlanning?.status,
                stories: this.state.workflow.storyGeneration?.status,
                prompts: this.state.workflow.promptGeneration?.status
            }
        };
    }

    /**
     * Get subscriber count for debugging
     */
    getSubscriberCount(eventType = 'stateChanged') {
        if (!this.subscribers.has(eventType)) return 0;
        return this.subscribers.get(eventType).size;
    }

    /**
     * Clean up resources
     */
    dispose() {
        this.cancelAutoSave();
        this.subscribers.clear();
        this.stateHistory = [];
        console.log('StateManagementService disposed');
    }
}
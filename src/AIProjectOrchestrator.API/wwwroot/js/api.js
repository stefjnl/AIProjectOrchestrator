window.APIClient = {
    baseUrl: '/api',

    async handleResponse(response) {
        if (!response.ok) {
            const error = await response.json().catch(() => ({ detail: 'Unknown error' }));
            console.error('API Error:', {
                status: response.status,
                correlationId: response.headers.get('X-Correlation-ID') || 'unknown',
                detail: error.detail || error.title || 'An unexpected error occurred'
            });
            throw new Error(error.detail || error.title || 'An unexpected error occurred');
        }
        return response.json();
    },

    async _request(method, endpoint, data = null) {
        const url = `${this.baseUrl}${endpoint}`;
        const correlationId = crypto.randomUUID();
        
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'X-Correlation-ID': correlationId,
            },
        };

        if (data) {
            options.body = JSON.stringify(data);
        }

        console.log(`API Request: ${method} ${url} (CorrelationId: ${correlationId})`);
        console.log('Request Data:', data);
        console.log('Request Options:', options);

        try {
            const response = await fetch(url, options);

            console.log(`API Response Status: ${response.status} (CorrelationId: ${correlationId})`);
            console.log('API Response Headers:', [...response.headers.entries()]);

            return await this.handleResponse(response);
        } catch (error) {
            console.error('API Network/Fetch Error:', error);
            if (error instanceof TypeError && error.message === 'Failed to fetch') {
                throw new Error('Network error: Could not connect to the API. Please ensure the backend is running and accessible.');
            }
            // Handle specific HTTP errors more gracefully
            if (error.message && error.message.includes('HTTP 503')) {
                throw new Error('Service temporarily unavailable: AI providers are offline but core functionality still works.');
            }
            throw error;
        }
    },

    async get(endpoint) {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'GET',
            headers: {
                'X-Correlation-ID': crypto.randomUUID()
            }
        });
        return this.handleResponse(response);
    },

    async post(endpoint, data) {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Correlation-ID': crypto.randomUUID()
            },
            body: JSON.stringify(data)
        });
        return this.handleResponse(response);
    },

    async delete(endpoint) {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'DELETE',
            headers: {
                'X-Correlation-ID': crypto.randomUUID()
            }
        });
        return this.handleResponse(response);
    },

    async getProjects() {
        const result = await this.get('/projects');
        // Ensure we always return an array
        return Array.isArray(result) ? result : [];
    },

    async getProject(id) {
        return this.get(`/projects/${id}`);
    },

    async createProject(projectData) {
        console.log('APIClient.createProject called with:', projectData);
        try {
            const result = await this.post('/projects', projectData);
            console.log('APIClient.createProject successful:', result);
            return result;
        } catch (error) {
            console.error('APIClient.createProject failed:', error);
            throw error;
        }
    },

    // Workflow stage APIs
    async analyzeRequirements(request) {
        return this.post('/requirements/analyze', request);
    },

    async getRequirements(analysisId) {
        return this.get(`/requirements/${analysisId}`);
    },

    async canCreateProjectPlan(analysisId) {
        return this.get(`/projectplanning/can-create/${analysisId}`);
    },

    async createProjectPlan(request) {
        return this.post('/projectplanning/create', request);
    },

    async getProjectPlan(planningId) {
        return this.get(`/projectplanning/${planningId}`);
    },

    async canGenerateStories(planningId) {
        return this.get(`/stories/can-generate/${planningId}`);
    },

    async generateStories(request) {
        return this.post('/stories/generate', request);
    },

    async getStories(storyGenerationId) {
        const result = await this.get(`/stories/generations/${storyGenerationId}/results`);
        // Ensure we always return an array
        return Array.isArray(result) ? result : [];
    },

    async canGenerateCode(storyGenId) {
        return this.get(`/code/can-generate/${storyGenId}`);
    },

    async generateCode(request) {
        return this.post('/code/generate', request);
    },

    // Review system
    async getReview(reviewId) {
        return this.get(`/review/${reviewId}`);
    },
    async getPendingReviews() {
        const result = await this.get('/review/pending');
        // Ensure we always return an array
        return Array.isArray(result) ? result : [];
    },
    async approveReview(reviewId) {
        return this.post(`/review/${reviewId}/approve`, {});
    },
    async rejectReview(reviewId, feedback) {
        return this.post(`/review/${reviewId}/reject`, { feedback });
    },

    async deleteReview(reviewId) {
        return this._request('DELETE', `/review/${reviewId}`);
    },

    // Phase 4: Prompt Generation API methods
    async generatePrompt(request) {
        return await this.post('/PromptGeneration/generate', request);
    },

    async getPromptStatus(promptId) {
        return await this.get(`/PromptGeneration/${promptId}/status`);
    },

    async canGeneratePrompt(storyGenerationId, storyIndex) {
        return await this.get(`/PromptGeneration/can-generate/${storyGenerationId}/${storyIndex}`);
    },

    async getPrompt(promptId) {
        return await this.get(`/PromptGeneration/${promptId}`);
    },

    async getWorkflowStatus(projectId) {
        return await this.get(`/review/workflow-status/${projectId}`);
    },

    async getApprovedStories(storyGenerationId) {
        const result = await this.get(`/stories/generations/${storyGenerationId}/approved`);
        // Ensure we always return an array
        return Array.isArray(result) ? result : [];
    },

    async deleteProject(id) {
        return this._request('DELETE', `/projects/${id}`);
    },

    // Prompt template methods
    async getPromptTemplates() {
        const result = await this.get('/PromptTemplates');
        // Ensure we always return an array
        return Array.isArray(result) ? result : [];
    },

    // Story management methods
    async approveStory(storyId) {
        return await this._request('PUT', `/stories/${storyId}/approve`, {});
    },

    async rejectStory(storyId, feedback) {
        return await this._request('PUT', `/stories/${storyId}/reject`, { feedback });
    },

    async editStory(storyId, updatedStory) {
        return await this._request('PUT', `/stories/${storyId}/edit`, updatedStory);
    },

    async approveStories(storyGenerationId) {
        return await this.post(`/stories/generations/${storyGenerationId}/approve`, {});
    },

    async generatePromptFromPlayground(promptContent) {
        return this.post('/playground-prompt-generation', { promptContent });
    },

    // Provider management methods
    async getProviders() {
        const result = await this.get('/ProviderManagement/providers');
        // Ensure we always return an array
        return Array.isArray(result) ? result : [];
    },

    async getCurrentProvider() {
        return this.get('/ProviderManagement/current');
    },

    async switchProvider(provider) {
        return this.post('/ProviderManagement/switch', { Provider: provider });
    },

};

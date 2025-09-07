window.APIClient = {
    baseUrl: 'http://localhost:8086/api',

    async _request(method, endpoint, data = null) {
        const url = `${this.baseUrl}${endpoint}`;
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            },
        };

        if (data) {
            options.body = JSON.stringify(data);
        }

        console.log(`API Request: ${method} ${url}`);
        console.log('Request Data:', data);
        console.log('Request Options:', options);

        try {
            const response = await fetch(url, options);

            console.log(`API Response Status: ${response.status}`);
            console.log('API Response Headers:', [...response.headers.entries()]);

            let responseBody = '';
            try {
                responseBody = await response.text();
                console.log('API Response Body:', responseBody);
            } catch (e) {
                console.warn('Could not read response body:', e);
            }

            if (!response.ok) {
                let errorMessage = `Request failed with status ${response.status}`;
                if (responseBody) {
                    try {
                        const errorData = JSON.parse(responseBody);
                        if (errorData.message) {
                            errorMessage = errorData.message;
                        } else if (typeof errorData === 'string') {
                            errorMessage = errorData;
                        }
                    } catch (e) {
                        errorMessage = responseBody; // Use raw body if not JSON
                    }
                }
                throw new Error(`HTTP ${response.status}: ${errorMessage}`);
            }

            return responseBody ? JSON.parse(responseBody) : {};

        } catch (error) {
            console.error('API Network/Fetch Error:', error);
            if (error instanceof TypeError && error.message === 'Failed to fetch') {
                throw new Error('Network error: Could not connect to the API. Please ensure the backend is running and accessible.');
            }
            throw error;
        }
    },

    async get(endpoint) {
        return this._request('GET', endpoint);
    },

    async post(endpoint, data) {
        return this._request('POST', endpoint, data);
    },

    async getProjects() {
        return this.get('/projects');
    },

    async getProject(id) {
        return this.get(`/projects/${id}`);
    },

    async createProject(projectData) {
        return this.post('/projects', projectData);
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
        return this.get(`/stories/${storyGenerationId}/results`);
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
        return this.get('/review/pending');
    },
    async approveReview(reviewId) {
        return this.post(`/review/${reviewId}/approve`, {});
    },
    async rejectReview(reviewId, feedback) {
        return this.post(`/review/${reviewId}/reject`, { feedback });
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

    async getStories(storyGenerationId) {
        return await this.get(`/stories/${storyGenerationId}/approved`);
    },

    async deleteProject(id) {
        return this._request('DELETE', `/projects/${id}`);
    },

    // Helper method to retrieve approved stories
    async getApprovedStories(storyGenerationId) {
        // Note: This endpoint may need to be created or use existing story endpoints
        return await this.get(`/stories/${storyGenerationId}/approved`);
    }
};

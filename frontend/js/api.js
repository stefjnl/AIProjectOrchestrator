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

            // Check if response is actually JSON
            let isJsonResponse = false;
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                isJsonResponse = true;
            }

            if (!response.ok) {
                let errorMessage = `Request failed with status ${response.status}`;
                if (responseBody) {
                    // Try to parse as JSON first
                    if (isJsonResponse) {
                        try {
                            const errorData = JSON.parse(responseBody);
                            if (errorData.message) {
                                errorMessage = errorData.message;
                            } else if (typeof errorData === 'string') {
                                errorMessage = errorData;
                            }
                        } catch (e) {
                            // If JSON parsing fails, use the raw body
                            errorMessage = responseBody;
                        }
                    } else {
                        // Not JSON, use raw body
                        errorMessage = responseBody;
                    }
                }
                throw new Error(`HTTP ${response.status}: ${errorMessage}`);
            }

            // For successful responses, try to parse as JSON if content-type indicates JSON
            if (responseBody && isJsonResponse) {
                try {
                    return JSON.parse(responseBody);
                } catch (e) {
                    console.warn('Failed to parse JSON response:', e);
                    // Return the raw body if JSON parsing fails
                    return responseBody;
                }
            } else if (responseBody && responseBody.trim() !== '') {
                // If there's a response body but it's not JSON, return as text
                return responseBody;
            } else {
                // Empty response
                return {};
            }

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

    async delete(endpoint) {
        return this._request('DELETE', endpoint);
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

};

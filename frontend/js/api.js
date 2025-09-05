
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

        try {
            const response = await fetch(url, options);

            if (!response.ok) {
                let errorMessage = `Request failed with status ${response.status}`;
                try {
                    const errorData = await response.json();
                    if (errorData.message) {
                        errorMessage = errorData.message;
                    } else if (typeof errorData === 'string') {
                        errorMessage = errorData;
                    }
                } catch (e) {
                    // If response is not JSON, use status text
                    errorMessage = response.statusText;
                }
                throw new Error(`HTTP ${response.status}: ${errorMessage}`);
            }

            // Handle cases where response might be empty (e.g., 204 No Content)
            const text = await response.text();
            return text ? JSON.parse(text) : {};

        } catch (error) {
            if (error instanceof TypeError && error.message === 'Failed to fetch') {
                throw new Error('Network error: Could not connect to the API. Please ensure the backend is running.');
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
    }
};

// API client for AI Project Orchestrator
const API_BASE_URL = 'http://localhost:8086/api';

class APIClient {
    static async get(endpoint) {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error(`API Error: ${response.status} - ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    static async post(endpoint, data) {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            throw new Error(`API Error: ${response.status} - ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    static async put(endpoint, data) {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            throw new Error(`API Error: ${response.status} - ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    static async delete(endpoint) {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) {
            throw new Error(`API Error: ${response.status} - ${response.statusText}`);
        }
        
        return response.ok;
    }
}

// Project API functions
async function getProjects() {
    return await APIClient.get('/projects');
}

async function getProject(id) {
    return await APIClient.get(`/projects/${id}`);
}

async function createProject(project) {
    return await APIClient.post('/projects', project);
}

async function updateProject(id, project) {
    return await APIClient.put(`/projects/${id}`, project);
}

async function deleteProject(id) {
    return await APIClient.delete(`/projects/${id}`);
}

// Requirements Analysis API functions
async function analyzeRequirements(request) {
    return await APIClient.post('/requirements/analyze', request);
}

async function getRequirementsStatus(id) {
    return await APIClient.get(`/requirements/${id}/status`);
}

// Review API functions
async function getPendingReviews() {
    return await APIClient.get('/review/pending');
}

async function approveReview(id, feedback = null) {
    return await APIClient.post(`/review/${id}/approve`, { feedback });
}

async function rejectReview(id, feedback) {
    return await APIClient.post(`/review/${id}/reject`, { feedback });
}

// Health check
async function getHealthStatus() {
    return await APIClient.get('/health');
}
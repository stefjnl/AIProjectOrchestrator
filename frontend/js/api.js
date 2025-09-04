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
        
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        } else {
            // Handle text responses
            const text = await response.text();
            // Try to parse as JSON first, if that fails, check if it's a boolean string
            try {
                return JSON.parse(text);
            } catch {
                // Check if it's a boolean string
                if (text === 'true') return true;
                if (text === 'false') return false;
                return text;
            }
        }
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

// Project Planning API functions
async function createProjectPlan(request) {
    return await APIClient.post('/projectplanning/create', request);
}

async function getProjectPlanningStatus(id) {
    return await APIClient.get(`/projectplanning/${id}/status`);
}

async function canCreateProjectPlan(requirementsAnalysisId) {
    return await APIClient.get(`/projectplanning/can-create/${requirementsAnalysisId}`);
}

// Story Generation API functions
async function generateStories(request) {
    return await APIClient.post('/stories/generate', request);
}

async function getStoryGenerationStatus(id) {
    return await APIClient.get(`/stories/${id}/status`);
}

async function canGenerateStories(planningId) {
    return await APIClient.get(`/stories/can-generate/${planningId}`);
}

// Code Generation API functions
async function generateCode(request) {
    return await APIClient.post('/code/generate', request);
}

async function getCodeGenerationStatus(id) {
    return await APIClient.get(`/code/${id}/status`);
}

async function canGenerateCode(storyGenerationId) {
    return await APIClient.get(`/code/can-generate/${storyGenerationId}`);
}

// Review API functions
async function getPendingReviews() {
    return await APIClient.get('/review/pending');
}

async function getReview(reviewId) {
    return await APIClient.get(`/review/${reviewId}`);
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

// Make functions globally available
window.getProjects = getProjects;
window.getProject = getProject;
window.createProject = createProject;
window.updateProject = updateProject;
window.deleteProject = deleteProject;
window.analyzeRequirements = analyzeRequirements;
window.getRequirementsStatus = getRequirementsStatus;
window.createProjectPlan = createProjectPlan;
window.getProjectPlanningStatus = getProjectPlanningStatus;
window.canCreateProjectPlan = canCreateProjectPlan;
window.generateStories = generateStories;
window.getStoryGenerationStatus = getStoryGenerationStatus;
window.canGenerateStories = canGenerateStories;
window.generateCode = generateCode;
window.getCodeGenerationStatus = getCodeGenerationStatus;
window.canGenerateCode = canGenerateCode;
window.getPendingReviews = getPendingReviews;
window.approveReview = approveReview;
window.rejectReview = rejectReview;
window.getHealthStatus = getHealthStatus;
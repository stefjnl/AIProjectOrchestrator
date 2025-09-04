// Mock fetch for testing API functions
global.fetch = jest.fn();

// Import API functions
// Note: In a real test environment, we would import these from the actual file
// For this example, we'll define them here
const API_BASE_URL = 'http://localhost:8086/api';

class APIClient {
    static async get(endpoint) {
        const response = await fetch(`${API_BASE_URL}${endpoint}`);
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
}

// Test suite for API functions
describe('API Functions', () => {
    beforeEach(() => {
        fetch.mockClear();
    });
    
    test('should call analyzeRequirements with correct parameters', async () => {
        // Mock response
        const mockResponse = { analysisId: '123', reviewId: '456' };
        fetch.mockResolvedValueOnce({
            ok: true,
            json: () => Promise.resolve(mockResponse)
        });
        
        // Import and test the function
        // Note: In a real test, we would import this function
        async function analyzeRequirements(request) {
            return await APIClient.post('/requirements/analyze', request);
        }
        
        const request = { projectDescription: 'Test project' };
        const response = await analyzeRequirements(request);
        
        expect(fetch).toHaveBeenCalledWith(
            'http://localhost:8086/api/requirements/analyze',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(request)
            }
        );
        expect(response).toEqual(mockResponse);
    });
    
    test('should call createProjectPlan with correct parameters', async () => {
        // Mock response
        const mockResponse = { planningId: '123', reviewId: '456' };
        fetch.mockResolvedValueOnce({
            ok: true,
            json: () => Promise.resolve(mockResponse)
        });
        
        // Import and test the function
        async function createProjectPlan(request) {
            return await APIClient.post('/projectplanning/create', request);
        }
        
        const request = { requirementsAnalysisId: 'req-123' };
        const response = await createProjectPlan(request);
        
        expect(fetch).toHaveBeenCalledWith(
            'http://localhost:8086/api/projectplanning/create',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(request)
            }
        );
        expect(response).toEqual(mockResponse);
    });
    
    test('should call generateStories with correct parameters', async () => {
        // Mock response
        const mockResponse = { generationId: '123', reviewId: '456' };
        fetch.mockResolvedValueOnce({
            ok: true,
            json: () => Promise.resolve(mockResponse)
        });
        
        // Import and test the function
        async function generateStories(request) {
            return await APIClient.post('/stories/generate', request);
        }
        
        const request = { projectPlanningId: 'plan-123' };
        const response = await generateStories(request);
        
        expect(fetch).toHaveBeenCalledWith(
            'http://localhost:8086/api/stories/generate',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(request)
            }
        );
        expect(response).toEqual(mockResponse);
    });
    
    test('should call generateCode with correct parameters', async () => {
        // Mock response
        const mockResponse = { generationId: '123', reviewId: '456' };
        fetch.mockResolvedValueOnce({
            ok: true,
            json: () => Promise.resolve(mockResponse)
        });
        
        // Import and test the function
        async function generateCode(request) {
            return await APIClient.post('/code/generate', request);
        }
        
        const request = { storyGenerationId: 'story-123' };
        const response = await generateCode(request);
        
        expect(fetch).toHaveBeenCalledWith(
            'http://localhost:8086/api/code/generate',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(request)
            }
        );
        expect(response).toEqual(mockResponse);
    });
});
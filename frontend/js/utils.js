// Test function to verify API connectivity
async function testAPIConnection() {
    try {
        console.log('Testing API connection...');
        
        // Test health endpoint
        const health = await getHealthStatus();
        console.log('Health check:', health);
        
        // Test projects endpoint
        const projects = await getProjects();
        console.log('Projects loaded:', projects.length);
        
        return true;
    } catch (error) {
        console.error('API connection failed:', error);
        return false;
    }
}

// Safe API call wrapper with error handling
async function safeAPICall(apiFunction, errorElementId = null) {
    try {
        return await apiFunction();
    } catch (error) {
        console.error('API call failed:', error);
        
        if (errorElementId) {
            const errorElement = document.getElementById(errorElementId);
            if (errorElement) {
                errorElement.innerHTML = `<p class="status-error">Error: ${error.message}</p>`;
                errorElement.style.display = 'block';
            }
        }
        
        throw error;
    }
}
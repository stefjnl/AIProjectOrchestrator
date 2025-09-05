I need to debug and fix a 500 error in my frontend-to-backend API communication. 

CONTEXT:
- Frontend: nginx on localhost:8087 serving static files
- Backend: .NET 9 API on localhost:8086 (confirmed working)
- Error: 500 response when creating projects via POST /api/projects

CURRENT FILES:
- /js/api.js: Contains APIClient with createProject() method
- /projects/create.html: Form that calls createProject()

TASKS:
1. Add detailed error logging to APIClient.post() method to capture:
   - Exact HTTP status codes
   - Response headers
   - Response body content
   - Network error details

2. Test the exact API endpoint independently:
   - Create a simple test function to call POST /api/projects directly
   - Log the full request/response cycle
   - Verify request format matches backend expectations

3. Fix CORS or request format issues based on findings

4. Ensure proper error handling displays meaningful messages to users

REQUIREMENTS:
- Use vanilla JavaScript (no frameworks)
- Maintain existing APIClient structure
- Add comprehensive console logging for debugging
- Test with this sample data: {name: "test project", description: "test description"}

Please provide the updated api.js file with enhanced debugging and the test function.
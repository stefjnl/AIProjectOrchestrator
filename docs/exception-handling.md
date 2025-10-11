## **Simplified Exception Handling Implementation Plan**

### **Step 1: Single Middleware Solution**

**File**: `src/AIProjectOrchestrator.API/Middleware/ExceptionMiddleware.cs`

```csharp
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Handle correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}", 
                correlationId, context.Request.Path);
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex, string correlationId)
    {
        var (statusCode, message) = MapException(ex);
        
        var response = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = message,
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId }
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static (int statusCode, string message) MapException(Exception ex) => ex switch
    {
        AIProviderException => (503, "AI service temporarily unavailable"),
        ValidationException => (400, ex.Message),
        ArgumentException => (400, ex.Message),
        UnauthorizedAccessException => (401, "Unauthorized"),
        KeyNotFoundException => (404, "Resource not found"),
        NotImplementedException => (501, "Feature not implemented"),
        _ => (500, "An unexpected error occurred")
    };

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized", 
        404 => "Not Found",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        503 => "Service Unavailable",
        _ => "Error"
    };
}
```

### **Step 2: Enhanced Serilog Configuration**

**Update**: `src/AIProjectOrchestrator.API/Program.cs`

```csharp
// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AIProjectOrchestrator")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

// Register middleware
app.UseMiddleware<ExceptionMiddleware>();
```

### **Step 3: Domain Exception Cleanup**

**File**: `src/AIProjectOrchestrator.Domain/Exceptions/DomainExceptions.cs`

```csharp
// Keep only essential domain exceptions
public class AIProviderException : Exception
{
    public string Provider { get; }
    
    public AIProviderException(string provider, string message) : base(message)
    {
        Provider = provider;
    }
    
    public AIProviderException(string provider, string message, Exception innerException) 
        : base(message, innerException)
    {
        Provider = provider;
    }
}

public class ValidationException : ArgumentException
{
    public ValidationException(string message) : base(message) { }
}
```

### **Step 4: Service Layer Logging Enhancement**

**Update existing services** to use structured logging with correlation context:

```csharp
public class RequirementsAnalysisService
{
    public async Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(
        RequirementsAnalysisRequest request, 
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "AnalyzeRequirements",
            ["ProjectId"] = request.ProjectId
        });

        _logger.LogInformation("Starting requirements analysis for project {ProjectId}", request.ProjectId);
        
        try
        {
            // Existing logic...
            _logger.LogInformation("Requirements analysis completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Requirements analysis failed for project {ProjectId}", request.ProjectId);
            throw; // Let middleware handle the response
        }
    }
}
```

### **Step 5: Controller Simplification**

**Remove manual exception handling** from controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
public class RequirementsController : ControllerBase
{
    private readonly IRequirementsAnalysisService _requirementsService;

    public RequirementsController(IRequirementsAnalysisService requirementsService)
    {
        _requirementsService = requirementsService;
    }

    [HttpPost("analyze")]
    public async Task<RequirementsAnalysisResponse> AnalyzeRequirements(
        [FromBody] RequirementsAnalysisRequest request)
    {
        // No try-catch needed - middleware handles exceptions
        return await _requirementsService.AnalyzeRequirementsAsync(request);
    }
}
```

### **Step 6: Frontend Error Handling**

**Update**: `src/AIProjectOrchestrator.API/wwwroot/js/api.js`

```javascript
window.APIClient = {
    async handleResponse(response) {
        if (!response.ok) {
            const error = await response.json();
            console.error('API Error:', {
                status: response.status,
                correlationId: error.correlationId,
                detail: error.detail
            });
            throw new Error(error.detail || 'An unexpected error occurred');
        }
        return response.json();
    },

    async post(endpoint, data) {
        const response = await fetch(`/api${endpoint}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Correlation-ID': crypto.randomUUID()
            },
            body: JSON.stringify(data)
        });
        return this.handleResponse(response);
    }
};
```

## **Implementation Benefits**

### **Architectural Compliance**
- **Clean Architecture**: Single middleware in API layer, domain exceptions in Domain layer
- **SRP**: Each component has one responsibility
- **DRY**: No duplicate exception handling logic
- **KISS**: Simple, understandable solution

### **Practical Advantages**
- **Performance**: Single middleware, minimal overhead
- **Maintainability**: One file to modify for exception handling changes
- **Debugging**: Clear correlation ID tracking across all layers
- **Testing**: Simple to test, few dependencies

### **Implementation Steps**

1. **Create** `ExceptionMiddleware.cs` in API layer
2. **Update** `Program.cs` to register middleware first in pipeline
3. **Enhance** Serilog configuration for structured logging
4. **Remove** existing try-catch blocks from controllers
5. **Update** frontend error handling for correlation IDs
6. **Test** with PowerShell commands:

```powershell
# Test normal operation
Invoke-WebRequest -Uri "http://localhost:8086/api/projects" -Method GET

# Test error handling  
Invoke-WebRequest -Uri "http://localhost:8086/api/projects/999" -Method GET

# Test with correlation ID
$headers = @{"X-Correlation-ID" = [System.Guid]::NewGuid().ToString()}
Invoke-WebRequest -Uri "http://localhost:8086/api/projects" -Headers $headers -Method GET
```

## **Migration Strategy**

1. **Phase 1**: Add middleware alongside existing error handling
2. **Phase 2**: Remove manual try-catch blocks from controllers one by one
3. **Phase 3**: Update frontend to handle new error format
4. **Phase 4**: Monitor logs and adjust error mapping as needed

This simplified approach provides enterprise-grade exception handling without overengineering, maintains Clean Architecture principles, and delivers immediate value with minimal complexity.
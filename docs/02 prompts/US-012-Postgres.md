# PostgreSQL Persistence Implementation - AI Coding Assistant Prompt

## Implementation Task
Migrate the AI Project Orchestrator from in-memory storage to PostgreSQL 16 persistence, maintaining all existing functionality while adding production-ready data persistence capabilities.

## Business Requirements
**Current State**: The application uses `ConcurrentDictionary` in-memory storage that loses all data on restart, making it unsuitable for production use and creating development friction.

**Target State**: Complete PostgreSQL persistence enabling:
- Data retention across application restarts
- Production deployment capability  
- Long-running project development workflows
- Cross-session collaboration potential

**Critical Constraint**: Zero breaking changes to existing API contracts or frontend functionality.

## Technical Context

### Current Architecture
- **.NET 9 Web API** with Clean Architecture (Domain/Application/Infrastructure/API layers)
- **PostgreSQL 16** container already running with these credentials:
  - **Service Name**: `db`
  - **Database**: `aiprojectorchestrator`
  - **Username**: `user`
  - **Password**: `password`
  - **Port**: `5432`
  - **Network**: `aiprojectorchestrator-network`
- **Docker Compose** environment - backend connects via service name `db`
- **Existing Services**: All use in-memory `ConcurrentDictionary` storage
- **Critical Issue**: `IReviewService` registration causes instance isolation (needs singleton fix)

### Current Service Pattern Example
```csharp
public class RequirementsAnalysisService : IRequirementsAnalysisService
{
    private static readonly ConcurrentDictionary<string, RequirementsAnalysisResponse> _analyses = new();
    private static readonly ConcurrentDictionary<string, RequirementsAnalysisStatus> _statuses = new();
    
    public async Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(RequirementsAnalysisRequest request)
    {
        // Current in-memory implementation
    }
}
```

### Existing Domain Models (Reference)
Located in `src/AIProjectOrchestrator.Domain/Models/`:
- `RequirementsAnalysisRequest/Response/Status`
- `ProjectPlanningRequest/Response/Status`  
- `StoryGenerationRequest/Response/Status`
- `PromptGenerationRequest/Response/Status`
- Basic `Project` entity already exists

## Implementation Requirements

### 1. Database Infrastructure Setup

**Create ApplicationDbContext**:
```csharp
// Location: src/AIProjectOrchestrator.Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    // DbSet properties for all entities
    // OnModelCreating with proper configurations
    // Connection string configuration for Docker service name 'db'
}
```

**Entity Models Required**:
```csharp
// All in src/AIProjectOrchestrator.Domain/Entities/
public class Project 
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    
    // Navigation properties
    public ICollection<RequirementsAnalysis> RequirementsAnalyses { get; set; }
}

public class RequirementsAnalysis
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string AnalysisId { get; set; } // Preserve existing string IDs
    public RequirementsAnalysisStatus Status { get; set; }
    public string Content { get; set; }
    public string ReviewId { get; set; }
    public DateTime CreatedDate { get; set; }
    
    // Navigation properties
    public Project Project { get; set; }
    public Review Review { get; set; }
    public ICollection<ProjectPlanning> ProjectPlannings { get; set; }
}

// Similar entities for ProjectPlanning, StoryGeneration, PromptGeneration, Review
```

**Migration Creation**:
```bash
# Command to run after entity creation
dotnet ef migrations add InitialCreate --project src/AIProjectOrchestrator.Infrastructure --startup-project src/AIProjectOrchestrator.API
```

### 2. Repository Pattern Implementation

**Generic Repository Interface**:
```csharp
// Location: src/AIProjectOrchestrator.Domain/Interfaces/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<T> GetByStringIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

**Specific Repository Interfaces**:
```csharp
// Location: src/AIProjectOrchestrator.Domain/Interfaces/
public interface IRequirementsAnalysisRepository : IRepository<RequirementsAnalysis>
{
    Task<RequirementsAnalysis> GetByAnalysisIdAsync(string analysisId, CancellationToken cancellationToken = default);
    Task<RequirementsAnalysis> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
}

// Similar interfaces for other domain entities
```

### 3. Service Layer Migration

**Update Service Implementations**:
- Replace all `ConcurrentDictionary` usage with repository calls
- Maintain existing method signatures and return types
- Preserve all existing functionality and error handling
- Add proper transaction handling for multi-entity operations

**Critical Service Registration Fix**:
```csharp
// Location: src/AIProjectOrchestrator.API/Program.cs
// CURRENT ISSUE: IReviewService causes instance isolation
// REQUIRED FIX: Change to Singleton registration
builder.Services.AddSingleton<IReviewService, ReviewService>();

// Add new registrations
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    
builder.Services.AddScoped<IRepository<Project>, Repository<Project>>();
// ... register all repositories
```

### 4. Data Access Implementation

**Repository Implementation Pattern**:
```csharp
// Location: src/AIProjectOrchestrator.Infrastructure/Repositories/Repository.cs
public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;
    
    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    // Implement all interface methods with proper async/await
    // Include error handling and transaction support
}
```

**Service Migration Example**:
```csharp
// Update existing RequirementsAnalysisService
public class RequirementsAnalysisService : IRequirementsAnalysisService
{
    private readonly IRequirementsAnalysisRepository _repository;
    private readonly IProjectRepository _projectRepository;
    // ... other dependencies remain the same
    
    public async Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(RequirementsAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        // Validate project exists
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        
        // Create analysis entity
        var analysis = new RequirementsAnalysis
        {
            ProjectId = request.ProjectId,
            AnalysisId = Guid.NewGuid().ToString(),
            Status = RequirementsAnalysisStatus.Processing,
            Content = string.Empty,
            CreatedDate = DateTime.UtcNow
        };
        
        await _repository.AddAsync(analysis, cancellationToken);
        
        // Continue with existing AI processing logic...
        // Update analysis with results and save
        
        return new RequirementsAnalysisResponse
        {
            AnalysisId = analysis.AnalysisId,
            Status = analysis.Status,
            ReviewId = analysis.ReviewId
        };
    }
}
```

## Testing Requirements

### Unit Tests
- Update existing tests to use in-memory database provider
- Maintain 100% test pass rate (currently 101 tests)
- Add repository-specific unit tests

### Integration Tests  
- Test with real PostgreSQL database
- Verify end-to-end workflows with persistence
- Test application restart scenarios

## Code Quality Standards

### Entity Framework Patterns
- Use async/await throughout with CancellationToken support
- Proper Include() for related data loading
- Avoid N+1 query problems
- Use transactions for multi-entity operations

### Error Handling
- Database-specific exception handling
- Connection failure recovery
- Transaction rollback on errors
- Meaningful error messages preserved

### Performance Considerations
- Index frequently queried columns (AnalysisId, ProjectId, Status, CreatedDate)
- Efficient query patterns
- Proper connection management
- Query performance <100ms for typical operations

## Integration Specifications

### Database Connection
```json
// appsettings.json configuration - Use Docker service name 'db'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Port=5432;Database=aiprojectorchestrator;Username=user;Password=password"
  }
}
```

### Health Check Integration
- Add EF Core health checks for database connectivity
- Maintain existing health check endpoints
- Include database status in health check responses

### Migration Strategy
- Automatic migrations on development startup
- Seed data for development testing (optional)
- Production migration strategy documentation

## Deliverables Checklist

### Database Layer
- [ ] ApplicationDbContext with proper configuration
- [ ] All entity models with relationships
- [ ] Initial migration created and tested
- [ ] Connection string configuration for Docker service 'db'

### Repository Layer  
- [ ] Generic repository pattern implementation
- [ ] Domain-specific repositories for complex queries
- [ ] Repository registration in DI container
- [ ] Unit of Work pattern (if needed for transactions)

### Service Layer
- [ ] All services migrated from in-memory to database storage
- [ ] ReviewService singleton registration fix applied
- [ ] Existing API contracts preserved
- [ ] Transaction handling for multi-entity operations

### Testing & Validation
- [ ] All existing unit tests pass
- [ ] New integration tests with PostgreSQL
- [ ] End-to-end workflow testing
- [ ] Application restart persistence verification

### Infrastructure
- [ ] Docker Compose database integration working
- [ ] Health checks include database connectivity
- [ ] Logging configured for EF Core queries (development)
- [ ] Performance acceptable for typical operations

## Success Validation Commands

```bash
# Verify database connection from backend container
docker-compose exec backend dotnet ef database update

# Test application with persistence
curl -X POST http://localhost:8086/api/projects -H "Content-Type: application/json" -d '{"name":"Test","description":"Test project"}'
docker-compose restart backend
curl http://localhost:8086/api/projects  # Should return the created project

# Verify complete workflow persistence
# 1. Create project → Complete stages 1-3 → Restart container → Verify stage 4 still accessible
```

## Critical Implementation Notes

1. **Docker Network Connection**: Use service name `db` (not `localhost`) for database connection in containerized environment
2. **Maintain Backward Compatibility**: All existing REST endpoints must continue working without frontend changes
3. **Preserve String IDs**: Many services use string identifiers (AnalysisId, etc.) - maintain these for API compatibility
4. **Fix Review Service**: Critical singleton registration issue must be resolved
5. **Transaction Handling**: Multi-stage workflows may need transaction coordination
6. **Performance**: Target <100ms response times for typical database operations

**Database Credentials Summary**:
- **Host**: `db` (Docker service name)
- **Port**: `5432`
- **Database**: `aiprojectorchestrator`
- **Username**: `user`
- **Password**: `password`

Transform this application from a development prototype into a production-ready system with enterprise-grade data persistence while maintaining all existing functionality and API contracts.


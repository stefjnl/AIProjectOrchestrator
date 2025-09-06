# User Story: PostgreSQL Persistence Implementation

## Business Context
The AI Project Orchestrator currently uses in-memory storage that loses all data on application restart. This creates significant friction during development and makes the system unsuitable for production use. The PostgreSQL 16 container is already configured but unused.

## User Story
**As a** developer using the AI Project Orchestrator  
**I want** all workflow data persisted to PostgreSQL database  
**So that** my projects, reviews, and progress are retained across application restarts and I can work on long-running projects without losing state

## Current Pain Points
- Complete data loss on container restart
- Manual workflow recreation required for testing
- No cross-session collaboration capability
- Development testing inefficiency
- Production deployment impossible

## Acceptance Criteria

### Database Schema & Entities
- [ ] **Entity Models**: Create EF Core entities for all domain objects:
  - `Project` (Id, Name, Description, CreatedDate, UpdatedDate)
  - `RequirementsAnalysis` (Id, ProjectId, Status, Content, ReviewId, CreatedDate)
  - `ProjectPlanning` (Id, RequirementsAnalysisId, Status, Content, ReviewId, CreatedDate)
  - `StoryGeneration` (Id, ProjectPlanningId, Status, Stories, ReviewId, CreatedDate)
  - `PromptGeneration` (Id, StoryGenerationId, StoryIndex, Status, Content, ReviewId, CreatedDate)
  - `Review` (Id, Content, Status, ServiceName, PipelineStage, Feedback, CreatedDate, UpdatedDate)

- [ ] **Relationships**: Proper foreign key constraints and navigation properties
- [ ] **Migrations**: EF Core migrations for database schema creation
- [ ] **Seed Data**: Optional sample data for development testing

### DbContext Configuration
- [ ] **Connection String**: PostgreSQL connection via appsettings.json
- [ ] **Entity Configuration**: Fluent API configurations for constraints and indexes
- [ ] **Context Registration**: Proper DI registration with appropriate lifetime
- [ ] **Transaction Support**: DbContext transaction handling for multi-entity operations

### Repository Pattern Implementation
- [ ] **Generic Repository**: `IRepository<T>` with standard CRUD operations
- [ ] **Specific Repositories**: Domain-specific repositories for complex queries
- [ ] **Unit of Work**: `IUnitOfWork` pattern for transaction coordination
- [ ] **Repository Registration**: DI container registration for all repositories

### Service Layer Migration
- [ ] **Requirements Service**: Replace `ConcurrentDictionary` with repository calls
- [ ] **Planning Service**: Database persistence with proper context retrieval
- [ ] **Story Service**: Persistent storage with individual story access
- [ ] **Prompt Service**: Database storage maintaining existing API contracts
- [ ] **Review Service**: **CRITICAL** - Fix singleton issue + database persistence
- [ ] **Project Service**: Enhanced with proper relationship loading

### Data Access Patterns
- [ ] **Async Operations**: All database operations use async/await patterns
- [ ] **Eager Loading**: Proper Include() statements for related data
- [ ] **Query Optimization**: Efficient queries avoiding N+1 problems
- [ ] **Exception Handling**: Database-specific error handling and recovery

### Configuration & Infrastructure
- [ ] **Connection Management**: Proper connection string configuration
- [ ] **Database Initialization**: Automatic migration on startup (development)
- [ ] **Health Checks**: Database connectivity verification
- [ ] **Logging**: EF Core query logging for development debugging

### Backward Compatibility
- [ ] **API Contracts**: No breaking changes to existing REST endpoints
- [ ] **Response Models**: Maintain existing response structure
- [ ] **Frontend Compatibility**: No frontend changes required
- [ ] **Service Interfaces**: Preserve existing interface contracts

### Testing Requirements
- [ ] **Unit Tests**: Repository and service layer tests with in-memory database
- [ ] **Integration Tests**: Real PostgreSQL database testing
- [ ] **Migration Tests**: Verify schema creation and data integrity
- [ ] **Performance Tests**: Query performance with realistic data volumes

## Implementation Approach

### Phase 1: Infrastructure Setup
1. Create EF Core entities with proper relationships
2. Configure DbContext with PostgreSQL provider
3. Create and apply initial migration
4. Update DI registration

### Phase 2: Repository Layer
1. Implement generic repository pattern
2. Create domain-specific repositories
3. Add Unit of Work pattern
4. Register repositories in DI container

### Phase 3: Service Migration
1. **Priority**: Fix ReviewService singleton issue
2. Update each service to use repositories instead of in-memory storage
3. Maintain existing API contracts
4. Preserve current functionality

### Phase 4: Testing & Validation
1. Comprehensive testing with database integration
2. Performance validation
3. Data integrity verification
4. End-to-end workflow testing

## Technical Specifications

### Entity Relationships
```
Project (1) -> (N) RequirementsAnalysis
RequirementsAnalysis (1) -> (N) ProjectPlanning  
ProjectPlanning (1) -> (N) StoryGeneration
StoryGeneration (1) -> (N) PromptGeneration
Review (1) -> (1) [Any workflow entity]
```

### Critical Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=database;Port=5432;Database=aiprojectorchestrator;Username=postgres;Password=postgres123"
  }
}
```

### Service Registration Pattern
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
    
builder.Services.AddScoped<IRepository<Project>, Repository<Project>>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IReviewService, ReviewService>(); // Critical fix
```

## Definition of Done
- [ ] All unit tests pass (current: 101 tests)
- [ ] All integration tests pass with PostgreSQL
- [ ] Complete 4-stage workflow functions without data loss
- [ ] Application restart preserves all workflow state
- [ ] No breaking changes to existing API contracts
- [ ] Docker compose up creates and initializes database
- [ ] Review service singleton issue resolved
- [ ] Performance acceptable (queries <100ms for typical operations)

## Out of Scope
- Database connection pooling optimization
- Advanced caching strategies
- Database backup/restore functionality
- Multi-tenant database design
- Advanced query optimization beyond basic indexing

## Risk Mitigation
- **Breaking Changes**: Maintain existing interfaces and API contracts
- **Data Loss**: Implement proper transaction handling and rollback
- **Performance**: Index frequently queried columns (ProjectId, Status, CreatedDate)
- **Configuration**: Validate connection strings and database availability on startup

## Success Validation
After implementation, the following workflow should work seamlessly:
1. Create project → restart container → project still exists
2. Complete stages 1-3 → restart container → resume at stage 4
3. Submit reviews → restart container → reviews still pending/approved
4. Generate prompts → restart container → prompts available for download

This migration transforms the AI Project Orchestrator from a development prototype into a production-ready application with proper data persistence and enterprise-grade reliability.                                                                                                             
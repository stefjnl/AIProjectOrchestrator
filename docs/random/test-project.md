# Code Review Tool - Project Outline

## Project Overview
Local git repository analyzer with dual-engine architecture for .NET backend and JavaScript/HTML frontend code review. Custom markdown rule definitions with optional execution simulation.

## Core Features
- **Local Git Integration:** Analyze unstaged changes and modified files
- **Dual Analysis Engines:** Roslyn for .NET, AST parsing for JavaScript/HTML
- **Custom Rules:** Markdown-based rule definitions organized by category
- **Execution Simulation:** Optional V8 sandbox for runtime error detection
- **Unified Reporting:** Combined results from both analysis engines

## Technical Architecture

### Backend (.NET 9)
- Console application or minimal API
- **LibGit2Sharp** for git repository scanning
- **Roslyn Analyzers** for C# code analysis
- **Jint** for JavaScript execution simulation
- Markdown rule parser and engine

### Frontend (HTML/JavaScript)
- Simple web interface with analysis configuration
- Results dashboard with tabbed views (Static/Runtime issues)
- Simulation toggle with performance impact display
- Report export functionality

### File Structure
```
/src
  /CodeReviewTool.Core
  /CodeReviewTool.Analyzers
  /CodeReviewTool.Web
/rules
  /clean-code.md
  /solid-principles.md
  /frontend-patterns.md
  /dotnet-conventions.md
/frontend
  /index.html
  /js/
  /css/
```

## Analysis Capabilities

### .NET Backend Analysis (Roslyn)
- SOLID principle violations
- Clean Code issues (method length, complexity)
- .NET-specific antipatterns
- Architecture violations
- Resource management patterns
- Async/await usage validation

### Frontend Analysis (JavaScript AST)
- **Static Analysis:**
  - Undefined global variables
  - DOM safety checks
  - Dependency resolution
  - Module loading validation
- **Runtime Simulation:**
  - Execution error capture
  - DOM manipulation failures
  - Async race conditions
  - Event handler binding issues

## Rule System

### Markdown Rule Format
```markdown
# Rule: [Rule Name]
**Severity:** Error/Warning/Info
**Category:** [Category]
**Pattern:** [Description]
**AST Check:** [Technical implementation]
**Example:** [Code example]
```

### Rule Categories
- Clean Code principles
- SOLID principles
- Frontend dependency patterns
- .NET conventions
- Architecture patterns

## Implementation Phases

### Phase 1: Core Foundation
- Git integration and file scanning
- Basic AST parsing for both engines
- Static analysis implementation
- Simple HTML interface with simulation toggle (disabled)

### Phase 2: Static Analysis
- Complete rule engine with markdown parsing
- Roslyn analyzer integration
- JavaScript scope and dependency analysis
- Unified reporting system

### Phase 3: Execution Simulation
- Jint integration for controlled JavaScript execution
- Mock DOM environment
- Runtime error capture
- Enhanced reporting with simulation results

## Security & Performance

### Execution Simulation Safeguards
- Isolated V8 context with Jint
- 10-second execution timeout
- Memory limits for infinite loop prevention
- No network calls or file system access
- Whitelisted global APIs only

### Performance Considerations
- Analysis time tracking
- Optional simulation reduces default processing time
- Incremental analysis for large repositories
- Caching for repeated rule evaluations

## Target User
Individual developer (single-user, no authentication required)

## Success Metrics
- Catches integration issues like undefined global variables
- Identifies architectural violations before runtime
- Reduces debugging time for frontend dependency issues
- Provides actionable feedback with specific line numbers and fixess
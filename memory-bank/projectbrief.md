# Project Brief

## Overview
The AI Project Orchestrator is an enterprise-grade .NET application designed to automate development workflows through intelligent AI model coordination. It serves as a system that routes requirements through structured phases: analysis, planning, story generation, and implementation, leveraging multiple AI providers.

## Core Requirements
- **Automation Pipeline**: Automatically process user requirements into actionable development tasks using AI models.
- **Multi-Provider Support**: Integrate with Claude API, LM Studio (local), and OpenRouter for diverse AI capabilities.
- **Clean Architecture**: Maintain strict separation of concerns across Domain, Application, Infrastructure, and API layers.
- **Scalability and Maintainability**: Ensure the system is testable, observable, and extensible for enterprise use.

## Key Goals
- Build a .NET 9 Web API as the primary interface.
- Implement workflow orchestration with human approval checkpoints.
- Support Docker containerization for development and deployment.
- Achieve high code quality adhering to SOLID principles, .NET best practices, and structured logging.

## Scope Boundaries
- Focus on backend orchestration; frontend integration as secondary.
- Database persistence with PostgreSQL and EF Core.
- No direct code generation execution; focus on planning and prompting.

This document serves as the foundation for all Memory Bank files and project decisions.

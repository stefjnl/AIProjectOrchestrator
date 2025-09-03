# AI Project Orchestrator - Web UI

Welcome to the AI Project Orchestrator Web UI! This is a Blazor Server application that provides a complete web interface for managing AI-assisted software development workflows.

## What is this application?

The AI Project Orchestrator is a tool that helps automate the software development process by using AI to:
1. Analyze project ideas and convert them into structured requirements
2. Create project plans with roadmaps and architecture decisions
3. Generate user stories with acceptance criteria
4. (Future) Generate working code implementations

This Web UI allows you to interact with all these features through an easy-to-use web interface.

## Prerequisites

Before you can use this application, you need to have the following installed:
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- The AI Project Orchestrator API must be running (see separate documentation)

## Getting Started

### 1. Clone the Repository
If you haven't already, clone the repository to your local machine:
```bash
git clone <repository-url>
cd AIProjectOrchestrator
```

### 2. Start the API
The Web UI depends on the API to function. Make sure the API is running:
```bash
cd src/AIProjectOrchestrator.API
dotnet run
```

By default, the API runs on `https://localhost:5001`.

### 3. Configure the Web UI
Check the `appsettings.json` file in the Web project to ensure it's pointing to the correct API URL:
```json
{
  "ConnectionStrings": {
    "APIBaseUrl": "https://localhost:5001"
  }
}
```

Update this URL if your API is running on a different address.

### 4. Run the Web UI
Open a new terminal/command prompt and navigate to the Web project:
```bash
cd src/AIProjectOrchestrator.Web
dotnet run
```

The application will start and be available at `https://localhost:5002` by default.

When running with Docker, the Web UI will be accessible at http://localhost:8087

### 5. Access the Application
Open your web browser and navigate to `https://localhost:5002` (when running locally) or http://localhost:8087 (when running with Docker).

## Using the Application

### Dashboard
When you first access the application, you'll see the dashboard. This is your starting point and provides:
- Quick actions to create new projects
- System status information
- Recent activity feed

### Creating Your First Project
1. Click on "Projects" in the left navigation menu
2. Click the "Create New Project" button
3. Fill in the project name and description
4. Click "Create Project"

### Working with the AI Workflow
Each project follows a 4-stage workflow:

#### Stage 1: Requirements Analysis
1. Navigate to a project's details page
2. Click on the "Requirements" tab
3. Click "Start Analysis" to send your project description to the AI
4. The AI will analyze your project and generate structured requirements
5. Once complete, the output will be sent for human review

#### Stage 2: Project Planning
1. After requirements are approved, click on the "Planning" tab
2. Click "Start Planning" to generate a project plan
3. The AI will create a roadmap, architecture decisions, and milestones
4. Once complete, the output will be sent for human review

#### Stage 3: Story Generation
1. After planning is approved, click on the "Stories" tab
2. Click "Generate Stories" to create user stories
3. The AI will generate implementable user stories with acceptance criteria
4. Once complete, the output will be sent for human review

### Review Process
All AI-generated content must be reviewed by a human before proceeding to the next stage:
1. Navigate to the "Review Queue" in the main menu
2. Find the item you want to review
3. Click "Review" to open the review workspace
4. Read the AI-generated content
5. Either "Approve" to continue to the next stage or "Reject" with feedback
6. If you reject, you can provide feedback on what needs to be improved

## Features Overview

### Project Management
- **Create Projects**: Start new AI-assisted development projects
- **View Projects**: See a list of all your projects
- **Project Details**: Deep-dive into a specific project's workflow
- **Delete Projects**: Remove projects you no longer need

### Workflow Visualization
- **Progress Tracking**: See where each project is in the 4-stage workflow
- **Status Indicators**: Visual indicators for each stage (Processing, Pending Review, Approved, Rejected)
- **Real-time Updates**: Status updates happen in real-time through SignalR

### Review System
- **Centralized Queue**: All pending reviews in one place
- **Review Workspace**: Dedicated interface for reviewing AI-generated content
- **Approval Workflow**: Simple approve/reject system with feedback
- **History Tracking**: Audit trail of all review decisions

### Real-time Communication
- **SignalR Integration**: Real-time status updates without page refreshes
- **Connection Management**: Automatic reconnection if the connection is lost

## Technical Details

### Architecture
- **Blazor Server**: Server-side rendering for fast, responsive UI
- **SignalR**: Real-time communication between server and client
- **Clean Architecture**: Proper separation of concerns
- **Dependency Injection**: Proper service management

### Browser Support
The application works with all modern browsers:
- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

### Responsive Design
The UI works on:
- Desktop computers
- Laptops
- Tablets
- Mobile phones

## Troubleshooting

### Common Issues

**API Not Connected**
- Ensure the API is running on the correct URL
- Check the `appsettings.json` file for the correct API base URL
- Make sure there are no firewall or network issues

**Page Not Loading**
- Try refreshing the page
- Check the browser's developer console for errors (F12)
- Ensure all prerequisites are installed

**Real-time Updates Not Working**
- Check your internet connection
- The application will automatically reconnect, but you can refresh the page if needed

### Getting Help
If you encounter issues:
1. Check the browser's developer console for error messages
2. Verify the API is running and accessible
3. Check that all prerequisites are correctly installed

## Development Information

### Project Structure
```
AIProjectOrchestrator.Web/
├── Pages/                 # Main pages (routes)
├── Components/            # Reusable UI components
│   ├── Layout/            # Main layout components
│   ├── Workflow/          # Workflow-specific components
│   ├── Review/            # Review-specific components
│   └── Common/            # General-purpose components
├── Services/              # Client services
├── Hubs/                  # SignalR hubs
├── wwwroot/               # Static assets
└── ...
```

### Adding New Features
To extend the application:
1. Add new pages in the `Pages/` directory
2. Create reusable components in the `Components/` directory
3. Add new services in the `Services/` directory
4. Register services in `Program.cs`

### Running Tests
To run the Web UI tests:
```bash
cd tests/AIProjectOrchestrator.Web.Tests
dotnet test
```

## Contributing
If you'd like to contribute to this project:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License
This project is licensed under the MIT License - see the LICENSE file for details.

## Contact
For questions or support, please open an issue in the repository.
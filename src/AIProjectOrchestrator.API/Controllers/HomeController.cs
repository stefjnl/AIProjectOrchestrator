using Microsoft.AspNetCore.Mvc;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet("/")]
        public IActionResult Get()
        {
            var apiInfo = new
            {
                Service = "AI Project Orchestrator API",
                Version = "1.0.0",
                Description = "Backend API for AI-powered project orchestration",
                Endpoints = new
                {
                    Projects = new
                    {
                        GetAll = "GET /api/projects",
                        GetById = "GET /api/projects/{id}",
                        Create = "POST /api/projects",
                        Update = "PUT /api/projects/{id}",
                        Delete = "DELETE /api/projects/{id}"
                    },
                    Requirements = new
                    {
                        Analyze = "POST /api/requirements/analyze",
                        Status = "GET /api/requirements/{analysisId}/status"
                    },
                    Planning = new
                    {
                        Create = "POST /api/planning/create",
                        Status = "GET /api/planning/{planningId}/status",
                        CanCreate = "GET /api/planning/can-create/{requirementsAnalysisId}"
                    },
                    Stories = new
                    {
                        Generate = "POST /api/stories/generate",
                        Status = "GET /api/stories/{generationId}/status",
                        Results = "GET /api/stories/{generationId}/results",
                        CanGenerate = "GET /api/stories/can-generate/{projectPlanningId}"
                    },
                    Review = new
                    {
                        Get = "GET /api/review/{reviewId}",
                        Approve = "POST /api/review/{reviewId}/approve",
                        Reject = "POST /api/review/{reviewId}/reject",
                        Pending = "GET /api/review/pending"
                    },
                    Health = "GET /health",
                    Swagger = "GET /swagger"
                },
                WebUI = new
                {
                    Url = "http://localhost:8087",
                    Description = "Blazor Server Web Interface"
                },
                Status = "Running"
            };

            return Ok(apiInfo);
        }
    }
}

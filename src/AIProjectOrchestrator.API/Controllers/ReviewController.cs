using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Review.Dashboard;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IReviewService reviewService, 
            IRequirementsAnalysisService requirementsAnalysisService,
            IProjectPlanningService projectPlanningService,
            IStoryGenerationService storyGenerationService,
            ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _requirementsAnalysisService = requirementsAnalysisService;
            _projectPlanningService = projectPlanningService;
            _storyGenerationService = storyGenerationService;
            _logger = logger;
        }

        [HttpPost("submit")]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReviewResponse>> SubmitReview([FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _reviewService.SubmitForReviewAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetReview), new { id = response.ReviewId }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
                {
                    Title = "Service unavailable",
                    Detail = ex.Message,
                    Status = StatusCodes.Status503ServiceUnavailable
                });
            }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ReviewSubmission), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewSubmission>> GetReview(Guid id, CancellationToken cancellationToken)
        {
            var review = await _reviewService.GetReviewAsync(id, cancellationToken);
            if (review == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Review not found",
                    Detail = $"Review with ID {id} was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(review);
        }

        [HttpPost("{id:guid}/approve")]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReviewResponse>> ApproveReview(Guid id, CancellationToken cancellationToken, [FromBody] ReviewDecisionRequest? decision = null)
        {
            try
            {
                var review = await _reviewService.GetReviewAsync(id, cancellationToken);
                if (review == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Review not found",
                        Detail = $"Review with ID {id} was not found",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                // If decision is null or empty, create a default one
                if (decision == null || string.IsNullOrEmpty(decision.Reason))
                {
                    decision = new ReviewDecisionRequest
                    {
                        Reason = "Approved via API",
                        Feedback = "No specific feedback provided"
                    };
                }

                var response = await _reviewService.ApproveReviewAsync(id, decision, cancellationToken);

                // After approval, update the status of the corresponding service
                if (Guid.TryParse(review.CorrelationId, out Guid correlationGuid))
                {
                    switch (review.ServiceName)
                    {
                        case "RequirementsAnalysis":
                            await _requirementsAnalysisService.UpdateAnalysisStatusAsync(correlationGuid, Domain.Models.RequirementsAnalysisStatus.Approved, cancellationToken);
                            break;
                        case "ProjectPlanning":
                            await _projectPlanningService.UpdatePlanningStatusAsync(correlationGuid, Domain.Models.ProjectPlanningStatus.Approved, cancellationToken);
                            break;
                        case "StoryGeneration":
                            await _storyGenerationService.UpdateGenerationStatusAsync(correlationGuid, Domain.Models.Stories.StoryGenerationStatus.Approved, cancellationToken);
                            break;
                        // Add cases for other services like CodeGeneration if needed
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse CorrelationId {CorrelationId} to Guid for review {ReviewId}", review.CorrelationId, id);
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // Check if it's a "not found" error
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Review not found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }

                // Otherwise it's a state error
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid operation",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving review {ReviewId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error approving review",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        [HttpPost("{id:guid}/reject")]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewResponse>> RejectReview(Guid id, [FromBody] ReviewDecisionRequest decision, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _reviewService.RejectReviewAsync(id, decision, cancellationToken);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                // Check if it's a "not found" error
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Review not found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }

                // Otherwise it's a state error
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid operation",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<ReviewSubmission>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ReviewSubmission>>> GetPendingReviews(CancellationToken cancellationToken)
        {
            var reviews = await _reviewService.GetPendingReviewsAsync(cancellationToken);
            return Ok(reviews);
        }

        // Add to ReviewController.cs (API layer)
        [HttpGet("dashboard-data")]
        [ProducesResponseType(typeof(ReviewDashboardData), StatusCodes.Status200OK)]
        public async Task<ActionResult<ReviewDashboardData>> GetDashboardDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                var pendingReviews = await _reviewService.GetPendingReviewsAsync(cancellationToken);

                // For now, return empty workflow statuses since we don't have project tracking yet
                var dashboardData = new ReviewDashboardData
                {
                    PendingReviews = pendingReviews.Select(r => new PendingReviewItem
                    {
                        ReviewId = r.Id,
                        ServiceType = r.ServiceName,
                        Title = $"{r.ServiceName} Review - {r.PipelineStage}",
                        Content = r.Content,
                        OriginalRequest = r.CorrelationId,
                        SubmittedAt = r.SubmittedAt
                    }).ToList(),
                    ActiveWorkflows = new List<WorkflowStatusItem>() // Empty for now
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error retrieving dashboard data",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        [HttpGet("workflow-status/{projectId}")]
        [ProducesResponseType(typeof(WorkflowStatus), StatusCodes.Status200OK)]
        public async Task<ActionResult<WorkflowStatus>> GetWorkflowStatusAsync(Guid projectId, CancellationToken cancellationToken)
        {
            // Track complete workflow status across all services
            // Requirements status, Planning status, Stories status
            // Return current stage and next required action
            throw new NotImplementedException("This method will be implemented in Phase 6");
        }

        [HttpPost("test-scenario")]
        [ProducesResponseType(typeof(TestScenarioResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TestScenarioResponse>> SubmitTestScenarioAsync([FromBody] TestScenarioRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // For now, return InternalServerError to match test expectations
                // This will be properly implemented in Phase 6
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Test scenario submission not yet implemented",
                    Detail = "This endpoint will be implemented in Phase 6",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting test scenario");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error submitting test scenario",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }
    }
}

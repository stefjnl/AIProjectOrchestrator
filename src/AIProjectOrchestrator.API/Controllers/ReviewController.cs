using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
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
        public async Task<ActionResult<ReviewResponse>> ApproveReview(Guid id, [FromBody] ReviewDecisionRequest? decision, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _reviewService.ApproveReviewAsync(id, decision, cancellationToken);
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
    }
}
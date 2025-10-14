using Microsoft.AspNetCore.Mvc;
using CodePulseApi.Services;
using CodePulseApi.DTOs;

namespace CodePulseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var reviews = await _reviewService.GetReviewsAsync(page, limit, type, status);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReview(string id)
    {
        try
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            
            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }
            
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var review = await _reviewService.CreateReviewAsync(request);
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(string id, [FromBody] UpdateReviewRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var review = await _reviewService.UpdateReviewAsync(id, request);
            
            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }
            
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("ai-review/pull-request/{prId}")]
    public async Task<IActionResult> TriggerAIReview(string prId)
    {
        try
        {
            var review = await _reviewService.TriggerAIReviewAsync(prId);
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering AI review for PR {PrId}", prId);
            return StatusCode(500, new { message = "Failed to generate AI review" });
        }
    }

    [HttpGet("stats/overview")]
    public async Task<IActionResult> GetReviewStats([FromQuery] string period = "30d")
    {
        try
        {
            var stats = await _reviewService.GetReviewStatsAsync(period);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review stats");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

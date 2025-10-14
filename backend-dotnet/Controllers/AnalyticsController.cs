using Microsoft.AspNetCore.Mvc;
using CodePulseApi.Services;

namespace CodePulseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardAnalytics([FromQuery] string period = "30d")
    {
        try
        {
            var analytics = await _analyticsService.GetDashboardAnalyticsAsync(period);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("engineer/{id}")]
    public async Task<IActionResult> GetEngineerAnalytics(string id, [FromQuery] string period = "30d")
    {
        try
        {
            var analytics = await _analyticsService.GetEngineerAnalyticsAsync(id, period);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engineer analytics for {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("repository/{id}")]
    public async Task<IActionResult> GetRepositoryAnalytics(string id, [FromQuery] string period = "30d")
    {
        try
        {
            var analytics = await _analyticsService.GetRepositoryAnalyticsAsync(id, period);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository analytics for {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("team/comparison")]
    public async Task<IActionResult> GetTeamComparison([FromQuery] string period = "30d")
    {
        try
        {
            var comparison = await _analyticsService.GetTeamComparisonAsync(period);
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team comparison");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using CodePulseApi.Services;

namespace CodePulseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(ILeaderboardService leaderboardService, ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string period = "MONTHLY",
        [FromQuery] int limit = 10,
        [FromQuery] string metric = "TotalCommits")
    {
        try
        {
            var leaderboard = await _leaderboardService.GetLeaderboardAsync(period, limit, metric);
            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("engineer/{id}")]
    public async Task<IActionResult> GetEngineerDetails(string id, [FromQuery] string period = "MONTHLY")
    {
        try
        {
            var engineer = await _leaderboardService.GetEngineerDetailsAsync(id, period);
            
            if (engineer == null)
            {
                return NotFound(new { message = "Engineer not found" });
            }
            
            return Ok(engineer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engineer details for {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetLeaderboardTrends(
        [FromQuery] string period = "MONTHLY",
        [FromQuery] string? engineerId = null)
    {
        try
        {
            var trends = await _leaderboardService.GetLeaderboardTrendsAsync(period, engineerId);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard trends");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

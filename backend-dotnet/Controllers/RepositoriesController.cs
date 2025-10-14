
using Microsoft.AspNetCore.Mvc;
using CodePulseApi.Services;
using CodePulseApi.DTOs;

namespace CodePulseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RepositoriesController : ControllerBase
{
    private readonly IRepositoryService _repositoryService;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(IRepositoryService repositoryService, ILogger<RepositoriesController> logger)
    {
        _repositoryService = repositoryService;
        _logger = logger;
    }


    [HttpGet]
    public async Task<ActionResult<List<RepositoryResponseDto>>> GetRepositories()
    {
        try
        {
            var repositories = await _repositoryService.GetRepositoriesAsync();
            return Ok(repositories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repositories");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<RepositoryResponseDto>> GetRepository(string id)
    {
        try
        {
            var repository = await _repositoryService.GetRepositoryByIdAsync(id);
            if (repository == null)
            {
                return NotFound(new { message = "Repository not found" });
            }
            return Ok(repository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<AnalyticsDataDto>> GetRepositoryAnalytics(string id, [FromQuery] string period = "30d")
    {
        try
        {
            var analytics = await _repositoryService.GetRepositoryAnalyticsAsync(id, period);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository analytics for {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

using backend_dotnet.Services;
using backend_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Controllers
{
    [ApiController]
    [Route("api/repository")]
    [Authorize] // Require authentication for all endpoints
    public class RepositoryController : ControllerBase
    {
        private readonly RepositoryGatewayService _gatewayService;
        private readonly RepositoryIntegrationService _integrationService;

        public RepositoryController(RepositoryGatewayService gatewayService, RepositoryIntegrationService integrationService)
        {
            _gatewayService = gatewayService;
            _integrationService = integrationService;
        }

        // Create or update repository integration
        [HttpPost("integration")]
        public async Task<IActionResult> CreateOrUpdateIntegration([FromBody] RepositoryIntegration integration)
        {
            if (string.IsNullOrWhiteSpace(integration.RepoId) || string.IsNullOrWhiteSpace(integration.Token) || string.IsNullOrWhiteSpace(integration.Provider))
                return BadRequest("RepoId, Token, and Provider are required.");
            try
            {
                var result = await _integrationService.UpsertIntegrationAsync(integration);
                // Do not return token in response
                result.Token = null;
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while saving integration.");
            }
        }

        // Get repository integration by repoId
        [HttpGet("integration/{repoId}")]
        public async Task<IActionResult> GetIntegration(string repoId)
        {
            try
            {
                var result = await _integrationService.GetIntegrationAsync(repoId);
                if (result == null) return NotFound();
                result.Token = null; // Do not expose token
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching integration.");
            }
        }

        // Delete repository integration
        [HttpDelete("integration/{repoId}")]
        [Authorize(Roles = "Admin")] // Restrict to admin
        public async Task<IActionResult> DeleteIntegration(string repoId)
        {
            try
            {
                await _integrationService.DeleteIntegrationAsync(repoId);
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while deleting integration.");
            }
        }

        // List all integrations
        [HttpGet("integrations")]
        [Authorize(Roles = "Admin")] // Restrict to admin
        public async Task<IActionResult> ListIntegrations()
        {
            try
            {
                var result = await _integrationService.ListIntegrationsAsync();
                // Remove tokens from all results
                foreach (var item in result) item.Token = null;
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while listing integrations.");
            }
        }

        // Update integration (partial update)
        [HttpPatch("integration/{repoId}")]
        public async Task<IActionResult> UpdateIntegration(string repoId, [FromBody] RepositoryIntegration integration)
        {
            try
            {
                var result = await _integrationService.UpdateIntegrationAsync(repoId, integration);
                result.Token = null;
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while updating integration.");
            }
        }

        [HttpGet("{repoId}/details")]
        public async Task<IActionResult> GetRepositoryDetails(string repoId, [FromQuery] string owner, [FromQuery] string repo)
        {
            try
            {
                var result = await _gatewayService.GetRepositoryAsync(repoId, owner, repo);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching repository details.");
            }
        }

        [HttpGet("{repoId}/file")]
        public async Task<IActionResult> GetFileContent(string repoId, [FromQuery] string owner, [FromQuery] string repo, [FromQuery] string path)
        {
            try
            {
                var result = await _gatewayService.GetFileContentAsync(repoId, owner, repo, path);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching file content.");
            }
        }

        [HttpGet("{repoId}/prs")]
        public async Task<IActionResult> GetPullRequests(string repoId, [FromQuery] string owner, [FromQuery] string repo)
        {
            try
            {
                var result = await _gatewayService.GetPullRequestsAsync(repoId, owner, repo);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching pull requests.");
            }
        }

        [HttpGet("{repoId}/pr/{prNumber}/comments")]
        public async Task<IActionResult> GetPullRequestComments(string repoId, [FromQuery] string owner, [FromQuery] string repo, int prNumber)
        {
            try
            {
                var result = await _gatewayService.GetPullRequestCommentsAsync(repoId, owner, repo, prNumber);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching PR comments.");
            }
        }
    }
}

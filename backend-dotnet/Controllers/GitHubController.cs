using Microsoft.AspNetCore.Mvc;
using backend_dotnet.Services;
using System.Threading.Tasks;

namespace backend_dotnet.Controllers
{
    [ApiController]
    [Route("api/github")]
    public class GitHubController : ControllerBase
    {
        private readonly GitHubService _gitHubService;

        public GitHubController(GitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        [HttpGet("repo/{owner}/{repo}")]
        public async Task<IActionResult> GetRepository(string owner, string repo, [FromQuery] string token, [FromQuery] string? baseUrl)
        {
            var result = await _gitHubService.GetRepositoryAsync(owner, repo, token, baseUrl ?? "https://api.github.com/");
            return Ok(result);
        }

        [HttpGet("repo/{owner}/{repo}/file")]
        public async Task<IActionResult> GetFileContent(string owner, string repo, [FromQuery] string path, [FromQuery] string token, [FromQuery] string? baseUrl)
        {
            var result = await _gitHubService.GetFileContentAsync(owner, repo, path, token, baseUrl ?? "https://api.github.com/");
            return Ok(result);
        }

        [HttpGet("repo/{owner}/{repo}/prs")]
        public async Task<IActionResult> GetPullRequests(string owner, string repo, [FromQuery] string token, [FromQuery] string? baseUrl)
        {
            var result = await _gitHubService.GetPullRequestsAsync(owner, repo, token, baseUrl ?? "https://api.github.com/");
            return Ok(result);
        }

        [HttpGet("repo/{owner}/{repo}/pr/{prNumber}/comments")]
        public async Task<IActionResult> GetPullRequestComments(string owner, string repo, int prNumber, [FromQuery] string token, [FromQuery] string? baseUrl)
        {
            var result = await _gitHubService.GetPullRequestCommentsAsync(owner, repo, prNumber, token, baseUrl ?? "https://api.github.com/");
            return Ok(result);
        }
    }
}

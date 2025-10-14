using backend_dotnet.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace backend_dotnet.Services
{
    public class RepositoryGatewayService
    {
        private readonly RepositoryIntegrationService _integrationService;
        private readonly GitHubService _gitHubService;
        // Add GitLabService, BitbucketService as needed

        public RepositoryGatewayService(RepositoryIntegrationService integrationService, GitHubService gitHubService)
        {
            _integrationService = integrationService;
            _gitHubService = gitHubService;
        }

        public async Task<JObject> GetRepositoryAsync(string repoId, string owner, string repo)
        {
            var integration = await _integrationService.GetIntegrationAsync(repoId);
            switch (integration.Provider?.ToLower())
            {
                case "github":
                    return await _gitHubService.GetRepositoryAsync(owner, repo, integration.Token!, integration.BaseUrl!);
                // case "gitlab": return await _gitLabService.GetRepositoryAsync(...);
                // case "bitbucket": return await _bitbucketService.GetRepositoryAsync(...);
                default:
                    throw new System.Exception("Unsupported provider");
            }
        }

        public async Task<JObject> GetFileContentAsync(string repoId, string owner, string repo, string path)
        {
            var integration = await _integrationService.GetIntegrationAsync(repoId);
            switch (integration.Provider?.ToLower())
            {
                case "github":
                    return await _gitHubService.GetFileContentAsync(owner, repo, path, integration.Token!, integration.BaseUrl!);
                // case "gitlab": return await _gitLabService.GetFileContentAsync(...);
                // case "bitbucket": return await _bitbucketService.GetFileContentAsync(...);
                default:
                    throw new System.Exception("Unsupported provider");
            }
        }

        public async Task<JArray> GetPullRequestsAsync(string repoId, string owner, string repo)
        {
            var integration = await _integrationService.GetIntegrationAsync(repoId);
            switch (integration.Provider?.ToLower())
            {
                case "github":
                    return await _gitHubService.GetPullRequestsAsync(owner, repo, integration.Token!, integration.BaseUrl!);
                // case "gitlab": return await _gitLabService.GetPullRequestsAsync(...);
                // case "bitbucket": return await _bitbucketService.GetPullRequestsAsync(...);
                default:
                    throw new System.Exception("Unsupported provider");
            }
        }

        public async Task<JArray> GetPullRequestCommentsAsync(string repoId, string owner, string repo, int prNumber)
        {
            var integration = await _integrationService.GetIntegrationAsync(repoId);
            switch (integration.Provider?.ToLower())
            {
                case "github":
                    return await _gitHubService.GetPullRequestCommentsAsync(owner, repo, prNumber, integration.Token!, integration.BaseUrl!);
                // case "gitlab": return await _gitLabService.GetPullRequestCommentsAsync(...);
                // case "bitbucket": return await _bitbucketService.GetPullRequestCommentsAsync(...);
                default:
                    throw new System.Exception("Unsupported provider");
            }
        }
    }
}

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace backend_dotnet.Services
{
    public class GitHubService
    {
        private HttpClient CreateClient(string token, string baseUrl = "https://api.github.com/")
        {
            var client = new HttpClient();
            client.BaseAddress = new System.Uri(baseUrl);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CodepulseAi", "1.0"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<JObject> GetRepositoryAsync(string owner, string repo, string token, string baseUrl = "https://api.github.com/")
        {
            using var client = CreateClient(token, baseUrl);
            var response = await client.GetAsync($"repos/{owner}/{repo}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }

        public async Task<JObject> GetFileContentAsync(string owner, string repo, string path, string token, string baseUrl = "https://api.github.com/")
        {
            using var client = CreateClient(token, baseUrl);
            var response = await client.GetAsync($"repos/{owner}/{repo}/contents/{path}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }

        public async Task<JArray> GetPullRequestsAsync(string owner, string repo, string token, string baseUrl = "https://api.github.com/")
        {
            using var client = CreateClient(token, baseUrl);
            var response = await client.GetAsync($"repos/{owner}/{repo}/pulls");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JArray.Parse(content);
        }

        public async Task<JArray> GetPullRequestCommentsAsync(string owner, string repo, int prNumber, string token, string baseUrl = "https://api.github.com/")
        {
            using var client = CreateClient(token, baseUrl);
            var response = await client.GetAsync($"repos/{owner}/{repo}/issues/{prNumber}/comments");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JArray.Parse(content);
        }
    }
}

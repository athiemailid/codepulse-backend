
using CodePulseApi.Models;
using CodePulseApi.DTOs;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodePulseApi.Services;

public class RepositoryService : IRepositoryService
{
    private readonly Container _container;
    private readonly ILogger<RepositoryService> _logger;

    public RepositoryService(IConfiguration configuration, ILogger<RepositoryService> logger)
    {
        _logger = logger;
        var cosmosClient = new CosmosClient(configuration["CosmosDb:ConnectionString"]);
        var databaseName = configuration["CosmosDb:DatabaseName"];
        var containerName = configuration["CosmosDb:RepositoryContainerName"];
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<List<RepositoryResponseDto>> GetRepositoriesAsync()
    {
        var query = "SELECT * FROM c WHERE c.IsActive = true";
        var iterator = _container.GetItemQueryIterator<Repository>(query);
        var results = new List<RepositoryResponseDto>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var repo in response)
            {
                results.Add(MapToRepositoryResponseDto(repo));
            }
        }
        return results;
    }

    public async Task<RepositoryResponseDto?> GetRepositoryByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Repository>(id, new PartitionKey(id));
            return MapToRepositoryResponseDto(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Repository not found: {Id}", id);
            return null;
        }
    }

    public async Task<AnalyticsDataDto> GetRepositoryAnalyticsAsync(string id, string period = "30d")
    {
        var repo = await GetRepositoryByIdAsync(id);
        if (repo == null)
        {
            return new AnalyticsDataDto();
        }
        // Example: return basic stats
        return new AnalyticsDataDto
        {
            Period = period,
            TotalCommits = repo.TotalCommits,
            TotalPullRequests = repo.TotalPullRequests,
            TotalRepositories = 1,
            RepositoryStats = new List<RepositoryStatsDto> {
                new RepositoryStatsDto {
                    RepositoryId = repo.Id,
                    RepositoryName = repo.Name,
                    TotalCommits = repo.TotalCommits,
                    TotalPullRequests = repo.TotalPullRequests
                }
            }
        };
    }

    private RepositoryResponseDto MapToRepositoryResponseDto(Repository repo)
    {
        return new RepositoryResponseDto
        {
            Id = repo.Id,
            Name = repo.Name,
            Url = repo.Url,
            ProjectId = repo.ProjectId,
            DefaultBranch = repo.DefaultBranch,
            IsActive = repo.IsActive,
            CreatedAt = repo.CreatedAt,
            UpdatedAt = repo.UpdatedAt,
            TotalPullRequests = repo.PullRequests?.Count ?? 0,
            TotalCommits = repo.Commits?.Count ?? 0,
            RecentPullRequests = new List<PullRequestResponseDto>(), // Fill if needed
            RecentCommits = new List<CommitResponseDto>() // Fill if needed
        };
    }
}

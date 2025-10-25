using CodePulseApi.DTOs;
using CodePulseApi.Data;
using CodePulseApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using CodePulseApi.DTOs;

namespace CodePulseApi.Services;

public class WebhookService : IWebhookService
{
    private readonly CodePulseDbContext _context;
    private readonly IAzureAIFoundryService _aiService;
    private readonly ILogger<WebhookService> _logger;
    private const string GitHubSecret = "3q2+7w==kL9x8Yz1JH5vT2aB4cD9eF==";

    public WebhookService(
        CodePulseDbContext context,
        IAzureAIFoundryService aiService,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<bool> ProcessWebhookAsync(object webhook, string provider, string? signature = null)
    {
        try
        {
            if (provider.Equals("AzureDevOps", StringComparison.OrdinalIgnoreCase) && webhook is AzureDevOpsWebhookDto azureWebhook)
            {
                return await ProcessAzureDevOpsWebhookAsync(azureWebhook);
            }
            else if (provider.Equals("GitHub", StringComparison.OrdinalIgnoreCase) && webhook is GitHubWebhookDto githubWebhook)
            {
                if (!IsValidGitHubSignature(signature, JsonSerializer.Serialize(githubWebhook)))
                {
                    _logger.LogWarning("Invalid GitHub signature");
                    return false;
                }

                return await ProcessWebhookAsync(githubWebhook);
            }
            else
            {
                _logger.LogWarning("Unknown webhook provider or invalid payload");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return false;
        }
    }

    public async Task<bool> ProcessWebhookAsync(AzureDevOpsWebhookDto webhook)
    {
        return await ProcessAzureDevOpsWebhookAsync(webhook);
    }

    private async Task<bool> ProcessAzureDevOpsWebhookAsync(AzureDevOpsWebhookDto webhook)
    {
        _logger.LogInformation("Processing Azure DevOps webhook: {WebhookPayload}", JsonSerializer.Serialize(webhook));

        // Store webhook event for audit
        var webhookEvent = new WebhookEvent
        {
            EventType = webhook.EventType,
            Payload = JsonSerializer.Serialize(webhook),
            Processed = false
        };

        _context.WebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync();

        bool processed = false;  

        // Update webhook event status
        webhookEvent.Processed = processed;
        webhookEvent.ProcessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return processed;
    }
    public async Task<bool> ProcessWebhookAsync(GitHubWebhookDto webhook)
    {
        if (webhook == null)
        {
            _logger.LogError("Webhook payload is null.");
            return false;
        }

        if (string.IsNullOrEmpty(webhook.Ref))
        {
            _logger.LogWarning("Missing 'ref' field in webhook payload.");
            return false;
        }

        if (webhook.Repository == null || webhook.Repository.Id == 0)
        {
            _logger.LogWarning("Missing or invalid 'repository' field in webhook payload.");
            return false;
        }

        // Process the webhook (example logic)
        _logger.LogInformation("Processing webhook for repository: {RepoName}", webhook.Repository.FullName);
        return true;
    }

    private Task<bool> HandleAzurePullRequestEventAsync(AzureDevOpsWebhookDto webhook)
    {
        _logger.LogInformation("Handling Azure DevOps pull request event");
        // Add logic for handling Azure pull request events
        return Task.FromResult(true);
    }

    //private Task<bool> HandleAzureGitPushEventAsync(AzureDevOpsWebhookDto webhook)
    //{
    //    _logger.LogInformation("Handling Azure DevOps git push event");
    //    // Add logic for handling Azure git push events
    //    return Task.FromResult(true);
    //}

    //private async Task<bool> HandleGitHubPushEventAsync(GitHubWebhookDto webhook)
    //{
    //    _logger.LogInformation("Processing GitHub push event");

    //    // Extract repository and commits from the payload
    //    if (webhook.Payload is JsonElement payload)
    //    {
    //        var repository = payload.GetProperty("repository");
    //        var commits = payload.GetProperty("commits");

    //        var repoName = repository.GetProperty("name").GetString() ?? string.Empty;
    //        var repoUrl = repository.GetProperty("html_url").GetString() ?? string.Empty;

    //        if (string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(repoUrl))
    //        {
    //            _logger.LogWarning("Missing repository information in GitHub webhook");
    //            return false;
    //        }

    //        var repoEntity = await GetOrCreateRepositoryAsync(repoName, repoUrl);

    //        foreach (var commit in commits.EnumerateArray())
    //        {
    //            var commitId = commit.GetProperty("id").GetString() ?? string.Empty;
    //            var message = commit.GetProperty("message").GetString() ?? string.Empty;
    //            var author = commit.GetProperty("author");
    //            var authorName = author.GetProperty("name").GetString() ?? string.Empty;
    //            var authorEmail = author.GetProperty("email").GetString() ?? string.Empty;

    //            var existingCommit = await _context.Commits
    //                .FirstOrDefaultAsync(c => c.CommitId == commitId && c.RepositoryId == repoEntity.Id);

    //            if (existingCommit != null)
    //            {
    //                continue; // Skip existing commits
    //            }

    //            var engineer = await GetOrCreateEngineerAsync(authorName, authorEmail);

    //            var newCommit = new Commit
    //            {
    //                CommitId = commitId,
    //                Message = message,
    //                Author = authorName,
    //                AuthorEmail = authorEmail,
    //                CommitDate = DateTime.UtcNow,
    //                Url = commit.GetProperty("url").GetString() ?? string.Empty,
    //                RepositoryId = repoEntity.Id,
    //                AuthorId = engineer.Id
    //            };

    //            _context.Commits.Add(newCommit);
    //            _logger.LogInformation("Created commit {CommitHash} by {AuthorName} in repository {RepositoryName}",
    //                commitId, authorName, repoName);
    //        }

    //        await _context.SaveChangesAsync();
    //        return true;
    //    }

    //    _logger.LogWarning("Invalid payload format for GitHub push event");
    //    return false;
    //}

    //private async Task<bool> HandleGitHubPullRequestEventAsync(GitHubWebhookDto webhook)
    //{
    //    _logger.LogInformation("Processing GitHub pull request event");

    //    if (webhook.Payload is JsonElement payload)
    //    {
    //        var pullRequest = payload.GetProperty("pull_request");
    //        var repository = payload.GetProperty("repository");

    //        var repoName = repository.GetProperty("name").GetString() ?? string.Empty;
    //        var repoUrl = repository.GetProperty("html_url").GetString() ?? string.Empty;

    //        if (string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(repoUrl))
    //        {
    //            _logger.LogWarning("Missing repository information in GitHub webhook");
    //            return false;
    //        }

    //        var repoEntity = await GetOrCreateRepositoryAsync(repoName, repoUrl);

    //        var prId = pullRequest.GetProperty("id").GetInt32();
    //        var title = pullRequest.GetProperty("title").GetString() ?? string.Empty;
    //        var description = pullRequest.GetProperty("body").GetString() ?? string.Empty;
    //        var status = pullRequest.GetProperty("state").GetString() ?? string.Empty;
    //        var prUrl = pullRequest.GetProperty("html_url").GetString() ?? string.Empty;

    //        var existingPR = await _context.PullRequests
    //            .FirstOrDefaultAsync(pr => pr.PrId == prId && pr.RepositoryId == repoEntity.Id);

    //        if (existingPR == null)
    //        {
    //            var newPR = new PullRequest
    //            {
    //                PrId = prId,
    //                Title = title,
    //                Description = description,
    //                Status = status,
    //                Url = prUrl,
    //                RepositoryId = repoEntity.Id,
    //                CreatedDate = DateTime.UtcNow
    //            };

    //            _context.PullRequests.Add(newPR);
    //            _logger.LogInformation("Created pull request {PullRequestId} for repository {RepositoryName}",
    //                prId, repoName);
    //        }
    //        else
    //        {
    //            existingPR.Title = title;
    //            existingPR.Description = description;
    //            existingPR.Status = status;
    //            existingPR.Url = prUrl;

    //            if (status.Equals("closed", StringComparison.OrdinalIgnoreCase))
    //            {
    //                existingPR.ClosedDate = DateTime.UtcNow;
    //            }

    //            _logger.LogInformation("Updated pull request {PullRequestId} for repository {RepositoryName}",
    //                prId, repoName);
    //        }

    //        await _context.SaveChangesAsync();
    //        return true;
    //    }

    //    _logger.LogWarning("Invalid payload format for GitHub pull request event");
    //    return false;
    //}

    private bool IsValidGitHubSignature(string? signature, string payload)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var secret = Encoding.UTF8.GetBytes(GitHubSecret);
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var hashString = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

        return hashString == signature;
    }

    private async Task<Repository> GetOrCreateRepositoryAsync(string name, string url)
    {
        var existing = await _context.Repositories
            .FirstOrDefaultAsync(r => r.Name == name || r.Url == url);

        if (existing != null)
        {
            return existing;
        }

        var repository = new Repository
        {
            Name = name,
            Url = url,
            ProjectId = "default",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Repositories.Add(repository);
        await _context.SaveChangesAsync();

        return repository;
    }

    private async Task<Engineer> GetOrCreateEngineerAsync(string name, string email)
    {
        var existing = await _context.Engineers
            .FirstOrDefaultAsync(e => e.Email == email);

        if (existing != null)
        {
            return existing;
        }

        var engineer = new Engineer
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _context.Engineers.Add(engineer);
        await _context.SaveChangesAsync();

        return engineer;
    }
}

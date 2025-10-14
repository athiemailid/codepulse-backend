using CodePulseApi.DTOs;
using CodePulseApi.Data;
using CodePulseApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodePulseApi.Services;

public class WebhookService : IWebhookService
{
    private readonly CodePulseDbContext _context;
    private readonly IAzureAIFoundryService _aiService;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        CodePulseDbContext context, 
        IAzureAIFoundryService aiService, 
        ILogger<WebhookService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<bool> ProcessWebhookAsync(AzureDevOpsWebhookDto webhook)
    {
        try
        {
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

            switch (webhook.EventType)
            {
                case "git.pullrequest.created":
                case "git.pullrequest.updated":
                    processed = await HandlePullRequestEventAsync(webhook);
                    break;
                case "git.push":
                    processed = await HandleGitPushEventAsync(webhook);
                    break;
                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", webhook.EventType);
                    processed = true; // Mark as processed even if we don't handle it
                    break;
            }

            // Update webhook event status
            webhookEvent.Processed = processed;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return processed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return false;
        }
    }

    private async Task<bool> HandlePullRequestEventAsync(AzureDevOpsWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation("Processing pull request event: {EventType}", webhook.EventType);

            // Parse resource as JsonElement for proper JSON handling
            var resourceJson = JsonSerializer.Serialize(webhook.Resource);
            var resource = JsonSerializer.Deserialize<JsonElement>(resourceJson);
            
            if (!resource.TryGetProperty("repository", out var repository) ||
                !resource.TryGetProperty("pullRequestId", out var prIdElement))
            {
                _logger.LogWarning("Invalid pull request webhook payload");
                return false;
            }

            // Get repository information
            var repoName = repository.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var repoUrl = repository.TryGetProperty("remoteUrl", out var urlElement) ? urlElement.GetString() : null;
            
            if (string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(repoUrl))
            {
                _logger.LogWarning("Missing repository information in webhook");
                return false;
            }

            var repoEntity = await GetOrCreateRepositoryAsync(repoName, repoUrl);

            // Get engineer information
            var engineerName = "Unknown";
            var engineerEmail = "unknown@unknown.com";
            
            if (resource.TryGetProperty("createdBy", out var createdBy))
            {
                engineerName = createdBy.TryGetProperty("displayName", out var displayNameElement) 
                    ? displayNameElement.GetString() ?? "Unknown" : "Unknown";
                engineerEmail = createdBy.TryGetProperty("uniqueName", out var uniqueNameElement) 
                    ? uniqueNameElement.GetString() ?? "unknown@unknown.com" : "unknown@unknown.com";
            }

            var engineer = await GetOrCreateEngineerAsync(engineerName, engineerEmail);

            // Get pull request information
            var prId = prIdElement.GetInt32();
            var title = resource.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "No Title" : "No Title";
            var description = resource.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;
            var status = resource.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? "Active" : "Active";
            var sourceRef = resource.TryGetProperty("sourceRefName", out var sourceElement) ? sourceElement.GetString() ?? "" : "";
            var targetRef = resource.TryGetProperty("targetRefName", out var targetElement) ? targetElement.GetString() ?? "" : "";
            var prUrl = resource.TryGetProperty("url", out var prUrlElement) ? prUrlElement.GetString() ?? "" : "";
            var createdDate = resource.TryGetProperty("creationDate", out var dateElement) && 
                DateTime.TryParse(dateElement.GetString(), out var parsed) ? parsed : DateTime.UtcNow;

            // Check if pull request already exists
            var existingPR = await _context.PullRequests
                .FirstOrDefaultAsync(pr => pr.PrId == prId && pr.RepositoryId == repoEntity.Id);

            if (existingPR == null)
            {
                // Create new pull request
                var newPR = new PullRequest
                {
                    PrId = prId,
                    Title = title,
                    Description = description,
                    Status = status,
                    SourceRefName = sourceRef,
                    TargetRefName = targetRef,
                    Url = prUrl,
                    RepositoryId = repoEntity.Id,
                    CreatedById = engineer.Id,
                    CreatedDate = createdDate
                };

                _context.PullRequests.Add(newPR);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created pull request {PullRequestId} for repository {RepositoryName}", 
                    prId, repoName);
            }
            else
            {
                // Update existing pull request
                existingPR.Title = title;
                existingPR.Description = description;
                existingPR.Status = status;
                existingPR.SourceRefName = sourceRef;
                existingPR.TargetRefName = targetRef;
                existingPR.Url = prUrl;

                if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || 
                    status.Equals("Abandoned", StringComparison.OrdinalIgnoreCase))
                {
                    existingPR.ClosedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated pull request {PullRequestId} for repository {RepositoryName}", 
                    prId, repoName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling pull request event");
            return false;
        }
    }

    private async Task<bool> HandleGitPushEventAsync(AzureDevOpsWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation("Processing git push event: {EventType}", webhook.EventType);

            // Parse resource as JsonElement for proper JSON handling
            var resourceJson = JsonSerializer.Serialize(webhook.Resource);
            var resource = JsonSerializer.Deserialize<JsonElement>(resourceJson);

            if (!resource.TryGetProperty("repository", out var repository) ||
                !resource.TryGetProperty("commits", out var commitsElement))
            {
                _logger.LogWarning("Invalid git push webhook payload");
                return false;
            }

            // Get repository information
            var repoName = repository.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var repoUrl = repository.TryGetProperty("remoteUrl", out var urlElement) ? urlElement.GetString() : null;
            
            if (string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(repoUrl))
            {
                _logger.LogWarning("Missing repository information in webhook");
                return false;
            }

            var repoEntity = await GetOrCreateRepositoryAsync(repoName, repoUrl);

            // Process each commit
            if (commitsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var commit in commitsElement.EnumerateArray())
                {
                    var commitId = commit.TryGetProperty("commitId", out var commitIdElement) ? commitIdElement.GetString() : null;
                    var message = commit.TryGetProperty("comment", out var messageElement) ? messageElement.GetString() ?? "" : "";
                    var commitUrl = commit.TryGetProperty("url", out var commitUrlElement) ? commitUrlElement.GetString() ?? "" : "";

                    if (string.IsNullOrEmpty(commitId))
                    {
                        continue;
                    }

                    // Check if commit already exists
                    var existingCommit = await _context.Commits
                        .FirstOrDefaultAsync(c => c.CommitId == commitId && c.RepositoryId == repoEntity.Id);

                    if (existingCommit != null)
                    {
                        continue; // Skip existing commits
                    }

                    // Get author information
                    var authorName = "Unknown";
                    var authorEmail = "unknown@unknown.com";
                    var commitDate = DateTime.UtcNow;

                    if (commit.TryGetProperty("author", out var author))
                    {
                        authorName = author.TryGetProperty("name", out var authorNameElement) 
                            ? authorNameElement.GetString() ?? "Unknown" : "Unknown";
                        authorEmail = author.TryGetProperty("email", out var authorEmailElement) 
                            ? authorEmailElement.GetString() ?? "unknown@unknown.com" : "unknown@unknown.com";
                        
                        if (author.TryGetProperty("date", out var dateElement) &&
                            DateTime.TryParse(dateElement.GetString(), out var parsed))
                        {
                            commitDate = parsed;
                        }
                    }

                    // Get or create engineer
                    var engineer = await GetOrCreateEngineerAsync(authorName, authorEmail);

                    // Create commit record
                    var newCommit = new Commit
                    {
                        CommitId = commitId,
                        Message = message,
                        Author = authorName,
                        AuthorEmail = authorEmail,
                        CommitDate = commitDate,
                        Url = commitUrl,
                        RepositoryId = repoEntity.Id,
                        AuthorId = engineer.Id,
                        // Note: ChangedFiles, Additions, Deletions would need to be calculated
                        // from the actual diff, which isn't provided in the basic webhook
                        ChangedFiles = 0,
                        Additions = 0,
                        Deletions = 0
                    };

                    _context.Commits.Add(newCommit);

                    _logger.LogInformation("Created commit {CommitHash} by {AuthorName} in repository {RepositoryName}", 
                        commitId, authorName, repoName);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling git push event");
            return false;
        }
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
            ProjectId = "default", // Could be extracted from webhook if available
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

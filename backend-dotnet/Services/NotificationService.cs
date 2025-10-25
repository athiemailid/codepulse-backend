using Microsoft.AspNetCore.SignalR;
using CodePulseApi.DTOs;
using CodePulseApi.Hubs;
using CodePulseApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodePulseApi.Services;

/// <summary>
/// Service for managing real-time notifications via SignalR
/// Handles webhook events, repository updates, and system notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly CodePulseDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        CodePulseDbContext context,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Send a generic notification to all connected clients
    /// </summary>
    public async Task SendNotificationAsync(NotificationDto notification)
    {
        try
        {
            _logger.LogInformation("Sending notification: {Type} - {Title}", notification.Type, notification.Title);

            // Store notification in database for persistence
            await StoreNotificationAsync(notification);

            // Send to all connected clients
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification sent successfully: {Id}", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification: {Id}", notification.Id);
            throw;
        }
    }

    /// <summary>
    /// Send webhook-specific notification
    /// </summary>
    public async Task SendWebhookNotificationAsync(string webhookSource, string eventType, string repositoryId, string repositoryName, object data)
    {
        var notification = new WebhookNotificationDto
        {
            Type = NotificationType.WebhookReceived.ToString(),
            Title = $"Webhook Received from {webhookSource}",
            Message = $"New {eventType} event received for repository {repositoryName}",
            RepositoryId = repositoryId,
            RepositoryName = repositoryName,
            WebhookSource = webhookSource,
            EventType = eventType,
            Data = data,
            Severity = NotificationSeverity.Info,
            ActionUrl = $"/repositories/{repositoryId}",
            Metadata = new Dictionary<string, object>
            {
                { "webhookSource", webhookSource },
                { "eventType", eventType },
                { "repositoryId", repositoryId },
                { "processedAt", DateTime.UtcNow }
            }
        };

        // Send to repository subscribers and general notification group
        await SendNotificationToRepositorySubscribersAsync(repositoryId, notification);
        await _hubContext.Clients.Group("GeneralNotifications").SendAsync("ReceiveWebhookNotification", notification);

        _logger.LogInformation("Webhook notification sent for {EventType} in repository {RepositoryName}", eventType, repositoryName);
    }

    /// <summary>
    /// Send pull request-related notification
    /// </summary>
    public async Task SendPullRequestNotificationAsync(string repositoryId, string repositoryName, int pullRequestId, string title, string author, string status, string action)
    {
        var notificationType = action.ToLower() switch
        {
            "opened" => NotificationType.PullRequestCreated,
            "merged" => NotificationType.PullRequestMerged,
            "closed" => NotificationType.PullRequestClosed,
            _ => NotificationType.PullRequestCreated
        };

        var notification = new PullRequestNotificationDto
        {
            Type = notificationType.ToString(),
            Title = $"Pull Request {action}: {title}",
            Message = $"{author} {action} pull request #{pullRequestId} in {repositoryName}",
            RepositoryId = repositoryId,
            RepositoryName = repositoryName,
            PullRequestId = pullRequestId,
            PullRequestTitle = title,
            Author = author,
            Status = status,
            Severity = action.ToLower() == "merged" ? NotificationSeverity.Success : NotificationSeverity.Info,
            ActionUrl = $"/repositories/{repositoryId}/pull-requests/{pullRequestId}",
            Metadata = new Dictionary<string, object>
            {
                { "pullRequestId", pullRequestId },
                { "action", action },
                { "author", author },
                { "status", status }
            }
        };

        await SendNotificationToRepositorySubscribersAsync(repositoryId, notification);
        await _hubContext.Clients.Group($"WebhookEvent_pull_request").SendAsync("ReceivePullRequestNotification", notification);

        _logger.LogInformation("Pull request notification sent for PR #{PullRequestId} in repository {RepositoryName}", pullRequestId, repositoryName);
    }

    /// <summary>
    /// Send commit-related notification
    /// </summary>
    public async Task SendCommitNotificationAsync(string repositoryId, string repositoryName, string commitHash, string message, string author, string branch, int filesChanged)
    {
        var notification = new CommitNotificationDto
        {
            Type = NotificationType.CommitProcessed.ToString(),
            Title = $"New Commit: {message.Substring(0, Math.Min(50, message.Length))}...",
            Message = $"{author} committed to {branch} in {repositoryName}",
            RepositoryId = repositoryId,
            RepositoryName = repositoryName,
            CommitHash = commitHash,
            CommitMessage = message,
            Branch = branch,
            Author = author,
            FilesChanged = filesChanged,
            Severity = NotificationSeverity.Info,
            ActionUrl = $"/repositories/{repositoryId}/commits/{commitHash}",
            Metadata = new Dictionary<string, object>
            {
                { "commitHash", commitHash },
                { "branch", branch },
                { "author", author },
                { "filesChanged", filesChanged }
            }
        };

        await SendNotificationToRepositorySubscribersAsync(repositoryId, notification);
        await _hubContext.Clients.Group($"WebhookEvent_push").SendAsync("ReceiveCommitNotification", notification);

        _logger.LogInformation("Commit notification sent for {CommitHash} in repository {RepositoryName}", commitHash, repositoryName);
    }

    /// <summary>
    /// Send review-related notification
    /// </summary>
    public async Task SendReviewNotificationAsync(string repositoryId, string repositoryName, string reviewId, string reviewType, double score, string reviewer, string? pullRequestId = null, string? commitId = null)
    {
        var notification = new ReviewNotificationDto
        {
            Type = NotificationType.ReviewGenerated.ToString(),
            Title = $"Code Review Generated (Score: {score:F1})",
            Message = $"{reviewType} review completed by {reviewer} in {repositoryName}",
            RepositoryId = repositoryId,
            RepositoryName = repositoryName,
            ReviewId = reviewId,
            ReviewType = reviewType,
            Score = score,
            Reviewer = reviewer,
            PullRequestId = pullRequestId,
            CommitId = commitId,
            Severity = score >= 8.0 ? NotificationSeverity.Success : score >= 6.0 ? NotificationSeverity.Warning : NotificationSeverity.Error,
            ActionUrl = pullRequestId != null ? $"/repositories/{repositoryId}/pull-requests/{pullRequestId}/reviews/{reviewId}" 
                                              : $"/repositories/{repositoryId}/commits/{commitId}/reviews/{reviewId}",
            Metadata = new Dictionary<string, object>
            {
                { "reviewId", reviewId },
                { "reviewType", reviewType },
                { "score", score },
                { "reviewer", reviewer }
            }
        };

        await SendNotificationToRepositorySubscribersAsync(repositoryId, notification);

        _logger.LogInformation("Review notification sent for review {ReviewId} in repository {RepositoryName}", reviewId, repositoryName);
    }

    /// <summary>
    /// Send system-level notification
    /// </summary>
    public async Task SendSystemNotificationAsync(string title, string message, NotificationSeverity severity, string? userId = null, object? data = null)
    {
        var notification = new SystemNotificationDto
        {
            Type = NotificationType.SystemAlert.ToString(),
            Title = title,
            Message = message,
            Severity = severity,
            UserId = userId,
            Data = data,
            SystemComponent = "CodePulse System",
            Metadata = new Dictionary<string, object>
            {
                { "systemGenerated", true },
                { "severity", severity.ToString() }
            }
        };

        if (!string.IsNullOrEmpty(userId))
        {
            await SendNotificationToUserAsync(userId, notification);
        }
        else
        {
            await SendNotificationToAllUsersAsync(notification);
        }

        _logger.LogInformation("System notification sent: {Title} (Severity: {Severity})", title, severity);
    }

    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    public async Task SendNotificationToUserAsync(string userId, NotificationDto notification)
    {
        try
        {
            notification.UserId = userId;
            
            // Store notification in database
            await StoreNotificationAsync(notification);

            // Send to user's specific group
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Send notification to all subscribers of a repository
    /// </summary>
    public async Task SendNotificationToRepositorySubscribersAsync(string repositoryId, NotificationDto notification)
    {
        try
        {
            // Store notification in database
            await StoreNotificationAsync(notification);

            // Send to repository-specific group
            await _hubContext.Clients.Group($"Repository_{repositoryId}").SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification sent to repository {RepositoryId} subscribers: {Title}", repositoryId, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to repository {RepositoryId} subscribers", repositoryId);
            throw;
        }
    }

    /// <summary>
    /// Send notification to all connected users
    /// </summary>
    public async Task SendNotificationToAllUsersAsync(NotificationDto notification)
    {
        try
        {
            // Store notification in database
            await StoreNotificationAsync(notification);

            // Send to all connected clients
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification broadcast to all users: {Title}", notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast notification to all users");
            throw;
        }
    }

    /// <summary>
    /// Get user's notifications from database
    /// </summary>
    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int limit = 50, bool unreadOnly = false)
    {
        try
        {
            // For now, return empty list as we don't have notification entity in database
            // This can be implemented when you add notification storage to your database schema
            _logger.LogInformation("Getting notifications for user {UserId} (limit: {Limit}, unreadOnly: {UnreadOnly})", userId, limit, unreadOnly);
            return new List<NotificationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public async Task MarkNotificationAsReadAsync(string notificationId, string userId)
    {
        try
        {
            // This would update the notification in database when notification storage is implemented
            _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read for user {UserId}", notificationId, userId);
            throw;
        }
    }

    /// <summary>
    /// Get notification statistics for user
    /// </summary>
    public async Task<NotificationStatsDto> GetNotificationStatsAsync(string userId)
    {
        try
        {
            // Return basic stats - this would be enhanced when notification storage is implemented
            return new NotificationStatsDto
            {
                TotalNotifications = 0,
                UnreadNotifications = 0,
                TodayNotifications = 0,
                WeekNotifications = 0,
                NotificationsByType = new Dictionary<string, int>(),
                NotificationsBySeverity = new Dictionary<string, int>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Store notification in database for persistence (placeholder implementation)
    /// </summary>
    private async Task StoreNotificationAsync(NotificationDto notification)
    {
        try
        {
            // Create a simple log entry for now
            // You can enhance this by adding a Notifications table to your database schema
            _logger.LogInformation("Storing notification: {Id} - {Type} - {Title}", 
                notification.Id, notification.Type, notification.Title);

            // Placeholder for database storage
            // var notificationEntity = new NotificationEntity
            // {
            //     Id = notification.Id,
            //     Type = notification.Type,
            //     Title = notification.Title,
            //     Message = notification.Message,
            //     Data = JsonSerializer.Serialize(notification.Data),
            //     Severity = notification.Severity,
            //     Timestamp = notification.Timestamp,
            //     RepositoryId = notification.RepositoryId,
            //     UserId = notification.UserId,
            //     IsRead = notification.IsRead
            // };
            // 
            // _context.Notifications.Add(notificationEntity);
            // await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store notification {Id}", notification.Id);
            // Don't throw here to avoid breaking the notification flow
        }
    }
}
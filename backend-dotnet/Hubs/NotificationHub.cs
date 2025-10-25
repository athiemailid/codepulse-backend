using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CodePulseApi.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications to frontend clients
/// Handles webhook events, repository updates, and system notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var userEmail = GetUserEmail();
        var userName = GetUserName();
        
        // Validate that we have a valid authenticated user
        if (string.IsNullOrEmpty(userId) || userId == "anonymous")
        {
            _logger.LogWarning("Anonymous or invalid user attempted to connect to NotificationHub. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }
        
        _logger.LogInformation("Authenticated user connected to NotificationHub. UserId: {UserId}, Email: {Email}, Name: {Name}, ConnectionId: {ConnectionId}", 
            userId, userEmail, userName, Context.ConnectionId);

        try
        {
            // Join user-specific group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            
            // Join general notifications group
            await Groups.AddToGroupAsync(Context.ConnectionId, "GeneralNotifications");

            // Send welcome message with user info
            await Clients.Caller.SendAsync("ConnectionEstablished", new
            {
                userId = userId,
                userEmail = userEmail,
                userName = userName,
                connectionId = Context.ConnectionId,
                connectedAt = DateTime.UtcNow,
                message = "Successfully connected to notification hub"
            });

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user connection setup for UserId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var userEmail = GetUserEmail();
        
        _logger.LogInformation("Client disconnected from NotificationHub. UserId: {UserId}, Email: {Email}, ConnectionId: {ConnectionId}", 
            userId, userEmail, Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with exception. ConnectionId: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to repository-specific notifications
    /// </summary>
    /// <param name="repositoryId">Repository ID to subscribe to</param>
    public async Task SubscribeToRepository(string repositoryId)
    {
        var userId = GetUserId();
        
        if (!IsAuthenticated())
        {
            _logger.LogWarning("Unauthenticated user attempted to subscribe to repository. ConnectionId: {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", new { message = "Authentication required" });
            return;
        }
        
        if (string.IsNullOrEmpty(repositoryId))
        {
            _logger.LogWarning("Invalid repository ID provided for subscription. UserId: {UserId}", userId);
            await Clients.Caller.SendAsync("Error", new { message = "Invalid repository ID" });
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Repository_{repositoryId}");
        
        _logger.LogInformation("User {UserId} subscribed to repository {RepositoryId} notifications", userId, repositoryId);
        
        // Notify the client of successful subscription
        await Clients.Caller.SendAsync("SubscriptionConfirmed", new { repositoryId, message = "Successfully subscribed to repository notifications" });
    }

    /// <summary>
    /// Unsubscribe from repository-specific notifications
    /// </summary>
    /// <param name="repositoryId">Repository ID to unsubscribe from</param>
    public async Task UnsubscribeFromRepository(string repositoryId)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(repositoryId))
        {
            _logger.LogWarning("Invalid repository ID provided for unsubscription. UserId: {UserId}", userId);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Repository_{repositoryId}");
        
        _logger.LogInformation("User {UserId} unsubscribed from repository {RepositoryId} notifications", userId, repositoryId);
        
        // Notify the client of successful unsubscription
        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new { repositoryId, message = "Successfully unsubscribed from repository notifications" });
    }

    /// <summary>
    /// Subscribe to webhook event notifications
    /// </summary>
    /// <param name="eventTypes">Array of event types to subscribe to (e.g., "push", "pull_request", "issues")</param>
    public async Task SubscribeToWebhookEvents(string[] eventTypes)
    {
        var userId = GetUserId();
        
        if (!IsAuthenticated())
        {
            _logger.LogWarning("Unauthenticated user attempted to subscribe to webhook events. ConnectionId: {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", new { message = "Authentication required" });
            return;
        }
        
        if (eventTypes == null || eventTypes.Length == 0)
        {
            _logger.LogWarning("No event types provided for webhook subscription. UserId: {UserId}", userId);
            await Clients.Caller.SendAsync("Error", new { message = "No event types provided" });
            return;
        }

        foreach (var eventType in eventTypes)
        {
            if (!string.IsNullOrEmpty(eventType))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"WebhookEvent_{eventType}");
            }
        }
        
        _logger.LogInformation("User {UserId} subscribed to webhook events: {EventTypes}", userId, string.Join(", ", eventTypes));
        
        // Notify the client of successful subscription
        await Clients.Caller.SendAsync("WebhookSubscriptionConfirmed", new { eventTypes, message = "Successfully subscribed to webhook events" });
    }

    /// <summary>
    /// Get the current connection status and subscriptions
    /// </summary>
    public async Task GetConnectionStatus()
    {
        var userId = GetUserId();
        var userEmail = GetUserEmail();
        
        var status = new
        {
            connectionId = Context.ConnectionId,
            userId = userId,
            userEmail = userEmail,
            connectedAt = DateTime.UtcNow,
            isAuthenticated = Context.User?.Identity?.IsAuthenticated ?? false
        };
        
        await Clients.Caller.SendAsync("ConnectionStatus", status);
    }

    /// <summary>
    /// Handle heartbeat/ping from client to maintain connection
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", new { timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Extract user ID from claims
    /// </summary>
    private string GetUserId()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return "anonymous";

        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               Context.User?.FindFirst("sub")?.Value ?? 
               Context.User?.FindFirst("userId")?.Value ?? 
               Context.User?.FindFirst("id")?.Value ??
               "anonymous";
    }

    /// <summary>
    /// Extract user email from claims
    /// </summary>
    private string GetUserEmail()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return "unknown";

        return Context.User?.FindFirst(ClaimTypes.Email)?.Value ?? 
               Context.User?.FindFirst("email")?.Value ?? 
               "unknown";
    }

    /// <summary>
    /// Extract user name from claims
    /// </summary>
    private string GetUserName()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return "unknown";

        return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? 
               Context.User?.FindFirst("name")?.Value ?? 
               Context.User?.FindFirst("preferred_username")?.Value ??
               Context.User?.FindFirst("username")?.Value ??
               GetUserEmail(); // Fallback to email if name not available
    }

    /// <summary>
    /// Check if the current user is authenticated
    /// </summary>
    private bool IsAuthenticated()
    {
        return Context.User?.Identity?.IsAuthenticated == true;
    }
}
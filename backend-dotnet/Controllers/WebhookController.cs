using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CodePulseApi.DTOs;
using CodePulseApi.Services;

namespace CodePulseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService, 
        INotificationService notificationService,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> ProcessWebhook(
        [FromRoute] string provider,
        [FromBody] GitHubWebhookDto webhook,
        [FromHeader(Name = "X-Hub-Signature-256")] string? signature = null)
    {
        try
        {
            // Log the incoming request details
            _logger.LogInformation("Received webhook for provider: {Provider}", provider);
            _logger.LogInformation("Webhook payload: {@Webhook}", webhook);
            if (!string.IsNullOrEmpty(signature))
            {
                _logger.LogInformation("Received signature: {Signature}", signature);
            }

            // Send immediate notification about webhook receipt
            if (webhook.Repository != null)
            {
                await _notificationService.SendWebhookNotificationAsync(
                    provider,
                    webhook.Action ?? "unknown",
                    webhook.Repository.Id.ToString(),
                    webhook.Repository.FullName ?? "Unknown Repository",
                    new
                    {
                        webhook.Action,
                        webhook.Ref,
                        webhook.Repository.FullName,
                        Timestamp = DateTime.UtcNow
                    });
            }

            // Process the webhook
            var result = await _webhookService.ProcessWebhookAsync(webhook, provider, signature);

            if (result)
            {
                _logger.LogInformation("Webhook processed successfully for provider: {Provider}", provider);
                
                // Send success notification
                await _notificationService.SendSystemNotificationAsync(
                    "Webhook Processed Successfully",
                    $"Successfully processed {provider} webhook for repository {webhook.Repository?.FullName}",
                    NotificationSeverity.Success);

                return Ok(new { message = "Webhook processed successfully" });
            }

            _logger.LogWarning("Failed to process webhook for provider: {Provider}", provider);
            
            // Send failure notification
            await _notificationService.SendSystemNotificationAsync(
                "Webhook Processing Failed",
                $"Failed to process {provider} webhook for repository {webhook.Repository?.FullName}",
                NotificationSeverity.Error);

            return BadRequest(new { message = "Failed to process webhook" });
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "An error occurred while processing the webhook for provider: {Provider}", provider);
            
            // Send error notification
            await _notificationService.SendSystemNotificationAsync(
                "Webhook Processing Error",
                $"An error occurred while processing {provider} webhook: {ex.Message}",
                NotificationSeverity.Critical);

            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

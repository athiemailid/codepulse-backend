using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CodePulseApi.DTOs;
using CodePulseApi.Services;
using CodePulseApi.DTOs;

namespace CodePulseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IWebhookService webhookService, ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
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

            // Process the webhook
            var result = await _webhookService.ProcessWebhookAsync(webhook, provider, signature);

            if (result)
            {
                _logger.LogInformation("Webhook processed successfully for provider: {Provider}", provider);
                return Ok(new { message = "Webhook processed successfully" });
            }

            _logger.LogWarning("Failed to process webhook for provider: {Provider}", provider);
            return BadRequest(new { message = "Failed to process webhook" });
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "An error occurred while processing the webhook for provider: {Provider}", provider);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

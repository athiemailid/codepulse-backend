using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("azure-devops")]
    public async Task<IActionResult> HandleAzureDevOpsWebhook([FromBody] AzureDevOpsWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation("Received Azure DevOps webhook: {EventType}", webhook.EventType);
            
            var processed = await _webhookService.ProcessWebhookAsync(webhook);
            
            if (processed)
            {
                return Ok(new { message = "Webhook processed successfully" });
            }
            
            return BadRequest(new { message = "Failed to process webhook" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { message = "Internal server error processing webhook" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { 
            status = "OK", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}

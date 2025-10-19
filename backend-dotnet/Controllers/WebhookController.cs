using Microsoft.AspNetCore.Mvc;
using CodePulseApi.DTOs;
using CodePulseApi.Services;

namespace CodePulseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhookController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> ProcessWebhook([FromRoute] string provider, [FromBody] object webhook, [FromHeader(Name = "X-Hub-Signature-256")] string? signature = null)
    {
        var result = await _webhookService.ProcessWebhookAsync(webhook, provider, signature);
        if (result)
        {
            return Ok(new { message = "Webhook processed successfully" });
        }
        return BadRequest(new { message = "Failed to process webhook" });
    }
}

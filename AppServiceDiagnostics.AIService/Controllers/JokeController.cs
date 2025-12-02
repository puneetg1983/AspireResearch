using Aspire.Azure.ManagedIdentity.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppServiceDiagnostics.AIService;

namespace AppServiceDiagnostics.AIService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JokeController : ControllerBase
{
    private readonly JokeAgentService _jokeService;
    private readonly ILogger<JokeController> _logger;

    public JokeController(JokeAgentService jokeService, ILogger<JokeController> logger)
    {
        _jokeService = jokeService;
        _logger = logger;
    }

    [HttpGet]
    // Require managed identity authorization in Production, allow anonymous in Development
    [Authorize(Policy = ManagedIdentityAuthConstants.PolicyName)]
    public async Task<ActionResult<string>> GetJoke(CancellationToken cancellationToken = default)
    {
        try
        {
            var joke = await _jokeService.GetJokeAsync(cancellationToken);
            return Ok(new { joke });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get joke");
            return StatusCode(500, new { error = "Failed to get joke", message = ex.Message });
        }
    }

    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", service = "ai-service" });
    }
}
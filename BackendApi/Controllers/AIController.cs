using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIController> _logger;

    public AIController(IHttpClientFactory httpClientFactory, ILogger<AIController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("aiservice");
        _logger = logger;
    }

    [HttpGet("joke")]
    public async Task<ActionResult> GetJoke(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/joke", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return Content(content, "application/json");
            }
            else
            {
                _logger.LogWarning("AI service returned error: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, new { error = "AI service unavailable" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get joke from AI service");
            return StatusCode(500, new { error = "Failed to get joke", fallback = "Why don't programmers like nature? It has too many bugs!" });
        }
    }
}
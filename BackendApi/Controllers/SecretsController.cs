using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SecretsController : ControllerBase
    {
        private readonly SecretClient _secretClient;

        public SecretsController(SecretClient secretClient) 
        {
            _secretClient = secretClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSecrets()
        {
            try
            {
                List<string> secretNames = new List<string>();
                await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync())
                {
                    if (secretProperties.ContentType != null && secretProperties.ContentType.Contains("x-pkcs12"))
                    {
                        continue;
                    }
                    secretNames.Add(secretProperties.Name);
                }
                return Ok(secretNames);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving secrets: {ex.Message}");
            }
        }

        [HttpGet("{secretName}")]
        public async Task<IActionResult> Get(string secretName )
        {
            try
            {
                KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
                return Ok(secret.Value);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound($"Secret '{secretName}' not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving secret '{secretName}': {ex.Message}");
            }
        }
    }
}

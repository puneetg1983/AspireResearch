using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Azure.ManagedIdentity.Client;

/// <summary>
/// HTTP message handler that automatically adds managed identity authentication tokens to outgoing requests.
/// </summary>
public class ManagedIdentityAuthHandler : DelegatingHandler
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ManagedIdentityAuthHandler> _logger;
    private readonly TokenCredential? _credential;
    private readonly bool _isEnabled;
    private readonly string _scope;

    public ManagedIdentityAuthHandler(
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<ManagedIdentityAuthHandler> logger,
        string scope)
    {
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
        _scope = scope;

        // Only enable in Production when explicitly configured
        _isEnabled = _configuration.GetValue<bool>(ManagedIdentityAuthConstants.UseManagedIdentityAuthKey) && 
                     _environment.IsProduction();

        if (_isEnabled)
        {
            _credential = new DefaultAzureCredential();
            _logger.LogInformation("Managed identity authentication enabled for service-to-service calls");
        }
        else
        {
            _logger.LogInformation("Managed identity authentication disabled in {Environment} environment", 
                _environment.EnvironmentName);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_credential != null && _isEnabled)
        {
            try
            {
                var tokenResult = await _credential.GetTokenAsync(
                    new TokenRequestContext(new[] { _scope }),
                    cancellationToken);

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    tokenResult.Token);

                _logger.LogDebug("Added managed identity token to request to {Uri}", request.RequestUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire managed identity token for {Uri}", request.RequestUri);
                throw;
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

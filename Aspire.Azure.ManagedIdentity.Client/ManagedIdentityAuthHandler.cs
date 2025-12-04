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

        // Enable in Production automatically (Aspire sets AZURE_CLIENT_ID when identity is assigned)
        _isEnabled = _environment.IsProduction();

        if (_isEnabled)
        {
            // Check if Aspire has configured a specific user-assigned managed identity
            var clientId = _configuration["AZURE_CLIENT_ID"];

            if (string.IsNullOrEmpty(clientId))
            {
                throw new InvalidOperationException(
                    "Managed identity authentication is enabled, but no AZURE_CLIENT_ID is configured. " +
                    "Ensure that the Aspire-managed user-assigned managed identity is assigned to this service.");
            }

            // Use ManagedIdentityCredential with the specific client ID set by Aspire
            _credential = new ManagedIdentityCredential(clientId);
            _logger.LogInformation("Managed identity authentication enabled with client ID: {ClientId}", clientId);
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
                _logger.LogInformation("Acquiring managed identity token for scope {scope} to {Uri}", _scope, request.RequestUri);
                var tokenResult = await _credential.GetTokenAsync(
                    new TokenRequestContext([_scope]),
                    cancellationToken);                

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    tokenResult.Token);

                _logger.LogInformation("Added managed identity token {token} to request to {Uri}", tokenResult.Token[..100], request.RequestUri);
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

using Aspire.Azure.ManagedIdentity.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for adding managed identity authentication to HTTP clients.
/// </summary>
public static class ManagedIdentityHttpClientExtensions
{
    /// <summary>
    /// Configures the HTTP client to use managed identity authentication for outbound requests.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="scope">The OAuth2 scope for token requests (e.g., "https://management.azure.com/.default").</param>
    public static IHttpClientBuilder AddManagedIdentityAuth(this IHttpClientBuilder builder, string scope)
    {
        builder.Services.AddTransient(sp => new ManagedIdentityAuthHandler(
            sp.GetRequiredService<IHostEnvironment>(),
            sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ManagedIdentityAuthHandler>>(),
            scope));
        builder.AddHttpMessageHandler<ManagedIdentityAuthHandler>();
        return builder;
    }
}

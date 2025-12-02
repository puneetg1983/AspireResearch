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
    public static IHttpClientBuilder AddManagedIdentityAuth(this IHttpClientBuilder builder)
    {
        builder.Services.AddTransient<ManagedIdentityAuthHandler>();
        builder.AddHttpMessageHandler<ManagedIdentityAuthHandler>();
        return builder;
    }
}

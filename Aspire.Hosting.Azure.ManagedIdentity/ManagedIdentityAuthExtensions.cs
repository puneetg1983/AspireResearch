using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for configuring service-to-service authentication using managed identities.
/// </summary>
public static class ManagedIdentityAuthExtensions
{
    /// <summary>
    /// Provisions a user-assigned managed identity for this resource and configures it for authentication.
    /// The resource will use this identity to authenticate to other services.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="identityName">Optional name for the managed identity. If not specified, uses the resource name with '-identity' suffix.</param>
    /// <returns>A reference holder for the user-assigned managed identity that can be passed to AllowManagedIdentities().</returns>
    public static ManagedIdentityReference WithUserAssignedManagedIdentity<T>(
        this IResourceBuilder<T> builder,
        string? identityName = null)
        where T : IResourceWithEnvironment
    {
        var name = identityName ?? $"{builder.Resource.Name}-identity";
        var identity = new UserAssignedManagedIdentityResource(name, builder.Resource);
        
        // Add annotation to the original resource
        builder.WithAnnotation(new ManagedIdentityAuthenticationAnnotation(identity));
        
        // Configure environment to indicate managed identity should be used
        builder.WithEnvironment(ManagedIdentityEnvironmentVariables.UseManagedIdentityAuth, "true");
        
        // Return a reference without adding to the resource model
        // This prevents the identity from appearing in the dashboard
        return new ManagedIdentityReference(identity, builder.ApplicationBuilder);
    }

    /// <summary>
    /// Configures this service to accept authentication from the specified managed identities.
    /// In Production, only requests with tokens from these identities will be allowed.
    /// In Development, all requests are allowed.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="allowedIdentities">The managed identities that are allowed to access this service.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> AllowManagedIdentities<T>(
        this IResourceBuilder<T> builder,
        params ManagedIdentityReference[] allowedIdentities)
        where T : IResourceWithEnvironment
    {
        var identityResources = allowedIdentities.Select(r => r.Identity).ToList();
        builder.WithAnnotation(new AllowedIdentitiesAnnotation(identityResources));
        
        // Configure environment variables for allowed client IDs (will be populated during provisioning)
        if (identityResources.Count > 0)
        {
            // Generate comma-separated list of identity references
            var identityRefs = string.Join(",", identityResources.Select(i => $"{{{i.Name}.clientId}}"));
            builder.WithEnvironment(ManagedIdentityEnvironmentVariables.AllowedClientIds, identityRefs);
            builder.WithEnvironment(ManagedIdentityEnvironmentVariables.RequireManagedIdentityAuth, "true");
        }
        
        return builder;
    }

    /// <summary>
    /// Configures the HTTP client for the specified service reference to use managed identity authentication.
    /// This is automatically applied when WithUserAssignedManagedIdentity is used.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="targetService">The target service that requires authentication.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithManagedIdentityAuth<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResourceWithEndpoints> targetService)
        where T : IResourceWithEnvironment
    {
        builder.WithAnnotation(new RequiresManagedIdentityTokenAnnotation(targetService.Resource.Name));
        
        // Configure environment to enable token acquisition for this specific service
        builder.WithEnvironment($"AUTH__{targetService.Resource.Name.ToUpperInvariant()}__ENABLED", "true");
        
        return builder;
    }
}

/// <summary>
/// Reference to a managed identity that doesn't add it to the resource model.
/// This prevents the identity from appearing in the Aspire Dashboard.
/// </summary>
public sealed class ManagedIdentityReference
{
    internal ManagedIdentityReference(IUserAssignedManagedIdentityResource identity, IDistributedApplicationBuilder applicationBuilder)
    {
        Identity = identity;
        ApplicationBuilder = applicationBuilder;
    }

    internal IUserAssignedManagedIdentityResource Identity { get; }
    internal IDistributedApplicationBuilder ApplicationBuilder { get; }
}

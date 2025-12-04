using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for configuring service-to-service authentication using managed identities.
/// </summary>
public static class ManagedIdentityAuthExtensions
{
    /// <summary>
    /// Configures this service to accept authentication from Aspire's built-in Azure user-assigned managed identity resources.
    /// In Production, only requests with tokens from these identities will be allowed.
    /// In Development, all requests are allowed.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="allowedIdentities">The Azure managed identity resource builders that are allowed to access this service.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> AllowManagedIdentities<T>(
        this IResourceBuilder<T> builder,
        params IResourceBuilder<AzureUserAssignedIdentityResource>[] allowedIdentities)
        where T : IResourceWithEnvironment
    {
        // Configure environment variables for allowed principal IDs (Object IDs) - will be populated during provisioning
        if (allowedIdentities.Length > 0)
        {
            // Use BicepOutputReference to properly reference the principalId outputs from identity resources
            // This creates proper bicep parameter references instead of literal strings
            if (allowedIdentities.Length == 1)
            {
                // Single identity - directly use the PrincipalId reference (Object ID)
                // Use the Azure-specific extension to ensure proper bicep parameter generation
                AzureBicepResourceExtensions.WithEnvironment(builder, ManagedIdentityEnvironmentVariables.AllowedPrincipalIds, allowedIdentities[0].Resource.PrincipalId);
            }
            else
            {
                // Multiple identities - use ReferenceExpression to join them with commas
                // Build an interpolated string expression that combines all principal IDs (Object IDs)
                var principalIds = allowedIdentities.Select(i => i.Resource.PrincipalId).ToArray();
                
                // Create a ReferenceExpression that joins the principal IDs with commas
                // Using interpolated string: "{principalId0},{principalId1},{principalId2}..."
                ReferenceExpression joinedExpression = principalIds.Length switch
                {
                    2 => ReferenceExpression.Create($"{principalIds[0]},{principalIds[1]}"),
                    3 => ReferenceExpression.Create($"{principalIds[0]},{principalIds[1]},{principalIds[2]}"),
                    4 => ReferenceExpression.Create($"{principalIds[0]},{principalIds[1]},{principalIds[2]},{principalIds[3]}"),
                    _ => throw new NotSupportedException($"AllowManagedIdentities supports up to 4 identities, but {principalIds.Length} were provided.")
                };
                
                // Use regular WithEnvironment for ReferenceExpression (it has its own specific overload)
                builder.WithEnvironment(ManagedIdentityEnvironmentVariables.AllowedPrincipalIds, joinedExpression);
            }
        }
        
        return builder;
    }

}

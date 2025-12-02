using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation that marks a resource as requiring user-assigned managed identity authentication.
/// </summary>
public class ManagedIdentityAuthenticationAnnotation : IResourceAnnotation
{
    public ManagedIdentityAuthenticationAnnotation(IUserAssignedManagedIdentityResource identity)
    {
        Identity = identity;
    }

    /// <summary>
    /// Gets the user-assigned managed identity for this resource.
    /// </summary>
    public IUserAssignedManagedIdentityResource Identity { get; }
}

/// <summary>
/// Annotation that specifies which managed identities are allowed to access a service.
/// </summary>
public class AllowedIdentitiesAnnotation : IResourceAnnotation
{
    public AllowedIdentitiesAnnotation(IReadOnlyList<IUserAssignedManagedIdentityResource> allowedIdentities)
    {
        AllowedIdentities = allowedIdentities;
    }

    /// <summary>
    /// Gets the list of managed identities allowed to access this resource.
    /// </summary>
    public IReadOnlyList<IUserAssignedManagedIdentityResource> AllowedIdentities { get; }
}

/// <summary>
/// Annotation that indicates a service requires managed identity token authentication for outbound calls.
/// </summary>
public class RequiresManagedIdentityTokenAnnotation : IResourceAnnotation
{
    public RequiresManagedIdentityTokenAnnotation(string targetServiceName)
    {
        TargetServiceName = targetServiceName;
    }

    /// <summary>
    /// Gets the name of the target service requiring authentication.
    /// </summary>
    public string TargetServiceName { get; }
}

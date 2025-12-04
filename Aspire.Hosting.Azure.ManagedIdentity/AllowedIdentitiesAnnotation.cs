namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation that specifies which managed identities are allowed to access a service.
/// </summary>
public class AllowedIdentitiesAnnotation(IReadOnlyList<IUserAssignedManagedIdentityResource> allowedIdentities) : IResourceAnnotation
{

    /// <summary>
    /// Gets the list of managed identities allowed to access this resource.
    /// </summary>
    public IReadOnlyList<IUserAssignedManagedIdentityResource> AllowedIdentities { get; } = allowedIdentities;
}
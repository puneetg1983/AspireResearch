using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a user-assigned managed identity provisioned in Azure.
/// This is an infrastructure-only resource that should not be displayed in the dashboard.
/// </summary>
public interface IUserAssignedManagedIdentityResource : IResourceWithEnvironment
{
    /// <summary>
    /// Gets the name of the user-assigned managed identity.
    /// </summary>
    new string Name { get; }
    
    /// <summary>
    /// Gets the resource that owns this managed identity.
    /// </summary>
    IResource Owner { get; }
}

/// <summary>
/// Implementation of a user-assigned managed identity resource.
/// </summary>
internal class UserAssignedManagedIdentityResource : Resource, IUserAssignedManagedIdentityResource
{
    public UserAssignedManagedIdentityResource(string name, IResource owner) : base(name)
    {
        Owner = owner;
    }

    public IResource Owner { get; }
}

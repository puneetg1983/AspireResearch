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
}
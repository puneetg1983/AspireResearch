namespace Aspire.Hosting.Azure;

/// <summary>
/// Environment variable names for managed identity authentication.
/// These constants are shared between the hosting and client libraries.
/// </summary>
internal static class ManagedIdentityEnvironmentVariables
{
    /// <summary>
    /// Environment variable name for the managed identity client ID.
    /// </summary>
    public const string ManagedIdentityClientId = "ASPIRE_EXTENSIONS_MANAGED_IDENTITY_CLIENT_ID";
    
    /// <summary>
    /// Environment variable name for the comma-separated list of allowed client IDs.
    /// </summary>
    public const string AllowedClientIds = "ASPIRE_EXTENSIONS_ALLOWED_CLIENT_IDS";
    
    /// <summary>
    /// Environment variable name for the Azure tenant ID.
    /// </summary>
    public const string AzureTenantId = "ASPIRE_EXTENSIONS_AZURE_TENANT_ID";
    
    /// <summary>
    /// Configuration key to enable/disable managed identity authentication.
    /// </summary>
    public const string UseManagedIdentityAuth = "ASPIRE_EXTENSIONS_USE_MANAGED_IDENTITY_AUTH";
    
    /// <summary>
    /// Configuration key to require managed identity authentication validation.
    /// </summary>
    public const string RequireManagedIdentityAuth = "ASPIRE_EXTENSIONS_REQUIRE_MANAGED_IDENTITY_AUTH";
}

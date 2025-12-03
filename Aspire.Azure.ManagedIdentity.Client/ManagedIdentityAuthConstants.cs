namespace Aspire.Azure.ManagedIdentity.Client;

/// <summary>
/// Constants for managed identity authentication.
/// </summary>
public static class ManagedIdentityAuthConstants
{
    /// <summary>
    /// The authorization policy name that requires managed identity authentication.
    /// Use this with [Authorize(Policy = ManagedIdentityAuthConstants.PolicyName)] on controllers/endpoints.
    /// </summary>
    public const string PolicyName = "RequireManagedIdentity";
    
    /// <summary>
    /// Environment variable name for the managed identity client ID.
    /// </summary>
    public const string ManagedIdentityClientIdKey = "ASPIRE_EXTENSIONS_MANAGED_IDENTITY_CLIENT_ID";
    
    /// <summary>
    /// Environment variable name for the comma-separated list of allowed client IDs.
    /// </summary>
    public const string AllowedClientIdsKey = "ASPIRE_EXTENSIONS_ALLOWED_CLIENT_IDS";
    
    /// <summary>
    /// Environment variable name for the Azure tenant ID.
    /// </summary>
    public const string AzureTenantIdKey = "ASPIRE_EXTENSIONS_AZURE_TENANT_ID";
    
    /// <summary>
    /// Configuration key to enable/disable managed identity authentication.
    /// </summary>
    public const string UseManagedIdentityAuthKey = "ASPIRE_EXTENSIONS_USE_MANAGED_IDENTITY_AUTH";
    
    /// <summary>
    /// Configuration key to require managed identity authentication validation.
    /// </summary>
    public const string RequireManagedIdentityAuthKey = "ASPIRE_EXTENSIONS_REQUIRE_MANAGED_IDENTITY_AUTH";
}

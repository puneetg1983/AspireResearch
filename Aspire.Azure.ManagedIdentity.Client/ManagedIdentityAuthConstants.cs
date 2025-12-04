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
    /// Environment variable name for the comma-separated list of allowed principal IDs (Object IDs).
    /// </summary>
    public const string AllowedPrincipalIdsKey = "ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS";   
}

namespace Aspire.Hosting.Azure;

/// <summary>
/// Environment variable names for managed identity authentication.
/// These constants are shared between the hosting and client libraries.
/// </summary>
internal static class ManagedIdentityEnvironmentVariables
{    
    /// <summary>
    /// Environment variable name for the comma-separated list of allowed principal IDs (Object IDs).
    /// </summary>
    public const string AllowedPrincipalIds = "ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS";
}

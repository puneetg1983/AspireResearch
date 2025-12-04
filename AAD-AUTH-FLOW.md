# Azure AD Authentication Flow: Service-to-Service with Managed Identities

## Overview
BackendApi uses its managed identity to call AIService, authenticated via an Azure AD App Registration.

## The Flow

### 1. Token Request (BackendApi → Azure AD)
```csharp
// BackendApi requests a token
var token = await managedIdentityCredential.GetTokenAsync(
    new TokenRequestContext(["api://1d922779-2742-4cf2-8c82-425cf2c60aa8/.default"])
);
```

**What happens:**
- BackendApi's managed identity requests a token for resource `api://1d922779-2742-4cf2-8c82-425cf2c60aa8`
- The `/.default` scope means "grant all default permissions for this resource"
- Request goes to: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`

### 2. Token Issuance (Azure AD → BackendApi)
Azure AD validates and returns a JWT token with these claims:

```json
{
  "iss": "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0",
  "aud": "1d922779-2742-4cf2-8c82-425cf2c60aa8",
  "appid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tid": "72f988bf-86f1-41af-91ab-2d7cd011db47",
  "exp": 1733270400
}
```

**Key Point:** Even though the request used `api://GUID/.default`, the token's audience (`aud`) is just the GUID. This is standard Azure AD behavior.

### 3. API Call (BackendApi → AIService)
```http
GET https://aiservice/api/joke HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### 4. Token Validation (AIService)
AIService's JWT Bearer middleware validates the token through multiple checks:

#### a) **Signature Validation**
```csharp
options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
options.TokenValidationParameters.ValidateIssuerSigningKey = true;
```
- Downloads public keys from `{Authority}/.well-known/openid-configuration`
- Verifies token signature using Azure AD's public key
- Ensures token wasn't tampered with

#### b) **Issuer Validation**
```csharp
options.TokenValidationParameters.ValidIssuers = new[]
{
    "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0"
};
```
- Checks `iss` claim matches expected tenant
- Managed identities use v2.0 format (`login.microsoftonline.com/.../v2.0`)
- Ensures token came from the correct Azure AD tenant

#### c) **Audience Validation**
```csharp
options.Audience = "1d922779-2742-4cf2-8c82-425cf2c60aa8";
options.TokenValidationParameters.ValidateAudience = true;
```
- Checks `aud` claim equals `"1d922779-2742-4cf2-8c82-425cf2c60aa8"`
- Ensures token was issued specifically for this service (not another resource)

#### d) **Lifetime Validation**
```csharp
options.TokenValidationParameters.ValidateLifetime = true;
```
- Checks `exp` (expiration) claim
- Ensures token hasn't expired

#### e) **Principal Identity Validation (Custom)**
```csharp
// Extract Object ID (OID) from token claims
var principalId = context.Principal?.Claims.FirstOrDefault(c => 
    c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier" || // Managed identity tokens
    c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" || // Alternative
    c.Type == "oid" || // User tokens
    c.Type == "sub"    // Subject claim
)?.Value;

var allowedPrincipalIds = config["ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS"]?.Split(',');

if (!allowedPrincipalIds.Contains(principalId))
{
    context.Fail("Unauthorized principal identity");
}
```
- Checks Object ID (OID) claim from the JWT token
- **Important**: Managed identity tokens use full URI claim types, not short names
- Ensures only explicitly allowed service principals can call this API
- Uses Object ID instead of Application ID for proper authorization alignment with Azure RBAC

## Key Concepts

### Application ID URI vs Client ID
- **Application ID URI**: `api://1d922779-2742-4cf2-8c82-425cf2c60aa8` - Human-readable identifier used in requests
- **Client ID**: `1d922779-2742-4cf2-8c82-425cf2c60aa8` - GUID used in tokens as audience
- Azure AD automatically maps the URI to the GUID

### Why v2.0 Endpoint?
Managed identities issue tokens using the **v2.0 endpoint** (Microsoft Identity Platform):
- **Issuer**: `https://login.microsoftonline.com/{tenant}/v2.0`
- Must configure authority with `/v2.0` suffix
- v2.0 is the standard for all managed identity tokens

### Defense in Depth
Multiple validation layers ensure security:
1. **Signature**: Token is cryptographically valid
2. **Issuer**: Token came from correct Azure AD tenant
3. **Audience**: Token is for this specific service
4. **Lifetime**: Token is still valid (not expired)
5. **Client Identity**: Caller is explicitly allowed

If any validation fails, the request is rejected with a 401 Unauthorized response.

## Configuration

### Azure AD App Registration
- **Client ID**: `1d922779-2742-4cf2-8c82-425cf2c60aa8`
- **Application ID URI**: `api://1d922779-2742-4cf2-8c82-425cf2c60aa8`
- **Tenant ID**: `72f988bf-86f1-41af-91ab-2d7cd011db47`

### BackendApi Configuration
```csharp
builder.Services.AddHttpClient("aiservice", client =>
{
    client.BaseAddress = new Uri("https+http://aiservice");
})
.AddManagedIdentityAuth("api://1d922779-2742-4cf2-8c82-425cf2c60aa8/.default");
```

### AIService Configuration
```csharp
builder.Services.AddManagedIdentityAuthentication(
    builder.Configuration,
    builder.Environment,
    "1d922779-2742-4cf2-8c82-425cf2c60aa8",  // AAD app client ID
    "72f988bf-86f1-41af-91ab-2d7cd011db47"); // Tenant ID
```

### Environment Variables
- **ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS**: Comma-separated list of allowed caller Object IDs (OIDs) - BackendApi's managed identity principal ID
- **AZURE_CLIENT_ID**: Set automatically by Aspire for each service's managed identity
- **AZURE_TENANT_ID**: Azure AD tenant ID (optional, can be hardcoded)

## Aspire Integration

### How Aspire Provisions Managed Identities and Environment Variables

Aspire automates the provisioning of managed identities and configuration through bicep templates. Here's how the integration works:

#### 1. AppHost Configuration
```csharp
// In Program.cs (AppHost)
var backendApiIdentity = builder.AddAzureUserAssignedIdentity("backendapi-identity");
var aiServiceIdentity = builder.AddAzureUserAssignedIdentity("aiservice-identity");

var aiservice = builder.AddProject<Projects.AIService>("aiservice")
    .WithExternalHttpEndpoints()
    .WithIdentity(aiServiceIdentity)
    .AllowManagedIdentities(backendApiIdentity); // ← Configures allowed principals

var backendApi = builder.AddProject<Projects.BackendApi>("backendapi")
    .WithIdentity(backendApiIdentity);
```

**What happens:**
- `AddAzureUserAssignedIdentity()` creates managed identity resources in Azure
- `WithIdentity()` assigns the identity to the service
- `AllowManagedIdentities()` configures which identities can call this service

#### 2. Bicep Parameter Generation
When you call `AllowManagedIdentities(backendApiIdentity)`, Aspire:

1. **Extracts the PrincipalId** from the identity resource
2. **Creates a BicepOutputReference** to reference the identity's `principalId` output
3. **Generates bicep parameters** that pass the OID to the container app

```bicep
// backendapi-identity.module.bicep (generated)
resource backendapi_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'backendapi-identity'
  location: location
}

output clientId string = backendapi_identity.properties.clientId
output principalId string = backendapi_identity.properties.principalId  // ← OID exposed
output id string = backendapi_identity.id
```

```bicep
// aiservice-containerapp.module.bicep (generated)
param backendapi_identity_outputs_principalid string  // ← Parameter created

resource aiservice 'Microsoft.App/containerApps@2024-03-01' = {
  // ...
  properties: {
    template: {
      containers: [{
        env: [
          {
            name: 'ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS'
            value: backendapi_identity_outputs_principalid  // ← OID injected
          }
        ]
      }]
    }
  }
}
```

#### 3. The Extension Method Implementation
```csharp
// ManagedIdentityAuthExtensions.cs
public static IResourceBuilder<T> AllowManagedIdentities<T>(
    this IResourceBuilder<T> builder,
    params IResourceBuilder<AzureUserAssignedIdentityResource>[] allowedIdentities)
{
    if (allowedIdentities.Length == 1)
    {
        // Single identity - use BicepOutputReference for principalId
        AzureBicepResourceExtensions.WithEnvironment(
            builder, 
            "ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS", 
            allowedIdentities[0].Resource.PrincipalId);  // ← References .principalId
    }
    else
    {
        // Multiple identities - join with ReferenceExpression
        var principalIds = allowedIdentities.Select(i => i.Resource.PrincipalId).ToArray();
        
        ReferenceExpression joinedExpression = principalIds.Length switch
        {
            2 => ReferenceExpression.Create($"{principalIds[0]},{principalIds[1]}"),
            3 => ReferenceExpression.Create($"{principalIds[0]},{principalIds[1]},{principalIds[2]}"),
            // ... up to 4 identities
        };
        
        builder.WithEnvironment("ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS", joinedExpression);
    }
}
```

**Key Points:**
- Uses `PrincipalId` property (Object ID) instead of `ClientId` (Application ID)
- `ReferenceExpression.Create()` is required for multiple identities to preserve bicep references
- Cannot use `string.Join()` as it would evaluate immediately, losing bicep reference information

#### 4. Why PrincipalId (OID) vs ClientId?
| Property | Type | Purpose | Use Case |
|----------|------|---------|----------|
| **ClientId** | Application ID | Identifies the application registration | Client credentials, `AZURE_CLIENT_ID` env var |
| **PrincipalId** | Object ID (OID) | Identifies the security principal | Authorization, RBAC role assignments |

**For authentication/authorization**: Use `PrincipalId` (OID) because:
- Azure RBAC role assignments use Object ID
- More semantically correct for "who is allowed to call this service?"
- Aligns with Azure AD authorization patterns
- JWT tokens contain OID in claims for managed identities

#### 5. Token Claim Types
**Important**: Managed identity tokens use full URI claim types, not short names:

```csharp
// What DOESN'T work:
var oid = claims.FirstOrDefault(c => c.Type == "oid")?.Value;  // ❌ Returns null

// What WORKS:
var oid = claims.FirstOrDefault(c => 
    c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier"  // ✓
)?.Value;
```

**Actual token claims** (from managed identity):
```
http://schemas.microsoft.com/identity/claims/objectidentifier=5e9ccc1b-12c0-460f-be42-585ac084ba52
http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier=5e9ccc1b-12c0-460f-be42-585ac084ba52
azp=df0905f5-25b7-4e65-8255-631afedab625
aud=1d922779-2742-4cf2-8c82-425cf2c60aa8
```

#### 6. Runtime Flow
1. **Build time**: Aspire generates bicep with parameter references
2. **Deployment**: Bicep creates managed identities and gets their OIDs
3. **Runtime**: Container apps receive `ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS` with actual OID values
4. **Request**: Caller gets token with OID in claims
5. **Validation**: AIService extracts OID from token and validates against allowed list

## Troubleshooting

### Common Issues

#### Audience Mismatch
```
IDX10214: Audience validation failed. Audiences: '1d922779-2742-4cf2-8c82-425cf2c60aa8'. 
Did not match: validationParameters.ValidAudience: 'api://1d922779-2742-4cf2-8c82-425cf2c60aa8'
```
**Solution**: Use just the GUID (not `api://` prefix) in audience validation

#### Issuer Mismatch
```
IDX10205: Issuer validation failed. Issuer: 'https://login.microsoftonline.com/{tenant}/v2.0'.
```
**Solution**: Managed identities use v2.0 tokens. Configure authority with `/v2.0` suffix:
```csharp
options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
options.TokenValidationParameters.ValidIssuers = new[] 
{ 
    "https://login.microsoftonline.com/{tenant}/v2.0" 
};
```

#### Unauthorized Principal
```
Rejected unauthorized principal: {PrincipalId}
```
**Solution**: Add the calling service's managed identity Object ID (OID) to `ASPIRE_EXTENSIONS_ALLOWED_PRINCIPAL_IDS`

#### Principal ID is (null)
```
Rejected unauthorized principal: (null)
```
**Solution**: Token claims use full URI format. Ensure code checks for:
- `http://schemas.microsoft.com/identity/claims/objectidentifier` (most common)
- `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` (alternative)
- Not just the short name `oid`

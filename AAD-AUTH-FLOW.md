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
    "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0",
    "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/"
};
```
- Checks `iss` claim matches expected tenant
- Accepts both v2.0 (`login.microsoftonline.com`) and v1.0 (`sts.windows.net`) formats
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

#### e) **Client Identity Validation (Custom)**
```csharp
var clientId = context.Principal?.Claims.FirstOrDefault(c => c.Type == "appid")?.Value;
var allowedClientIds = config["ASPIRE_EXTENSIONS_ALLOWED_CLIENT_IDS"]?.Split(',');

if (!allowedClientIds.Contains(clientId))
{
    context.Fail("Unauthorized client identity");
}
```
- Checks `appid` claim (BackendApi's managed identity client ID)
- Ensures only explicitly allowed services can call this API
- Provides fine-grained access control

## Key Concepts

### Application ID URI vs Client ID
- **Application ID URI**: `api://1d922779-2742-4cf2-8c82-425cf2c60aa8` - Human-readable identifier used in requests
- **Client ID**: `1d922779-2742-4cf2-8c82-425cf2c60aa8` - GUID used in tokens as audience
- Azure AD automatically maps the URI to the GUID

### Why v2.0 Endpoint?
Managed identities issue tokens using the v2.0 endpoint format:
- **Issuer**: `https://login.microsoftonline.com/{tenant}/v2.0`
- Must configure authority with `/v2.0` suffix
- Also accept v1.0 format (`https://sts.windows.net/{tenant}/`) for compatibility

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
- **ASPIRE_EXTENSIONS_ALLOWED_CLIENT_IDS**: Comma-separated list of allowed caller client IDs (BackendApi's managed identity)
- **AZURE_CLIENT_ID**: Set automatically by Aspire for each service's managed identity
- **AZURE_TENANT_ID**: Azure AD tenant ID (optional, can be hardcoded)

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
Did not match: 'https://sts.windows.net/{tenant}/'
```
**Solution**: Configure authority with `/v2.0` suffix and accept both v1.0 and v2.0 issuers

#### Unauthorized Client
```
Rejected unauthorized client: {ClientId}
```
**Solution**: Add the calling service's managed identity client ID to `ASPIRE_EXTENSIONS_ALLOWED_CLIENT_IDS`

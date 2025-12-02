# Aspire Managed Identity Authentication Extensions

Production-ready .NET Aspire extensions for streamlined service-to-service authentication using Azure User-Assigned Managed Identities.

## 🎯 Overview

This library provides **zero-configuration** managed identity authentication between services deployed to Azure Container Apps via Aspire, while maintaining seamless local development without Azure dependencies.

## 📦 Packages

- **Aspire.Hosting.Azure.ManagedIdentity** - AppHost extensions for declaring authentication requirements
- **Aspire.Azure.ManagedIdentity.Client** - Runtime libraries for authentication implementation

## ✨ Features

- ✅ **Declarative API** - Configure authentication in AppHost with fluent syntax
- ✅ **Automatic Provisioning** - User-assigned managed identities created during deployment
- ✅ **Environment-Aware** - Production requires auth, Development allows all
- ✅ **Type-Safe** - Strongly-typed identity references prevent configuration errors
- ✅ **Zero Secrets** - No API keys, connection strings, or credentials
- ✅ **Audit Logging** - Full diagnostic logging for compliance

## 🚀 Quick Start

### 1. Add Package References

**AppHost Project:**
```xml
<ItemGroup>
  <ProjectReference Include="..\Aspire.Hosting.Azure.ManagedIdentity\Aspire.Hosting.Azure.ManagedIdentity.csproj" />
</ItemGroup>
```

**Service Projects (Caller & Callee):**
```xml
<ItemGroup>
  <ProjectReference Include="..\Aspire.Azure.ManagedIdentity.Client\Aspire.Azure.ManagedIdentity.Client.csproj" />
</ItemGroup>
```

### 2. Configure AppHost (Program.cs)

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Service that will make authenticated calls
var backendApi = builder.AddProject<Projects.BackendApi>("backendapi");

// Provision user-assigned managed identity for backendApi
var backendIdentity = backendApi.WithUserAssignedManagedIdentity();

// Service that requires authentication
// Automatically provisions: ALLOWED_CLIENT_IDS, AZURE_TENANT_ID
var aiService = builder.AddProject<Projects.AIService>("aiservice")
    .AllowManagedIdentities(backendIdentity); // Only allow backendApi

builder.Build().Run();
```

**That's it for AppHost configuration!** 🎉

### 3. Configure Caller Service (BackendApi)

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add HTTP client with automatic managed identity auth
builder.Services.AddHttpClient("aiservice", client =>
{
    client.BaseAddress = new Uri("https+http://aiservice");
})
.AddManagedIdentityAuth(); // ✨ Extension method adds authentication

var app = builder.Build();
app.Run();
```

### 4. Configure Callee Service (AIService)

**Program.cs:**
```csharp
using Aspire.Azure.ManagedIdentity.Client;

var builder = WebApplication.CreateBuilder(args);

// Add authentication services
builder.Services.AddManagedIdentityAuthentication(
    builder.Configuration, 
    builder.Environment);

builder.Services.AddControllers();

var app = builder.Build();

// Enable authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

**Controller:**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class JokeController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RequireManagedIdentity")] // ✨ Enforced in Production only
    public async Task<IActionResult> GetJoke()
    {
        return Ok(new { joke = "Why did the developer quit? No arrays!" });
    }
}
```

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        AppHost                              │
│  ┌──────────────┐                  ┌──────────────┐        │
│  │  BackendApi  │                  │  AIService   │        │
│  │              │                  │              │        │
│  │ .WithUser... │─────────────────>│ .AllowManaged│        │
│  │ Managed...   │  Identity Ref    │ Identities() │        │
│  └──────────────┘                  └──────────────┘        │
└─────────────────────────────────────────────────────────────┘
                      │                        │
                      ▼ Provisions             ▼ Provisions
┌─────────────────────────────────────────────────────────────┐
│                    Azure (Production)                        │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  User-Assigned Managed Identity: backendapi-identity   │ │
│  │  Client ID: a1b2c3d4-...                               │ │
│  └────────────────────────────────────────────────────────┘ │
│         │                                    │               │
│         ▼ Assigned to                       ▼ Allowed by    │
│  ┌──────────────┐   Bearer Token   ┌──────────────┐        │
│  │ Container App│ ─────────────────>│ Container App│        │
│  │  BackendApi  │   JWT (appid=...)│  AIService   │        │
│  │              │                   │ Validates    │        │
│  └──────────────┘                   └──────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## 📋 API Reference

### AppHost Extensions

#### `WithUserAssignedManagedIdentity()`
Provisions a user-assigned managed identity for a resource.

```csharp
IResourceBuilder<IUserAssignedManagedIdentityResource> WithUserAssignedManagedIdentity<T>(
    this IResourceBuilder<T> builder,
    string? identityName = null)
    where T : IResourceWithEnvironment
```

**Parameters:**
- `identityName` (optional): Custom name for the managed identity. Defaults to `{resourceName}-identity`.

**Returns:** Identity resource builder that can be passed to `AllowManagedIdentities()`.

**Example:**
```csharp
var api = builder.AddProject<Projects.Api>("api");
var apiIdentity = api.WithUserAssignedManagedIdentity("my-api-identity");
```

---

#### `AllowManagedIdentities()`
Configures a service to accept authentication from specific managed identities.

```csharp
IResourceBuilder<T> AllowManagedIdentities<T>(
    this IResourceBuilder<T> builder,
    params IResourceBuilder<IUserAssignedManagedIdentityResource>[] allowedIdentities)
    where T : IResourceWithEnvironment
```

**Parameters:**
- `allowedIdentities`: One or more managed identity resources that can authenticate to this service.

**Returns:** The resource builder for chaining.

**Example:**
```csharp
var service = builder.AddProject<Projects.Service>("service")
    .AllowManagedIdentities(identity1, identity2, identity3);
```

---

### Runtime Extensions

#### `AddManagedIdentityAuth()` (HttpClient)
Adds managed identity token authentication to an HTTP client.

```csharp
IHttpClientBuilder AddManagedIdentityAuth(this IHttpClientBuilder builder)
```

**Example:**
```csharp
builder.Services.AddHttpClient("myservice", client => { ... })
    .AddManagedIdentityAuth();
```

---

#### `AddManagedIdentityAuthentication()` (Service)
Configures a service to validate incoming managed identity tokens.

```csharp
IServiceCollection AddManagedIdentityAuthentication(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
```

**Example:**
```csharp
builder.Services.AddManagedIdentityAuthentication(
    builder.Configuration, 
    builder.Environment);
```

---

**Note:** After calling `AddManagedIdentityAuthentication()`, use standard ASP.NET Core middleware:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

---

## 🔒 Security Best Practices

### Principle of Least Privilege
Only grant access to the specific identities that need it:

```csharp
var service = builder.AddProject<Projects.Service>("service")
    .AllowManagedIdentities(identity1); // Only identity1, not identity2
```

### Automatic Configuration
When you call `.AllowManagedIdentities()`, the library automatically provisions:
- **`ALLOWED_CLIENT_IDS`** - Comma-separated list of allowed managed identity client IDs
- **`AZURE_TENANT_ID`** - Azure tenant ID for token validation (resolved during deployment)
- **`USE_MANAGED_IDENTITY_AUTH`** - Flag to enable authentication in the caller

### Token Scope
Tokens use `https://management.azure.com/.default` scope by default. This is validated by the callee.

### Audit Logging
All authentication events are logged:

**Caller (BackendApi):**
```
[INF] Managed identity authentication enabled for service-to-service calls
[DBG] Added managed identity token to request to https://aiservice...
```

**Callee (AIService):**
```
[INF] Successfully authenticated authorized managed identity: a1b2c3d4-...
[WRN] Rejected unauthorized client: xyz-... Expected: a1b2c3d4-...
```

### Environment Isolation
- **Production**: Full authentication enforced
- **Development**: Authentication bypassed for easy local testing

---

## 🧪 Testing

### Local Development
```bash
cd AppHost
dotnet run
```

**Expected Behavior:**
- ✅ Services communicate without authentication
- ✅ No Azure dependencies required
- ✅ Logs show authentication disabled

### Azure Deployment
```bash
azd up
```

**Post-Deployment:**

1. **Verify Managed Identity Created:**
```bash
az identity list --resource-group <rg> --query "[].{Name:name, ClientId:clientId}"
```

2. **Check Environment Variables (Auto-Provisioned):**
```bash
# AIService should have ALLOWED_CLIENT_IDS and AZURE_TENANT_ID
az containerapp show --name aiservice --resource-group <rg> \
  --query "properties.template.containers[0].env[?name=='ALLOWED_CLIENT_IDS' || name=='AZURE_TENANT_ID']"
```

Expected output:
```json
[
  {
    "name": "ALLOWED_CLIENT_IDS",
    "value": "a1b2c3d4-ef56-7890-abcd-ef1234567890"
  },
  {
    "name": "AZURE_TENANT_ID",
    "value": "12345678-1234-1234-1234-123456789012"
  }
]
```

3. **Verify Authentication:**
```bash
# Check logs for authentication success
az containerapp logs show --name aiservice --resource-group <rg> --tail 50
```

Expected log:
```
[INF] Successfully authenticated authorized managed identity: a1b2c3d4-...
```

---

## 🐛 Troubleshooting

### Issue: `401 Unauthorized` from callee

**Causes:**
1. Managed identity not provisioned
2. Client ID not in allowed list
3. Token acquisition failed

**Solution:**
```bash
# Verify AZURE_TENANT_ID and ALLOWED_CLIENT_IDS are set (auto-provisioned by library)
az containerapp show --name aiservice --resource-group <rg> \
  --query "properties.template.containers[0].env[?name=='AZURE_TENANT_ID' || name=='ALLOWED_CLIENT_IDS']"

# Should show both environment variables with placeholders or resolved values
```

**Note:** `AZURE_TENANT_ID` is now automatically provisioned when you call `.AllowManagedIdentities()`. No manual configuration needed!

### Issue: `Failed to acquire managed identity token`

**Causes:**
1. Running locally but environment set to Production
2. Managed identity not assigned to Container App

**Solution:**
```bash
# Verify identity assignment
az containerapp identity show --name backendapi --resource-group <rg>

# Should show:
# {
#   "type": "UserAssigned",
#   "userAssignedIdentities": { ... }
# }
```

### Issue: Authentication works locally but not in Azure

**Cause:** `ASPNETCORE_ENVIRONMENT` not set correctly.

**Solution:**
Verify environment in AppHost:
```csharp
.WithEnvironment("ASPNETCORE_ENVIRONMENT", 
    builder.ExecutionContext.IsPublishMode ? "Production" : "Development")
```

---

## 📚 Advanced Scenarios

### Multiple Callers
Allow multiple services to authenticate:

```csharp
var api1 = builder.AddProject<Projects.Api1>("api1");
var api1Identity = api1.WithUserAssignedManagedIdentity();

var api2 = builder.AddProject<Projects.Api2>("api2");
var api2Identity = api2.WithUserAssignedManagedIdentity();

var sharedService = builder.AddProject<Projects.Shared>("shared")
    .AllowManagedIdentities(api1Identity, api2Identity);
```

### Custom Identity Names
Use custom names for identities:

```csharp
var identity = api.WithUserAssignedManagedIdentity("prod-api-identity-v2");
```

### Chained Authentication
Service A calls Service B, which calls Service C:

```csharp
var serviceA = builder.AddProject<Projects.ServiceA>("servicea");
var serviceAIdentity = serviceA.WithUserAssignedManagedIdentity();

var serviceB = builder.AddProject<Projects.ServiceB>("serviceb")
    .AllowManagedIdentities(serviceAIdentity);
var serviceBIdentity = serviceB.WithUserAssignedManagedIdentity();

var serviceC = builder.AddProject<Projects.ServiceC>("servicec")
    .AllowManagedIdentities(serviceBIdentity);
```

---

## 🎉 Benefits

| Feature | Manual Implementation | This Library |
|---------|----------------------|--------------|
| **Lines of Code** | ~200+ | ~10 |
| **Bicep Templates** | Manual | Automatic |
| **Identity Provisioning** | Manual | Automatic |
| **Environment Config** | Manual (AZURE_TENANT_ID, etc.) | Automatic |
| **Type Safety** | ❌ | ✅ |
| **Local Dev** | Complex | Seamless |
| **Audit Logging** | Manual | Built-in |
| **Tenant ID Config** | Manual | Auto-provisioned |

---

## 📄 License

This library follows the same license as your Aspire application.

## 🤝 Contributing

Contributions welcome! Please ensure:
- ✅ All tests pass
- ✅ Code follows .NET conventions
- ✅ Documentation updated

---

**Last Updated:** December 2, 2025  
**Aspire Version:** 13.0.0  
**.NET Version:** 10.0

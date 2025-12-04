using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Aspire.Azure.ManagedIdentity.Client;

/// <summary>
/// Extension methods for configuring managed identity authentication in services.
/// </summary>
public static class ManagedIdentityAuthServiceExtensions
{
    /// <summary>
    /// Adds managed identity authentication middleware that validates incoming JWT tokens from allowed identities.
    /// In Production: Requires valid Entra ID tokens from configured client IDs.
    /// In Development: Allows all requests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <param name="scope">The OAuth2 scope/audience for token validation (e.g., "https://management.azure.com").</param>
    public static IServiceCollection AddManagedIdentityAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string scope,
        string tenantId)
    {
        if (environment.IsProduction())
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                    options.Audience = scope;
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuers = new[]
                        {
                            $"https://login.microsoftonline.com/{tenantId}/v2.0",
                            $"https://sts.windows.net/{tenantId}/"
                        }
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("ManagedIdentityAuth");
                            logger.LogError(context.Exception, "Authentication failed for request from {UserAgent}", 
                                context.Request.Headers.UserAgent.ToString());
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("ManagedIdentityAuth");
                            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                            
                            // Try multiple claim types that might contain the principal/object ID
                            // Managed identity tokens use full URI claim types
                            var principalId = context.Principal?.Claims.FirstOrDefault(c => 
                                c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier" || // Full URI (most common for managed identities)
                                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" || // Alternative URI
                                c.Type == "oid" ||           // Short name (user tokens)
                                c.Type == "sub"              // Subject claim (fallback)
                            )?.Value;
                            
                            // Get allowed principal IDs (Object IDs) from configuration (comma-separated)
                            var allowedPrincipalIds = config[ManagedIdentityAuthConstants.AllowedPrincipalIdsKey]?.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            if (allowedPrincipalIds != null && allowedPrincipalIds.Length > 0)
                            {
                                if (principalId == null || !allowedPrincipalIds.Contains(principalId))
                                {
                                    logger.LogWarning("Rejected unauthorized principal: {PrincipalId}. Allowed: {AllowedPrincipalIds}", 
                                        principalId, string.Join(", ", allowedPrincipalIds));
                                    context.Fail("Unauthorized client identity");
                                    return Task.CompletedTask;
                                }
                            }
                            
                            logger.LogInformation("Successfully authenticated authorized managed identity (OID): {PrincipalId}", principalId);
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(ManagedIdentityAuthConstants.PolicyName, policy => 
                    policy.RequireAuthenticatedUser());
            });
        }
        else
        {
            // Development: Allow all requests
            services.AddAuthorization(options =>
            {
                options.AddPolicy(ManagedIdentityAuthConstants.PolicyName, policy => 
                    policy.RequireAssertion(_ => true));
            });
        }

        return services;
    }

    /// <summary>
    /// Configures the application to use managed identity authentication middleware.
    /// Should be called after UseRouting() and before UseEndpoints().
    /// </summary>
    public static IApplicationBuilder UseManagedIdentityAuthentication(
        this IApplicationBuilder app,
        IHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }
}

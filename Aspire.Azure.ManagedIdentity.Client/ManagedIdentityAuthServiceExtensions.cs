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
        string scope)
    {
        var requireAuth = configuration.GetValue<bool>("REQUIRE_MANAGED_IDENTITY_AUTH");

        if (requireAuth && environment.IsProduction())
        {
            var tenantId = configuration["AZURE_TENANT_ID"] 
                ?? throw new InvalidOperationException("AZURE_TENANT_ID must be configured in Production when REQUIRE_MANAGED_IDENTITY_AUTH is enabled");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{tenantId}";
                    options.Audience = scope;
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
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
                            var clientId = context.Principal?.Claims.FirstOrDefault(c => c.Type == "appid")?.Value;
                            
                            // Get allowed client IDs from configuration (comma-separated)
                            var allowedClientIds = config["ASPIRE_EXTENSIONS_ALLOWED_CLIENT_IDS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            
                            if (allowedClientIds != null && allowedClientIds.Length > 0)
                            {
                                if (clientId == null || !allowedClientIds.Contains(clientId))
                                {
                                    logger.LogWarning("Rejected unauthorized client: {ClientId}. Allowed: {AllowedClientIds}", 
                                        clientId, string.Join(", ", allowedClientIds));
                                    context.Fail("Unauthorized client identity");
                                    return Task.CompletedTask;
                                }
                            }
                            
                            logger.LogInformation("Successfully authenticated authorized managed identity: {ClientId}", clientId);
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

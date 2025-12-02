using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.Lifecycle;

/// <summary>
/// Lifecycle hook that provisions user-assigned managed identities and configures Container Apps.
/// </summary>
internal class ManagedIdentityProvisioningHook : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        eventing.Subscribe<BeforeStartEvent>((beforeStartEvent, ct) =>
        {
            // Process all resources that have managed identity annotations
            foreach (var resource in beforeStartEvent.Model.Resources)
            {
                ProcessManagedIdentityAuthentication(resource);
                ProcessAllowedIdentities(resource);
            }
            
            return Task.CompletedTask;
        });
        
        return Task.CompletedTask;
    }

    private void ProcessManagedIdentityAuthentication(IResource resource)
    {
        var authAnnotation = resource.Annotations
            .OfType<ManagedIdentityAuthenticationAnnotation>()
            .FirstOrDefault();
            
        if (authAnnotation == null)
            return;

        // Add infrastructure provisioning for the user-assigned managed identity
        if (resource is IResourceWithEnvironment envResource)
        {
            // The actual Bicep provisioning will happen through ConfigureInfrastructure
            // For now, we mark it for provisioning
            var identity = authAnnotation.Identity;
            
            // Add environment variable that will reference the identity's client ID
            envResource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
            {
                // This will be replaced with actual client ID during deployment
                context.EnvironmentVariables[$"ASPIRE_EXTENSIONS_MANAGED_IDENTITY_CLIENT_ID"] = $"{{{identity.Name}.clientId}}";
            }));
        }
    }

    private void ProcessAllowedIdentities(IResource resource)
    {
        var allowedAnnotation = resource.Annotations
            .OfType<AllowedIdentitiesAnnotation>()
            .FirstOrDefault();
            
        if (allowedAnnotation == null)
            return;

        if (resource is IResourceWithEnvironment envResource)
        {
            // Configure environment variables for authentication validation
            var clientIds = string.Join(",", 
                allowedAnnotation.AllowedIdentities.Select(i => $"{{{i.Name}.clientId}}"));
            
            envResource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
            {
                context.EnvironmentVariables["ASPIRE_EXTENSIONS_ALLOWED_CLIENT_IDS"] = clientIds;
                // Automatically configure the tenant ID from Azure provisioning context
                // This will be resolved to the actual tenant ID during deployment
                context.EnvironmentVariables["ASPIRE_EXTENSIONS_AZURE_TENANT_ID"] = "{env.AZURE_TENANT_ID}";
            }));
        }
    }
}

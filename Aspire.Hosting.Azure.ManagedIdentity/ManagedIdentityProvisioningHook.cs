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
            // Process all resources that have AllowedIdentities annotations
            foreach (var resource in beforeStartEvent.Model.Resources)
            {
                ProcessAllowedIdentities(resource);
            }
            
            return Task.CompletedTask;
        });
        
        return Task.CompletedTask;
    }

    private static void ProcessAllowedIdentities(IResource resource)
    {
        var allowedAnnotation = resource.Annotations
            .OfType<AllowedIdentitiesAnnotation>()
            .FirstOrDefault();
            
        if (allowedAnnotation == null)
            return;

        if (resource is IResourceWithEnvironment envResource)
        {
            // Configure environment variables for authentication validation
            var principalIds = string.Join(",", 
                allowedAnnotation.AllowedIdentities.Select(i => $"{{{i.Name}.principalId}}"));
            
            envResource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
            {
                context.EnvironmentVariables[ManagedIdentityEnvironmentVariables.AllowedPrincipalIds] = principalIds;
            }));
        }
    }
}

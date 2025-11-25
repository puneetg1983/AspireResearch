var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Add Ev2 deployment support
builder.AddEv2Environment();

// Conditionally update Secrets based on new KeyVault or existing Connection String
// var secrets = builder.ExecutionContext.IsPublishMode ? builder.AddAzureKeyVault("azurekv") : builder.AddConnectionString("azurekv");
var secrets = builder.AddAzureKeyVault("azurekv");

var cosmos = builder.AddAzureCosmosDB("cosmos-db");
var customers = cosmos.AddCosmosDatabase("customers");
var profiles = customers.AddContainer("profiles", "/id");

builder.AddAzureContainerAppEnvironment("env");

builder.WithSecureDefaults();

var backendApi = builder.AddProject<Projects.BackendApi>("backendapi")
    .WithHttpHealthCheck("/health")
    .WithReference(secrets)
    .WithReference(profiles);

builder.AddProject<Projects.AppServiceDiagnostics_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(backendApi)
    .WaitFor(backendApi);


builder.Build().Run();

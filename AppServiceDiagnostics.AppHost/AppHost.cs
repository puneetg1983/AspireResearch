using Azure.Provisioning.KeyVault;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Add Ev2 deployment support
builder.AddEv2Environment();

// Conditionally update Secrets based on new KeyVault or existing Connection String
// var secrets = builder.ExecutionContext.IsPublishMode ? builder.AddAzureKeyVault("azurekv") : builder.AddConnectionString("azurekv");
var secrets = builder.AddAzureKeyVault("azurekv");

var cosmos = builder.AddAzureCosmosDB("cosmos-db");

// Add Azure AI Foundry for AI-powered joke generation
var foundry = builder.AddAzureAIFoundry("foundry");
var chat = foundry.AddDeployment("chat", "gpt-4o-mini", "2024-07-18", "OpenAI");

builder.AddAzureContainerAppEnvironment("env");

builder.WithSecureDefaults();

var aiService = builder.AddProject<Projects.AppServiceDiagnostics_AIService>("aiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(foundry)
    .WaitFor(foundry);

var backendApi = builder.AddProject<Projects.BackendApi>("backendapi")
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(secrets)
    .WithRoleAssignments(secrets, KeyVaultBuiltInRole.KeyVaultSecretsUser, KeyVaultBuiltInRole.KeyVaultCertificateUser)
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(aiService)
    .WaitFor(aiService);

builder.AddProject<Projects.AppServiceDiagnostics_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(backendApi)
    .WaitFor(backendApi);


builder.Build().Run();

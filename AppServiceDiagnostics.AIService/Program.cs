using AppServiceDiagnostics.AIService;
using Aspire.Azure.ManagedIdentity.Client;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add Azure Chat Completions client using Aspire 13 specifications
builder.AddAzureChatCompletionsClient("foundry");

// Add managed identity authentication
// Validate against the AAD app's client ID (audience in the token)
builder.Services.AddManagedIdentityAuthentication(
    builder.Configuration,
    builder.Environment,
    scope: "1d922779-2742-4cf2-8c82-425cf2c60aa8",
    tenantId: "72f988bf-86f1-41af-91ab-2d7cd011db47");

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register the JokeAgentService
builder.Services.AddSingleton<JokeAgentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable managed identity authentication middleware
app.UseManagedIdentityAuthentication(app.Environment);

app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
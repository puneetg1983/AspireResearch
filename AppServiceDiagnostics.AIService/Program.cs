using AppServiceDiagnostics.AIService;
using Aspire.Azure.ManagedIdentity.Client;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add Azure Chat Completions client using Aspire 13 specifications
builder.AddAzureChatCompletionsClient("foundry");

// Configure managed identity authentication
builder.Services.AddManagedIdentityAuthentication(
    builder.Configuration,
    builder.Environment,
    "https://management.azure.com");

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
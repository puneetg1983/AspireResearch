
namespace BackendApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Tell app to get the KeyVault configuration from Aspire Secrets configuration
        builder.Configuration.AddAzureKeyVaultSecrets("azurekv");
        
        // Inject Certificate Client and Secret Client for controllers
        builder.AddAzureKeyVaultClient("azurekv");
        builder.AddAzureKeyVaultCertificateClient("azurekv");
        
        // Add Redis cache
        builder.AddRedisClient("cache");
        
        // Add Cosmos DB
        builder.AddAzureCosmosClient("cosmos-db");

        // Configure Cosmos DB serialization options
        builder.Services.Configure<Microsoft.Azure.Cosmos.CosmosClientOptions>(options =>
        {
            options.SerializerOptions = new Microsoft.Azure.Cosmos.CosmosSerializationOptions
            {
                PropertyNamingPolicy = Microsoft.Azure.Cosmos.CosmosPropertyNamingPolicy.CamelCase
            };
        });

        // Add AI Service HTTP client using Aspire service discovery
        builder.Services.AddHttpClient("aiservice", client =>
        {
            // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
            // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
            client.BaseAddress = new Uri("https+http://aiservice");
        });

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}

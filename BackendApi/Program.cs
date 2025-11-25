
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

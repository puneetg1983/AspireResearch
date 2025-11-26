using AppServiceDiagnostics.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppServiceDiagnostics.Web;

public class BackendClient(HttpClient httpClient)
{
    

    public async Task<IEnumerable<string>> GetSecretNamesAsync()
    {
        var secretNames = await httpClient.GetFromJsonAsync<IEnumerable<string>>("/secrets");
        return secretNames ?? Enumerable.Empty<string>();
    }

    public async Task<string?> GetSecretValueAsync(string secretName)
    {
        var secretValue = await httpClient.GetStringAsync($"/secrets/{secretName}");
        return secretValue;
    }

    public async Task<IEnumerable<string>> GetCertificateNames()
    {
        var certificateNames = await httpClient.GetFromJsonAsync<IEnumerable<string>>("/certificates");
        return certificateNames ?? Enumerable.Empty<string>();
    }

    public async Task<LoadedCert?> GetCertificate(string certificateName)
    {
        var loadedCert = await httpClient.GetFromJsonAsync<LoadedCert>($"/certificates/{certificateName}");
        return loadedCert;
    }

    public async Task<string?> GetAIJokeAsync()
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<JokeResponse>("/api/ai/joke");
            return response?.joke;
        }
        catch (Exception)
        {
            return "Why did the AI cross the road? To get to the other side of the algorithm!";
        }
    }

    private record JokeResponse(string joke);
}

using AppServiceDiagnostics.Models;
using System.Text.Json;
using System.Text;

namespace AppServiceDiagnostics.Web;

public class ProfilesClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfilesClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProfilesClient(HttpClient httpClient, ILogger<ProfilesClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<ProfileResponse> CreateProfileAsync(ProfileRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/profiles", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileResponse>(responseContent, _jsonOptions) ?? 
                       new ProfileResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ProfileResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new ProfileResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return new ProfileResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<ProfileResponse> GetProfileAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/profiles/{Uri.EscapeDataString(id)}?partitionKey={Uri.EscapeDataString(partitionKey)}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileResponse>(responseContent, _jsonOptions) ?? 
                       new ProfileResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ProfileResponse 
                { 
                    Success = false, 
                    Message = "Profile not found" 
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ProfileResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new ProfileResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile");
            return new ProfileResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<ProfileResponse> UpdateProfileAsync(string id, string partitionKey, ProfileRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"/api/profiles/{Uri.EscapeDataString(id)}?partitionKey={Uri.EscapeDataString(partitionKey)}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileResponse>(responseContent, _jsonOptions) ?? 
                       new ProfileResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ProfileResponse 
                { 
                    Success = false, 
                    Message = "Profile not found" 
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ProfileResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new ProfileResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return new ProfileResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<ProfileResponse> DeleteProfileAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/profiles/{Uri.EscapeDataString(id)}?partitionKey={Uri.EscapeDataString(partitionKey)}");
            
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileResponse>(responseContent, _jsonOptions) ?? 
                       new ProfileResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ProfileResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new ProfileResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile");
            return new ProfileResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<ProfileResponse> QueryProfilesAsync(ProfileQuery query)
    {
        try
        {
            var json = JsonSerializer.Serialize(query, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/profiles/query", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileResponse>(responseContent, _jsonOptions) ?? 
                       new ProfileResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ProfileResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new ProfileResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying profiles");
            return new ProfileResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<ProfileResponse> GetAllProfilesAsync(int maxItems = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/profiles?maxItems={maxItems}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileResponse>(responseContent, _jsonOptions) ?? 
                       new ProfileResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ProfileResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new ProfileResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all profiles");
            return new ProfileResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }
}
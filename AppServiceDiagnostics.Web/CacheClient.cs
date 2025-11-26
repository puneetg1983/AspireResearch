using AppServiceDiagnostics.Models;
using System.Text.Json;

namespace AppServiceDiagnostics.Web;

public class CacheClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CacheClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheClient(HttpClient httpClient, ILogger<CacheClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<CacheResponse> SetCacheItemAsync(CacheRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/cache/set", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CacheResponse>(responseContent, _jsonOptions) ?? 
                       new CacheResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<CacheResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new CacheResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache item");
            return new CacheResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<CacheResponse> GetCacheItemAsync(string key)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/cache/get/{Uri.EscapeDataString(key)}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CacheResponse>(responseContent, _jsonOptions) ?? 
                       new CacheResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new CacheResponse 
                { 
                    Success = false, 
                    Message = "Item not found or has expired" 
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<CacheResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new CacheResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache item");
            return new CacheResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<CacheResponse> DeleteCacheItemAsync(string key)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/cache/delete/{Uri.EscapeDataString(key)}");
            
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CacheResponse>(responseContent, _jsonOptions) ?? 
                       new CacheResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<CacheResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new CacheResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache item");
            return new CacheResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<List<string>> GetAllKeysAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/cache/keys");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(responseContent, _jsonOptions) ?? new List<string>();
            }
            else
            {
                _logger.LogError("Failed to get cache keys. Status: {StatusCode}", response.StatusCode);
                return new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache keys");
            return new List<string>();
        }
    }

    public async Task<CacheResponse> ClearCacheAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/cache/clear", null);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CacheResponse>(responseContent, _jsonOptions) ?? 
                       new CacheResponse { Success = false, Message = "Failed to deserialize response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<CacheResponse>(errorContent, _jsonOptions);
                return errorResponse ?? new CacheResponse 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return new CacheResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }
}
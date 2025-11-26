using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace AppServiceDiagnostics.Models;

public class Profile
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("partitionKey")]
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("_etag")]
    public string? ETag { get; set; }
}

public class ProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Profile? Profile { get; set; }
    public List<Profile> Profiles { get; set; } = new();
}

public class ProfileQuery
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public int MaxItems { get; set; } = 100;
}
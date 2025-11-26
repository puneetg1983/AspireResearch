namespace AppServiceDiagnostics.Models;

public class CacheItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime? Expiration { get; set; }
}

public class CacheRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? ExpirationMinutes { get; set; }
}

public class CacheResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CacheItem? Item { get; set; }
}
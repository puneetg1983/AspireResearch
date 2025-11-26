using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using AppServiceDiagnostics.Models;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly IDatabase _database;
    private readonly ILogger<CacheController> _logger;

    public CacheController(IConnectionMultiplexer redis, ILogger<CacheController> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    [HttpPost("set")]
    public async Task<ActionResult<CacheResponse>> SetCacheItem([FromBody] CacheRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Value))
            {
                return BadRequest(new CacheResponse 
                { 
                    Success = false, 
                    Message = "Key and Value are required" 
                });
            }

            var cacheItem = new CacheItem
            {
                Key = request.Key,
                Value = request.Value,
                Expiration = request.ExpirationMinutes.HasValue 
                    ? DateTime.UtcNow.AddMinutes(request.ExpirationMinutes.Value) 
                    : null
            };

            var json = JsonSerializer.Serialize(cacheItem);
            
            bool result;
            if (request.ExpirationMinutes.HasValue)
            {
                var expiry = TimeSpan.FromMinutes(request.ExpirationMinutes.Value);
                result = await _database.StringSetAsync(request.Key, json, expiry);
            }
            else
            {
                result = await _database.StringSetAsync(request.Key, json);
            }

            if (result)
            {
                _logger.LogInformation("Successfully set cache item with key: {Key}", request.Key);
                return Ok(new CacheResponse 
                { 
                    Success = true, 
                    Message = "Item cached successfully",
                    Item = cacheItem
                });
            }
            else
            {
                return StatusCode(500, new CacheResponse 
                { 
                    Success = false, 
                    Message = "Failed to cache item" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache item with key: {Key}", request.Key);
            return StatusCode(500, new CacheResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpGet("get/{key}")]
    public async Task<ActionResult<CacheResponse>> GetCacheItem(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new CacheResponse 
                { 
                    Success = false, 
                    Message = "Key is required" 
                });
            }

            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                return NotFound(new CacheResponse 
                { 
                    Success = false, 
                    Message = "Item not found or has expired" 
                });
            }

            var cacheItem = JsonSerializer.Deserialize<CacheItem>((string)value!);
            
            _logger.LogInformation("Successfully retrieved cache item with key: {Key}", key);
            return Ok(new CacheResponse 
            { 
                Success = true, 
                Message = "Item retrieved successfully",
                Item = cacheItem
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache item with key: {Key}", key);
            return StatusCode(500, new CacheResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpDelete("delete/{key}")]
    public async Task<ActionResult<CacheResponse>> DeleteCacheItem(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new CacheResponse 
                { 
                    Success = false, 
                    Message = "Key is required" 
                });
            }

            var result = await _database.KeyDeleteAsync(key);
            
            if (result)
            {
                _logger.LogInformation("Successfully deleted cache item with key: {Key}", key);
                return Ok(new CacheResponse 
                { 
                    Success = true, 
                    Message = "Item deleted successfully" 
                });
            }
            else
            {
                return NotFound(new CacheResponse 
                { 
                    Success = false, 
                    Message = "Item not found" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache item with key: {Key}", key);
            return StatusCode(500, new CacheResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpGet("keys")]
    public async Task<ActionResult<IEnumerable<string>>> GetAllKeys()
    {
        try
        {
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: "*").Select(key => (string)key!).ToList();
            
            _logger.LogInformation("Successfully retrieved {Count} cache keys", keys.Count);
            return Ok(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache keys");
            return StatusCode(500, new { Message = $"Internal server error: {ex.Message}" });
        }
    }

    [HttpPost("clear")]
    public async Task<ActionResult<CacheResponse>> ClearCache()
    {
        try
        {
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            
            _logger.LogInformation("Successfully cleared cache");
            return Ok(new CacheResponse 
            { 
                Success = true, 
                Message = "Cache cleared successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new CacheResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }
}
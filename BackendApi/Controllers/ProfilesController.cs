using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using AppServiceDiagnostics.Models;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly Container _container;
    private readonly ILogger<ProfilesController> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProfilesController(CosmosClient cosmosClient, ILogger<ProfilesController> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        try
        {
            _container = cosmosClient.GetContainer("customers", "profiles-v2");
            _logger.LogInformation("Successfully initialized Cosmos DB container reference");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Cosmos DB container reference. This might indicate connection or configuration issues.");
            // Don't throw here - let individual methods handle the null container
            _container = null!;
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProfileResponse>> CreateProfile([FromBody] ProfileRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new ProfileResponse 
                { 
                    Success = false, 
                    Message = "Name and Email are required" 
                });
            }

            var profile = new Profile
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKey = request.Email.ToLowerInvariant(), // Using email as partition key
                Name = request.Name,
                Email = request.Email,
                Department = request.Department,
                Metadata = request.Metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var response = await _container.CreateItemAsync(profile, new PartitionKey(profile.PartitionKey));
            
            _logger.LogInformation("Successfully created profile with ID: {Id}", profile.Id);
            return Ok(new ProfileResponse 
            { 
                Success = true, 
                Message = "Profile created successfully",
                Profile = response.Resource
            });
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Profile creation conflict for email: {Email}", request.Email);
            return Conflict(new ProfileResponse 
            { 
                Success = false, 
                Message = "A profile with this email already exists" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return StatusCode(500, new ProfileResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProfileResponse>> GetProfile(string id, [FromQuery] string partitionKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(partitionKey))
            {
                return BadRequest(new ProfileResponse 
                { 
                    Success = false, 
                    Message = "ID and partition key are required" 
                });
            }

            var response = await _container.ReadItemAsync<Profile>(id, new PartitionKey(partitionKey));
            
            _logger.LogInformation("Successfully retrieved profile with ID: {Id}", id);
            return Ok(new ProfileResponse 
            { 
                Success = true, 
                Message = "Profile retrieved successfully",
                Profile = response.Resource
            });
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new ProfileResponse 
            { 
                Success = false, 
                Message = "Profile not found" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile with ID: {Id}", id);
            return StatusCode(500, new ProfileResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(string id, [FromBody] ProfileRequest request, [FromQuery] string partitionKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(partitionKey))
            {
                return BadRequest(new ProfileResponse 
                { 
                    Success = false, 
                    Message = "ID and partition key are required" 
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new ProfileResponse 
                { 
                    Success = false, 
                    Message = "Name and Email are required" 
                });
            }

            // First, read the existing item to get the ETag
            var existingResponse = await _container.ReadItemAsync<Profile>(id, new PartitionKey(partitionKey));
            var existingProfile = existingResponse.Resource;

            // Update the profile
            existingProfile.Name = request.Name;
            existingProfile.Email = request.Email;
            existingProfile.Department = request.Department;
            existingProfile.Metadata = request.Metadata;
            existingProfile.UpdatedAt = DateTime.UtcNow;

            var response = await _container.ReplaceItemAsync(existingProfile, id, new PartitionKey(partitionKey));
            
            _logger.LogInformation("Successfully updated profile with ID: {Id}", id);
            return Ok(new ProfileResponse 
            { 
                Success = true, 
                Message = "Profile updated successfully",
                Profile = response.Resource
            });
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new ProfileResponse 
            { 
                Success = false, 
                Message = "Profile not found" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile with ID: {Id}", id);
            return StatusCode(500, new ProfileResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ProfileResponse>> DeleteProfile(string id, [FromQuery] string partitionKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(partitionKey))
            {
                return BadRequest(new ProfileResponse 
                { 
                    Success = false, 
                    Message = "ID and partition key are required" 
                });
            }

            await _container.DeleteItemAsync<Profile>(id, new PartitionKey(partitionKey));
            
            _logger.LogInformation("Successfully deleted profile with ID: {Id}", id);
            return Ok(new ProfileResponse 
            { 
                Success = true, 
                Message = "Profile deleted successfully" 
            });
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new ProfileResponse 
            { 
                Success = false, 
                Message = "Profile not found" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile with ID: {Id}", id);
            return StatusCode(500, new ProfileResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpPost("query")]
    public async Task<ActionResult<ProfileResponse>> QueryProfiles([FromBody] ProfileQuery query)
    {
        try
        {
            var queryDefinition = new QueryDefinition("SELECT * FROM c");
            var parameters = new List<string>();

            // Build dynamic query based on provided filters
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c WHERE CONTAINS(LOWER(c.name), @name)")
                    .WithParameter("@name", query.Name.ToLowerInvariant());
            }
            else if (!string.IsNullOrWhiteSpace(query.Email))
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = @email")
                    .WithParameter("@email", query.Email.ToLowerInvariant());
            }
            else if (!string.IsNullOrWhiteSpace(query.Department))
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c WHERE LOWER(c.department) = @department")
                    .WithParameter("@department", query.Department.ToLowerInvariant());
            }

            var iterator = _container.GetItemQueryIterator<Profile>(
                queryDefinition, 
                requestOptions: new QueryRequestOptions { MaxItemCount = query.MaxItems });

            var profiles = new List<Profile>();
            
            while (iterator.HasMoreResults && profiles.Count < query.MaxItems)
            {
                var response = await iterator.ReadNextAsync();
                profiles.AddRange(response);
            }
            
            _logger.LogInformation("Successfully queried {Count} profiles", profiles.Count);
            return Ok(new ProfileResponse 
            { 
                Success = true, 
                Message = $"Retrieved {profiles.Count} profiles",
                Profiles = profiles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying profiles");
            return StatusCode(500, new ProfileResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        try
        {
            return Ok(new { Status = "Healthy", Message = "Profiles controller is working", DatabaseName = "customers", ContainerName = "profiles-v2" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Health check failed");
            return StatusCode(500, new { Status = "Unhealthy", Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetAllProfiles([FromQuery] int maxItems = 100)
    {
        try
        {
            // First, check if we can access the container
            if (_container == null)
            {
                _logger.LogWarning("Container is null");
                return Ok(new ProfileResponse 
                { 
                    Success = false, 
                    Message = "Cosmos DB container is not initialized",
                    Profiles = new List<Profile>()
                });
            }

            var queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c.createdAt DESC");
            var iterator = _container.GetItemQueryIterator<Profile>(
                queryDefinition, 
                requestOptions: new QueryRequestOptions { MaxItemCount = maxItems });

            var profiles = new List<Profile>();
            
            while (iterator.HasMoreResults && profiles.Count < maxItems)
            {
                var response = await iterator.ReadNextAsync();
                profiles.AddRange(response);
            }
            
            _logger.LogInformation("Successfully retrieved {Count} profiles", profiles.Count);
            return Ok(new ProfileResponse 
            { 
                Success = true, 
                Message = $"Retrieved {profiles.Count} profiles",
                Profiles = profiles
            });
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB error retrieving all profiles. Status: {StatusCode}, Message: {Message}", ex.StatusCode, ex.Message);
            return StatusCode(500, new ProfileResponse 
            { 
                Success = false, 
                Message = $"Database error (Status: {ex.StatusCode}): {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all profiles");
            return StatusCode(500, new ProfileResponse 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }
}
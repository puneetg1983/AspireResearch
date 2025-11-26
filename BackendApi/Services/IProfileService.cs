using AppServiceDiagnostics.Models;

namespace BackendApi.Services;

public interface IProfileService
{
    Task<ProfileResponse> CreateProfileAsync(ProfileRequest request);
    Task<ProfileResponse> GetProfileAsync(string id, string partitionKey);
    Task<ProfileResponse> UpdateProfileAsync(string id, string partitionKey, ProfileRequest request);
    Task<ProfileResponse> DeleteProfileAsync(string id, string partitionKey);
    Task<ProfileResponse> QueryProfilesAsync(ProfileQuery query);
    Task<ProfileResponse> GetAllProfilesAsync(int maxItems = 100);
}
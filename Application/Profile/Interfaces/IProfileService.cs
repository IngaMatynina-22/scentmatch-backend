using ScentMatch.Application.Profile.DTOs;

namespace ScentMatch.Application.Profile.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> GetPreferencesAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> UpdatePreferencesAsync(int userId, UpdateUserPreferencesRequest request, CancellationToken cancellationToken = default);
}

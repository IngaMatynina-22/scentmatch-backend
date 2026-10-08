using ScentMatch.Domain.Entities;

namespace ScentMatch.Application.Auth.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAt) CreateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}

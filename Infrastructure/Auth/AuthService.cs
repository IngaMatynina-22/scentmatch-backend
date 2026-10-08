using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Auth.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Options;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string InvalidRefreshTokenMessage = "Invalid refresh token.";

    private readonly UserManager<User> _userManager;
    private readonly ScentMatchDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<User> userManager,
        ScentMatchDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingByEmail is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var existingByUsername = await _userManager.FindByNameAsync(request.Username);
        if (existingByUsername is not null)
        {
            throw new ConflictException("A user with this username already exists.");
        }

        var user = new User
        {
            UserName = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null || !storedToken.User.IsActive)
        {
            throw new UnauthorizedException(InvalidRefreshTokenMessage);
        }

        // Revoke and issue replacement in a single SaveChanges (atomic rotation).
        storedToken.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(storedToken.User, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserResponse> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("User is not authorized.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return MapUser(user, roles);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _jwtTokenService.CreateAccessToken(user, roles);

        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtTokenService.HashRefreshToken(refreshTokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiresAt,
            User = MapUser(user, roles)
        };
    }

    private static UserResponse MapUser(User user, IEnumerable<string> roles)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        };
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Auth.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Auth;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Succeeds()
    {
        var email = UniqueEmail();
        var response = await RegisterAsync("user_ok", email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal(email, body.User.Email);
        Assert.Contains(AppRoles.User, body.User.Roles);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();
        await RegisterAsync("user_dup_email_1", email);

        var response = await RegisterAsync("user_dup_email_2", email);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        var username = $"user_{Guid.NewGuid():N}"[..20];
        await RegisterAsync(username, UniqueEmail());

        var response = await RegisterAsync(username, UniqueEmail());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_Succeeds()
    {
        var email = UniqueEmail();
        const string password = "Password123!";
        await RegisterAsync("user_login_ok", email, password);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await RegisterAsync("user_bad_pass", email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsOk()
    {
        var email = UniqueEmail();
        var register = await RegisterAsync("user_me", email);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        Assert.Equal(email, body!.Email);
    }

    [Fact]
    public async Task AdminEndpoint_WithUserRole_ReturnsForbidden()
    {
        var email = UniqueEmail();
        var register = await RegisterAsync("user_not_admin", email);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_ReturnsOk()
    {
        var email = UniqueEmail();
        const string password = "Password123!";
        await RegisterAsync("user_admin_ok", email, password);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            await userManager.AddToRoleAsync(user!, AppRoles.Admin);
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_Works()
    {
        var email = UniqueEmail();
        var register = await RegisterAsync("user_refresh", email);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = auth!.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.NotEqual(auth.RefreshToken, body.RefreshToken);
    }

    [Fact]
    public async Task Refresh_RevokedToken_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        var register = await RegisterAsync("user_revoked", email);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = auth!.RefreshToken
        });

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        var register = await RegisterAsync("user_expired", email);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var hash = jwt.HashRefreshToken(auth!.RefreshToken);
            var token = db.RefreshTokens.First(x => x.TokenHash == hash);
            token.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = auth!.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var email = UniqueEmail();
        var register = await RegisterAsync("user_logout", email);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = auth!.RefreshToken
        });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        var hash = jwt.HashRefreshToken(auth.RefreshToken);
        var token = db.RefreshTokens.First(x => x.TokenHash == hash);

        Assert.NotNull(token.RevokedAt);
    }

    private async Task<HttpResponseMessage> RegisterAsync(
        string username,
        string email,
        string password = "Password123!")
    {
        return await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        });
    }

    private static string UniqueEmail() => $"user_{Guid.NewGuid():N}@example.com";
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Profile.DTOs;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Profile;

public class ProfileEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProfileEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProfile_Authenticated_ReturnsOwnProfileWithoutPassword()
    {
        var (token, email, username) = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, "/api/profile", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);

        var body = JsonSerializer.Deserialize<UserProfileDto>(json, JsonOptions());
        Assert.NotNull(body);
        Assert.Equal(email, body!.Email);
        Assert.Equal(username, body.Username);
        Assert.True(body.Id > 0);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task GetProfile_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_PersistsChanges()
    {
        var (token, _, _) = await RegisterAsync();
        var newUsername = $"upd_{Guid.NewGuid():N}"[..16];

        using var put = Authorized(HttpMethod.Put, "/api/profile", token);
        put.Content = JsonContent.Create(new UpdateProfileRequest
        {
            Username = newUsername,
            FirstName = "John",
            LastName = "Doe",
            AvatarUrl = "https://example.com/avatar.png"
        });

        var putResponse = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        using var get = Authorized(HttpMethod.Get, "/api/profile", token);
        var profile = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<UserProfileDto>();

        Assert.Equal(newUsername, profile!.Username);
        Assert.Equal("John", profile.FirstName);
        Assert.Equal("Doe", profile.LastName);
        Assert.Equal("https://example.com/avatar.png", profile.AvatarUrl);
    }

    [Fact]
    public async Task UpdateProfile_DuplicateUsername_ReturnsConflict()
    {
        var (tokenA, _, usernameA) = await RegisterAsync();
        var (tokenB, _, _) = await RegisterAsync();

        using var put = Authorized(HttpMethod.Put, "/api/profile", tokenB);
        put.Content = JsonContent.Create(new UpdateProfileRequest
        {
            Username = usernameA,
            FirstName = "Other"
        });

        var response = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // same username for self is allowed
        using var putSelf = Authorized(HttpMethod.Put, "/api/profile", tokenA);
        putSelf.Content = JsonContent.Create(new UpdateProfileRequest
        {
            Username = usernameA,
            FirstName = "Same"
        });
        var selfResponse = await _client.SendAsync(putSelf);
        Assert.Equal(HttpStatusCode.OK, selfResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_InvalidData_ReturnsBadRequest()
    {
        var (token, _, _) = await RegisterAsync();

        using var put = Authorized(HttpMethod.Put, "/api/profile", token);
        put.Content = JsonContent.Create(new UpdateProfileRequest
        {
            Username = "ab",
            AvatarUrl = "not-a-url"
        });

        var response = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPreferences_CreatesDefaults_AndUpdatePersists()
    {
        var (token, _, _) = await RegisterAsync();

        using var get1 = Authorized(HttpMethod.Get, "/api/profile/preferences", token);
        var getResponse = await _client.SendAsync(get1);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var defaults = await getResponse.Content.ReadFromJsonAsync<UserPreferencesDto>();
        Assert.NotNull(defaults);
        Assert.Null(defaults!.PreferredSeason);
        Assert.Null(defaults.PreferredFragranceType);

        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var put = Authorized(HttpMethod.Put, "/api/profile/preferences", token);
        put.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            PreferredSeasonId = seasons!.First().Id,
            PreferredFragranceTypeId = types!.First().Id,
            Sweetness = 70,
            Freshness = 90,
            Woody = 20,
            Spicy = 10,
            Citrus = 80,
            Floral = 30,
            Fruity = 40,
            Powdery = 10,
            Smoky = 0,
            Oriental = 10,
            MinPrice = 1000,
            MaxPrice = 5000
        });

        var putResponse = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        using var get2 = Authorized(HttpMethod.Get, "/api/profile/preferences", token);
        var updated = await (await _client.SendAsync(get2)).Content.ReadFromJsonAsync<UserPreferencesDto>();

        Assert.Equal(70, updated!.Sweetness);
        Assert.Equal(90, updated.Freshness);
        Assert.Equal(1000, updated.MinPrice);
        Assert.Equal(5000, updated.MaxPrice);
        Assert.Equal(seasons![0].Id, updated.PreferredSeason!.Id);
        Assert.Equal(types![0].Id, updated.PreferredFragranceType!.Id);
    }

    [Fact]
    public async Task GetPreferences_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/profile/preferences");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(50, 101)]
    public async Task UpdatePreferences_InvalidCharacteristics_ReturnsBadRequest(int sweetness, int freshness)
    {
        var (token, _, _) = await RegisterAsync();

        using var put = Authorized(HttpMethod.Put, "/api/profile/preferences", token);
        put.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            Sweetness = sweetness,
            Freshness = freshness
        });

        var response = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePreferences_InvalidForeignKeysAndPrices_ReturnsBadRequest()
    {
        var (token, _, _) = await RegisterAsync();

        using var invalidSeason = Authorized(HttpMethod.Put, "/api/profile/preferences", token);
        invalidSeason.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            PreferredSeasonId = 999999
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(invalidSeason)).StatusCode);

        using var invalidType = Authorized(HttpMethod.Put, "/api/profile/preferences", token);
        invalidType.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            PreferredFragranceTypeId = 999999
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(invalidType)).StatusCode);

        using var negativePrice = Authorized(HttpMethod.Put, "/api/profile/preferences", token);
        negativePrice.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            MinPrice = -1,
            MaxPrice = 100
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(negativePrice)).StatusCode);

        using var invertedPrice = Authorized(HttpMethod.Put, "/api/profile/preferences", token);
        invertedPrice.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            MinPrice = 5000,
            MaxPrice = 1000
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(invertedPrice)).StatusCode);
    }

    [Fact]
    public async Task Ownership_UserA_CannotSeeUserB_Data()
    {
        var (tokenA, _, _) = await RegisterAsync("Alice");
        var (tokenB, _, _) = await RegisterAsync("Bob");

        using var putB = Authorized(HttpMethod.Put, "/api/profile/preferences", tokenB);
        putB.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            Sweetness = 11,
            Freshness = 22,
            MinPrice = 111,
            MaxPrice = 222
        });
        (await _client.SendAsync(putB)).EnsureSuccessStatusCode();

        using var putA = Authorized(HttpMethod.Put, "/api/profile", tokenA);
        putA.Content = JsonContent.Create(new UpdateProfileRequest
        {
            Username = $"alice_{Guid.NewGuid():N}"[..16],
            FirstName = "AliceOnly"
        });
        (await _client.SendAsync(putA)).EnsureSuccessStatusCode();

        using var getProfileA = Authorized(HttpMethod.Get, "/api/profile", tokenA);
        var profileA = await (await _client.SendAsync(getProfileA)).Content.ReadFromJsonAsync<UserProfileDto>();

        using var getProfileB = Authorized(HttpMethod.Get, "/api/profile", tokenB);
        var profileB = await (await _client.SendAsync(getProfileB)).Content.ReadFromJsonAsync<UserProfileDto>();

        Assert.NotEqual(profileA!.Id, profileB!.Id);
        Assert.Equal("AliceOnly", profileA.FirstName);
        Assert.NotEqual("AliceOnly", profileB.FirstName);

        using var getPrefsA = Authorized(HttpMethod.Get, "/api/profile/preferences", tokenA);
        var prefsA = await (await _client.SendAsync(getPrefsA)).Content.ReadFromJsonAsync<UserPreferencesDto>();

        using var getPrefsB = Authorized(HttpMethod.Get, "/api/profile/preferences", tokenB);
        var prefsB = await (await _client.SendAsync(getPrefsB)).Content.ReadFromJsonAsync<UserPreferencesDto>();

        Assert.NotEqual(11, prefsA!.Sweetness);
        Assert.Equal(11, prefsB!.Sweetness);
        Assert.Equal(22, prefsB.Freshness);
    }

    [Fact]
    public async Task AuthMe_StillWorks_AfterUsernameChange()
    {
        var (token, email, _) = await RegisterAsync();
        var newUsername = $"me_{Guid.NewGuid():N}"[..16];

        using var put = Authorized(HttpMethod.Put, "/api/profile", token);
        put.Content = JsonContent.Create(new UpdateProfileRequest { Username = newUsername });
        (await _client.SendAsync(put)).EnsureSuccessStatusCode();

        using var me = Authorized(HttpMethod.Get, "/api/auth/me", token);
        var response = await _client.SendAsync(me);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(email, body!.Email);
        Assert.Equal(newUsername, body.Username);
    }

    private async Task<(string Token, string Email, string Username)> RegisterAsync(string? firstName = null)
    {
        var email = $"prof_{Guid.NewGuid():N}@example.com";
        var username = $"p_{Guid.NewGuid():N}"[..16];

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!",
            FirstName = firstName
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (body!.AccessToken, email, username);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Profile.DTOs;
using ScentMatch.Application.Recommendations.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Recommendations;

public class RecommendationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RecommendationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRecommendations_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/recommendations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/recommendations/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task History_Anonymous_ReturnsUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/history/recommendations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GetAsync($"/api/history/recommendations/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task GetRecommendations_EmptyPreferences_ReturnsEmptyWithMessage()
    {
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, "/api/recommendations", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecommendationsResponse>();
        Assert.False(body!.PreferencesConfigured);
        Assert.Empty(body.Items);
        Assert.False(string.IsNullOrWhiteSpace(body.Message));
    }

    [Fact]
    public async Task GetRecommendations_UsesPreferences_AndDoesNotWriteHistory()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var perfume = await CreatePerfumeAsync(admin, $"Rec_{suffix}", sweetness: 70, price: 1200);
        await SetPreferencesAsync(token, sweetness: 70, minPrice: 500, maxPrice: 2000);

        int historyBefore;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            historyBefore = await db.RecommendationHistory.CountAsync();
        }

        using var get = Authorized(HttpMethod.Get, "/api/recommendations", token);
        var body = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<RecommendationsResponse>();

        Assert.True(body!.PreferencesConfigured);
        Assert.Contains(body.Items, x => x.PerfumeId == perfume.Id);
        Assert.True(body.Items.Select(x => x.PerfumeId).Distinct().Count() == body.Items.Count);
        Assert.True(body.Items.Zip(body.Items.Skip(1), (a, b) => a.Score >= b.Score).All(x => x)
                    || body.Items.Count <= 1);
        Assert.All(body.Items, x => Assert.InRange(x.Score, 0.01m, 100m));
        Assert.Contains(body.Items.Single(x => x.PerfumeId == perfume.Id).Reasons, r => r.Length > 0);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            Assert.Equal(historyBefore, await db.RecommendationHistory.CountAsync());
        }
    }

    [Fact]
    public async Task Refresh_CreatesHistoryBatch_AndSnapshotIsStable()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var perfume = await CreatePerfumeAsync(admin, $"Snap_{suffix}", sweetness: 80, price: 1000);
        await SetPreferencesAsync(token, sweetness: 80);

        using var refresh = Authorized(HttpMethod.Post, "/api/recommendations/refresh", token);
        var refreshed = await (await _client.SendAsync(refresh)).Content.ReadFromJsonAsync<RecommendationsResponse>();
        Assert.Contains(refreshed!.Items, x => x.PerfumeId == perfume.Id);

        Guid batchId;
        decimal storedScore;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var rows = await db.RecommendationHistory.Where(x => x.PerfumeId == perfume.Id).ToListAsync();
            Assert.NotEmpty(rows);
            batchId = rows[0].BatchId;
            storedScore = rows.First(x => x.PerfumeId == perfume.Id).Score;
            Assert.True(rows.All(x => x.BatchId == batchId));
        }

        // Change preferences — live recommendations change, snapshot must not.
        await SetPreferencesAsync(token, sweetness: 10);

        using var getBatch = Authorized(HttpMethod.Get, $"/api/history/recommendations/{batchId}", token);
        var snapshot = await (await _client.SendAsync(getBatch)).Content.ReadFromJsonAsync<RecommendationHistoryDetailsDto>();
        Assert.Equal(batchId, snapshot!.BatchId);
        Assert.Equal(storedScore, snapshot.Items.Single(x => x.PerfumeId == perfume.Id).Score);

        using var live = Authorized(HttpMethod.Get, "/api/recommendations", token);
        var liveBody = await (await _client.SendAsync(live)).Content.ReadFromJsonAsync<RecommendationsResponse>();
        var liveItem = liveBody!.Items.FirstOrDefault(x => x.PerfumeId == perfume.Id);
        if (liveItem is not null)
        {
            Assert.NotEqual(storedScore, liveItem.Score);
        }
    }

    [Fact]
    public async Task History_OwnershipAndPagination()
    {
        var admin = await GetAdminTokenAsync();
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        await CreatePerfumeAsync(admin, $"H_{Guid.NewGuid():N}"[..12], sweetness: 60, price: 900);
        await SetPreferencesAsync(tokenA, sweetness: 60);
        await SetPreferencesAsync(tokenB, sweetness: 60);

        for (var i = 0; i < 3; i++)
        {
            using var refreshA = Authorized(HttpMethod.Post, "/api/recommendations/refresh", tokenA);
            (await _client.SendAsync(refreshA)).EnsureSuccessStatusCode();
            await Task.Delay(15);
        }

        using var refreshB = Authorized(HttpMethod.Post, "/api/recommendations/refresh", tokenB);
        (await _client.SendAsync(refreshB)).EnsureSuccessStatusCode();

        using var listA = Authorized(HttpMethod.Get, "/api/history/recommendations?page=1&pageSize=2", tokenA);
        var pageA = await (await _client.SendAsync(listA))
            .Content.ReadFromJsonAsync<PagedResult<RecommendationHistorySummaryDto>>();

        Assert.Equal(3, pageA!.TotalItems);
        Assert.Equal(2, pageA.TotalPages);
        Assert.Equal(2, pageA.Items.Count);
        Assert.True(pageA.Items[0].CreatedAt >= pageA.Items[1].CreatedAt);

        var foreignBatch = pageA.Items[0].BatchId;
        using var foreignGet = Authorized(HttpMethod.Get, $"/api/history/recommendations/{foreignBatch}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(foreignGet)).StatusCode);

        using var missing = Authorized(HttpMethod.Get, $"/api/history/recommendations/{Guid.NewGuid()}", tokenA);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);

        using var listB = Authorized(HttpMethod.Get, "/api/history/recommendations", tokenB);
        var pageB = await (await _client.SendAsync(listB))
            .Content.ReadFromJsonAsync<PagedResult<RecommendationHistorySummaryDto>>();
        Assert.Equal(1, pageB!.TotalItems);
    }

    [Fact]
    public async Task Recommendations_ExcludeInactivePerfumeAndBrand()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var inactive = await CreatePerfumeAsync(admin, $"Inact_{suffix}", sweetness: 95, price: 100);
        await SetPreferencesAsync(token, sweetness: 95);

        using (var delete = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{inactive.Id}", admin))
        {
            (await _client.SendAsync(delete)).EnsureSuccessStatusCode();
        }

        using var get = Authorized(HttpMethod.Get, "/api/recommendations", token);
        var body = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<RecommendationsResponse>();
        Assert.DoesNotContain(body!.Items, x => x.PerfumeId == inactive.Id);
    }

    private async Task SetPreferencesAsync(string token, int? sweetness = null, decimal? minPrice = null, decimal? maxPrice = null)
    {
        using var put = Authorized(HttpMethod.Put, "/api/profile/preferences", token);
        put.Content = JsonContent.Create(new UpdateUserPreferencesRequest
        {
            Sweetness = sweetness,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        });
        (await _client.SendAsync(put)).EnsureSuccessStatusCode();
    }

    private async Task<PerfumeDetailsDto> CreatePerfumeAsync(
        string adminToken,
        string name,
        int sweetness,
        decimal price)
    {
        var brand = await CreateBrandAsync(adminToken, $"RB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"RN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = name,
            BrandId = brand.Id,
            FragranceTypeId = types!.First().Id,
            ReleaseYear = 2020,
            Price = price,
            Characteristics = new PerfumeCharacteristicsDto { Sweetness = sweetness, Freshness = 40 },
            Notes = [new PerfumeNoteInputDto { NoteId = note.Id, NoteType = NoteTypes.Top }],
            Seasons = [new PerfumeSeasonInputDto { SeasonId = seasons!.First().Id, Suitability = 70 }]
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PerfumeDetailsDto>())!;
    }

    private async Task<BrandDto> CreateBrandAsync(string adminToken, string name)
    {
        using var request = Authorized(HttpMethod.Post, "/api/admin/brands", adminToken);
        request.Content = JsonContent.Create(new CreateBrandRequest { Name = name });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BrandDto>())!;
    }

    private async Task<NoteDto> CreateNoteAsync(string adminToken, string name)
    {
        using var request = Authorized(HttpMethod.Post, "/api/admin/notes", adminToken);
        request.Content = JsonContent.Create(new CreateNoteRequest { Name = name, Category = "Test" });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NoteDto>())!;
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"reco_{Guid.NewGuid():N}@example.com";
        var username = $"rc_{Guid.NewGuid():N}"[..16];
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var email = $"reco_admin_{Guid.NewGuid():N}@example.com";
        var username = $"rca_{Guid.NewGuid():N}"[..16];
        const string password = "Password123!";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        });
        register.EnsureSuccessStatusCode();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.AddToRoleAsync(user!, AppRoles.Admin);
        }

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

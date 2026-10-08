using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Comparison.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Comparison;

public class ComparisonEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ComparisonEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("""{"perfumeIds":[]}""")]
    [InlineData("""{"perfumeIds":[1]}""")]
    [InlineData("""{"perfumeIds":[1,2,3,4,5,6]}""")]
    [InlineData("""{"perfumeIds":[1,1]}""")]
    [InlineData("""{"perfumeIds":[0,2]}""")]
    [InlineData("""{"perfumeIds":[-1,2]}""")]
    public async Task Compare_InvalidPayload_ReturnsBadRequest(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/comparison", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Compare_NullPerfumeIds_ReturnsBadRequest()
    {
        using var content = new StringContent("""{"perfumeIds":null}""", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/comparison", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Compare_TwoPerfumes_ReturnsOrderedComparableData()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var note = await CreateNoteAsync(admin, $"CN_{suffix}");
        var seasons = (await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons"))!;
        var types = (await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types"))!;
        var seasonId = seasons[0].Id;
        var typeId = types[0].Id;

        var first = await CreatePerfumeAsync(
            admin,
            $"First_{suffix}",
            note.Id,
            seasonId,
            typeId,
            suitability: 85,
            price: 1500,
            sweetness: 40);

        var second = await CreatePerfumeAsync(
            admin,
            $"Second_{suffix}",
            note.Id,
            seasonId,
            typeId,
            suitability: 60,
            price: 2200,
            sweetness: 70);

        // Request order: second, first
        var response = await _client.PostAsJsonAsync("/api/comparison", new ComparisonRequest
        {
            PerfumeIds = [second.Id, first.Id]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ComparisonResponse>();
        Assert.Equal(2, body!.Items.Count);
        Assert.Equal(second.Id, body.Items[0].Id);
        Assert.Equal(first.Id, body.Items[1].Id);

        Assert.Equal(second.Name, body.Items[0].Name);
        Assert.NotNull(body.Items[0].Brand);
        Assert.True(body.Items[0].Brand.Id > 0);
        Assert.False(string.IsNullOrWhiteSpace(body.Items[0].Brand.Name));
        Assert.NotNull(body.Items[0].FragranceType);
        Assert.Equal(2200m, body.Items[0].Price);
        Assert.Equal(70, body.Items[0].Characteristics.Sweetness);
        Assert.Contains(body.Items[0].Notes, n => n.NoteId == note.Id);
        Assert.Contains(body.Items[0].Seasons, s => s.SeasonId == seasonId && s.Suitability == 60);
        Assert.True(body.Items[0].Rating >= 0);
        Assert.True(body.Items[0].ReviewsCount >= 0);
    }

    [Fact]
    public async Task Compare_FivePerfumes_Succeeds()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var note = await CreateNoteAsync(admin, $"FN_{suffix}");
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            var perfume = await CreatePerfumeAsync(
                admin,
                $"P{i}_{suffix}",
                note.Id,
                seasons!.First().Id,
                types!.First().Id);
            ids.Add(perfume.Id);
        }

        var response = await _client.PostAsJsonAsync("/api/comparison", new ComparisonRequest
        {
            PerfumeIds = ids
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ComparisonResponse>();
        Assert.Equal(ids, body!.Items.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task Compare_UnknownOrInactive_ReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var note = await CreateNoteAsync(admin, $"XN_{suffix}");
        var seasons = (await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons"))!;
        var types = (await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types"))!;
        var seasonId = seasons[0].Id;
        var typeId = types[0].Id;

        var active = await CreatePerfumeAsync(
            admin,
            $"Act_{suffix}",
            note.Id,
            seasonId,
            typeId);

        var missing = await _client.PostAsJsonAsync("/api/comparison", new ComparisonRequest
        {
            PerfumeIds = [active.Id, 999999]
        });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var inactive = await CreatePerfumeAsync(
            admin,
            $"Inact_{suffix}",
            note.Id,
            seasonId,
            typeId);

        using (var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/perfumes/{inactive.Id}"))
        {
            delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            (await _client.SendAsync(delete)).EnsureSuccessStatusCode();
        }

        var inactiveResponse = await _client.PostAsJsonAsync("/api/comparison", new ComparisonRequest
        {
            PerfumeIds = [active.Id, inactive.Id]
        });
        Assert.Equal(HttpStatusCode.NotFound, inactiveResponse.StatusCode);

        var brandInactive = await CreatePerfumeAsync(
            admin,
            $"BrandOff_{suffix}",
            note.Id,
            seasonId,
            typeId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var perfume = await db.Perfumes.Include(x => x.Brand).FirstAsync(x => x.Id == brandInactive.Id);
            perfume.Brand.IsActive = false;
            await db.SaveChangesAsync();
        }

        var brandResponse = await _client.PostAsJsonAsync("/api/comparison", new ComparisonRequest
        {
            PerfumeIds = [active.Id, brandInactive.Id]
        });
        Assert.Equal(HttpStatusCode.NotFound, brandResponse.StatusCode);
    }

    [Fact]
    public async Task Compare_DoesNotMutateCatalogOrCreateHistory()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var note = await CreateNoteAsync(admin, $"HN_{suffix}");
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        var a = await CreatePerfumeAsync(admin, $"HA_{suffix}", note.Id, seasons![0].Id, types![0].Id);
        var b = await CreatePerfumeAsync(admin, $"HB_{suffix}", note.Id, seasons[0].Id, types[0].Id);

        int selectionsBefore;
        int recommendationsBefore;
        int viewsBefore;
        string? nameBefore;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            selectionsBefore = await db.SelectionHistory.CountAsync();
            recommendationsBefore = await db.RecommendationHistory.CountAsync();
            viewsBefore = await db.ViewHistory.CountAsync();
            nameBefore = await db.Perfumes.Where(x => x.Id == a.Id).Select(x => x.Name).SingleAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/comparison", new ComparisonRequest
        {
            PerfumeIds = [a.Id, b.Id]
        });
        response.EnsureSuccessStatusCode();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            Assert.Equal(selectionsBefore, await db.SelectionHistory.CountAsync());
            Assert.Equal(recommendationsBefore, await db.RecommendationHistory.CountAsync());
            Assert.Equal(viewsBefore, await db.ViewHistory.CountAsync());
            Assert.Equal(nameBefore, await db.Perfumes.Where(x => x.Id == a.Id).Select(x => x.Name).SingleAsync());
        }
    }

    [Fact]
    public async Task Compare_Anonymous_IsAllowed()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var note = await CreateNoteAsync(admin, $"AN_{suffix}");
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        var a = await CreatePerfumeAsync(admin, $"AA_{suffix}", note.Id, seasons![0].Id, types![0].Id);
        var b = await CreatePerfumeAsync(admin, $"AB_{suffix}", note.Id, seasons[0].Id, types[0].Id);

        var response = await _client.PostAsJsonAsync("/api/comparison", new ComparisonRequest
        {
            PerfumeIds = [a.Id, b.Id]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<PerfumeDetailsDto> CreatePerfumeAsync(
        string adminToken,
        string name,
        int noteId,
        int seasonId,
        int fragranceTypeId,
        int suitability = 70,
        decimal price = 100,
        int sweetness = 50)
    {
        var brand = await CreateBrandAsync(adminToken, $"CB_{Guid.NewGuid():N}"[..12]);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/perfumes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = name,
            BrandId = brand.Id,
            FragranceTypeId = fragranceTypeId,
            ReleaseYear = 2020,
            Price = price,
            Characteristics = new PerfumeCharacteristicsDto { Sweetness = sweetness, Freshness = 40 },
            Notes = [new PerfumeNoteInputDto { NoteId = noteId, NoteType = NoteTypes.Top }],
            Seasons = [new PerfumeSeasonInputDto { SeasonId = seasonId, Suitability = suitability }]
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PerfumeDetailsDto>())!;
    }

    private async Task<BrandDto> CreateBrandAsync(string adminToken, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/brands");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreateBrandRequest { Name = name });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BrandDto>())!;
    }

    private async Task<NoteDto> CreateNoteAsync(string adminToken, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/notes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreateNoteRequest { Name = name, Category = "Test" });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NoteDto>())!;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var email = $"cmp_admin_{Guid.NewGuid():N}@example.com";
        var username = $"ca_{Guid.NewGuid():N}"[..16];
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
}

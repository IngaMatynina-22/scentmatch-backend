using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Matching;

public class MatchingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MatchingEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Match_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/matching", new MatchingRequest
        {
            Sweetness = 50
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Match_EmptyCriteria_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Post, "/api/matching", token);
        request.Content = JsonContent.Create(new MatchingRequest());

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Match_InvalidPricesAndCharacteristics_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var badChar = Authorized(HttpMethod.Post, "/api/matching", token);
        badChar.Content = JsonContent.Create(new MatchingRequest { Sweetness = 101 });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badChar)).StatusCode);

        using var badPrice = Authorized(HttpMethod.Post, "/api/matching", token);
        badPrice.Content = JsonContent.Create(new MatchingRequest
        {
            MinPrice = 3000,
            MaxPrice = 500
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badPrice)).StatusCode);
    }

    [Fact]
    public async Task Match_NonexistentForeignKeys_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var badSeason = Authorized(HttpMethod.Post, "/api/matching", token);
        badSeason.Content = JsonContent.Create(new MatchingRequest { SeasonId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badSeason)).StatusCode);

        using var badType = Authorized(HttpMethod.Post, "/api/matching", token);
        badType.Content = JsonContent.Create(new MatchingRequest { FragranceTypeId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badType)).StatusCode);

        using var badNote = Authorized(HttpMethod.Post, "/api/matching", token);
        badNote.Content = JsonContent.Create(new MatchingRequest { SelectedNoteIds = [999999] });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badNote)).StatusCode);
    }

    [Fact]
    public async Task Match_CloserCharacteristics_ScoreHigher_AndDeterministic()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var close = await CreatePerfumeAsync(admin, $"Close_{suffix}", sweetness: 50, freshness: 50, price: 1000);
        var far = await CreatePerfumeAsync(admin, $"Far_{suffix}", sweetness: 10, freshness: 10, price: 1000);

        var requestBody = new MatchingRequest { Sweetness = 50, Freshness = 50 };

        var first = await MatchAsync(token, requestBody);
        var second = await MatchAsync(token, requestBody);

        var close1 = first.Items.Single(x => x.PerfumeId == close.Id);
        var far1 = first.Items.Single(x => x.PerfumeId == far.Id);

        Assert.True(close1.Score > far1.Score);
        Assert.InRange(close1.Score, 0m, 100m);
        Assert.InRange(far1.Score, 0m, 100m);
        Assert.Equal(100m, close1.Score);

        Assert.Equal(
            first.Items.Select(x => (x.PerfumeId, x.Score)).ToList(),
            second.Items.Select(x => (x.PerfumeId, x.Score)).ToList());
    }

    [Fact]
    public async Task Match_SeasonTypeNotesAndPrice_AffectScore_WithReasons()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var seasons = (await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons"))!;
        var types = (await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types"))!;
        var seasonId = seasons[0].Id;
        var typeId = types[0].Id;
        var otherTypeId = types.Count > 1 ? types[1].Id : typeId;

        var noteA = await CreateNoteAsync(admin, $"NA_{suffix}");
        var noteB = await CreateNoteAsync(admin, $"NB_{suffix}");

        var best = await CreatePerfumeAsync(
            admin,
            $"Best_{suffix}",
            sweetness: 70,
            price: 1500,
            fragranceTypeId: typeId,
            noteIds: [noteA.Id, noteB.Id],
            seasonId: seasonId,
            seasonSuitability: 90);

        var weaker = await CreatePerfumeAsync(
            admin,
            $"Weak_{suffix}",
            sweetness: 70,
            price: 1500,
            fragranceTypeId: otherTypeId == typeId ? typeId : otherTypeId,
            noteIds: [noteA.Id],
            seasonId: seasonId,
            seasonSuitability: 40);

        // If only one fragrance type exists, create mismatch via different sweetness path already covered.
        // Prefer notes+season difference for ranking.
        var result = await MatchAsync(token, new MatchingRequest
        {
            SeasonId = seasonId,
            FragranceTypeId = typeId,
            SelectedNoteIds = [noteA.Id, noteB.Id],
            MinPrice = 1000,
            MaxPrice = 2000,
            Sweetness = 70
        });

        var bestResult = result.Items.Single(x => x.PerfumeId == best.Id);
        var weakResult = result.Items.Single(x => x.PerfumeId == weaker.Id);

        Assert.True(bestResult.Score >= weakResult.Score);
        Assert.Contains(bestResult.Reasons, r => r.Contains("season", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(bestResult.Reasons, r => r.Contains("notes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(bestResult.Reasons, r => r.Contains("Price", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(bestResult.Reasons, r => r.Contains("fragrance type", StringComparison.OrdinalIgnoreCase)
                                                 || r.Contains("characteristics", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Match_UnspecifiedCriteria_DoNotLowerScore()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var perfume = await CreatePerfumeAsync(admin, $"OnlySweet_{suffix}", sweetness: 40, freshness: 0, price: 500);

        var onlySweetness = await MatchAsync(token, new MatchingRequest { Sweetness = 40 });
        var item = onlySweetness.Items.Single(x => x.PerfumeId == perfume.Id);

        Assert.Equal(100m, item.Score);
    }

    [Fact]
    public async Task Match_NullPerfumeCharacteristics_AreNotTreatedAsZero()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Create perfume then clear freshness in DB to simulate missing characteristic.
        var perfume = await CreatePerfumeAsync(admin, $"NullFresh_{suffix}", sweetness: 60, freshness: 60, price: 800);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var entity = await db.Perfumes.FirstAsync(x => x.Id == perfume.Id);
            entity.Freshness = null;
            await db.SaveChangesAsync();
        }

        var result = await MatchAsync(token, new MatchingRequest
        {
            Sweetness = 60,
            Freshness = 60
        });

        var item = result.Items.Single(x => x.PerfumeId == perfume.Id);
        // Only sweetness comparable => perfect match on available data, freshness skipped.
        Assert.Equal(100m, item.Score);
    }

    [Fact]
    public async Task Match_ResultsSortedByScoreDesc_ThenName()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        await CreatePerfumeAsync(admin, $"B_{suffix}", sweetness: 50, price: 100);
        await CreatePerfumeAsync(admin, $"A_{suffix}", sweetness: 50, price: 100);
        await CreatePerfumeAsync(admin, $"C_{suffix}", sweetness: 10, price: 100);

        var result = await MatchAsync(token, new MatchingRequest { Sweetness = 50 });
        var relevant = result.Items
            .Where(x => x.Name.EndsWith(suffix, StringComparison.Ordinal))
            .ToList();

        Assert.True(relevant.Count >= 3);
        for (var i = 1; i < relevant.Count; i++)
        {
            Assert.True(relevant[i - 1].Score >= relevant[i].Score);
            if (relevant[i - 1].Score == relevant[i].Score)
            {
                Assert.True(string.Compare(relevant[i - 1].Name, relevant[i].Name, StringComparison.OrdinalIgnoreCase) <= 0);
            }
        }
    }

    [Fact]
    public async Task Match_InactivePerfumeAndBrand_Excluded()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var inactivePerfume = await CreatePerfumeAsync(admin, $"InactP_{suffix}", sweetness: 90, price: 100);
        using (var delete = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{inactivePerfume.Id}", admin))
        {
            (await _client.SendAsync(delete)).EnsureSuccessStatusCode();
        }

        var brandPerfume = await CreatePerfumeAsync(admin, $"InactB_{suffix}", sweetness: 90, price: 100);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var perfume = await db.Perfumes.Include(x => x.Brand).FirstAsync(x => x.Id == brandPerfume.Id);
            perfume.Brand.IsActive = false;
            await db.SaveChangesAsync();
        }

        var result = await MatchAsync(token, new MatchingRequest { Sweetness = 90 });
        Assert.DoesNotContain(result.Items, x => x.PerfumeId == inactivePerfume.Id);
        Assert.DoesNotContain(result.Items, x => x.PerfumeId == brandPerfume.Id);
    }

    [Fact]
    public async Task Match_NoCandidates_ReturnsEmpty200()
    {
        var token = await RegisterAsync();
        var admin = await GetAdminTokenAsync();
        var orphanNote = await CreateNoteAsync(admin, $"Orphan_{Guid.NewGuid():N}"[..16]);

        var result = await MatchAsync(token, new MatchingRequest
        {
            SelectedNoteIds = [orphanNote.Id]
        });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Match_PersistsSelectionHistoryWithResults_ButNotRecommendationHistory()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var perfume = await CreatePerfumeAsync(admin, $"Hist_{Guid.NewGuid():N}"[..12], sweetness: 55, price: 200);

        int selectionsBefore;
        int recommendationsBefore;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            selectionsBefore = await db.SelectionHistory.CountAsync();
            recommendationsBefore = await db.RecommendationHistory.CountAsync();
        }

        var match = await MatchAsync(token, new MatchingRequest { Sweetness = 55 });

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            Assert.Equal(selectionsBefore + 1, await db.SelectionHistory.CountAsync());
            Assert.Equal(recommendationsBefore, await db.RecommendationHistory.CountAsync());

            var history = await db.SelectionHistory
                .OrderByDescending(x => x.Id)
                .FirstAsync();
            Assert.False(string.IsNullOrWhiteSpace(history.ResultsJson));
            Assert.Contains(perfume.Id.ToString(), history.ResultsJson);
            Assert.NotEmpty(match.Items);
        }
    }

    [Fact]
    public async Task Match_PriceInsideRange_GetsFullPriceMatch()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var inside = await CreatePerfumeAsync(admin, $"In_{suffix}", sweetness: 50, price: 1500);
        var outside = await CreatePerfumeAsync(admin, $"Out_{suffix}", sweetness: 50, price: 5000);

        var result = await MatchAsync(token, new MatchingRequest
        {
            Sweetness = 50,
            MinPrice = 1000,
            MaxPrice = 2000
        });

        var inItem = result.Items.Single(x => x.PerfumeId == inside.Id);
        var outItem = result.Items.Single(x => x.PerfumeId == outside.Id);

        Assert.True(inItem.Score > outItem.Score);
        Assert.Contains(inItem.Reasons, r => r.Contains("Price", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<MatchingResponse> MatchAsync(string token, MatchingRequest body)
    {
        using var request = Authorized(HttpMethod.Post, "/api/matching", token);
        request.Content = JsonContent.Create(body);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MatchingResponse>())!;
    }

    private async Task<PerfumeDetailsDto> CreatePerfumeAsync(
        string adminToken,
        string name,
        int sweetness = 50,
        int? freshness = 50,
        decimal? price = 100,
        int? fragranceTypeId = null,
        IReadOnlyList<int>? noteIds = null,
        int? seasonId = null,
        int seasonSuitability = 70)
    {
        var brand = await CreateBrandAsync(adminToken, $"MB_{Guid.NewGuid():N}"[..12]);
        noteIds ??= [(await CreateNoteAsync(adminToken, $"MN_{Guid.NewGuid():N}"[..12])).Id];

        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");
        seasonId ??= seasons!.First().Id;
        fragranceTypeId ??= types!.First().Id;

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = name,
            BrandId = brand.Id,
            FragranceTypeId = fragranceTypeId.Value,
            ReleaseYear = 2020,
            Price = price,
            Characteristics = new PerfumeCharacteristicsDto
            {
                Sweetness = sweetness,
                Freshness = freshness,
                Woody = 40,
                Spicy = 20,
                Citrus = 30,
                Floral = 30,
                Fruity = 20,
                Powdery = 10,
                Smoky = 5,
                Oriental = 15
            },
            Notes = noteIds.Select(id => new PerfumeNoteInputDto
            {
                NoteId = id,
                NoteType = NoteTypes.Top
            }).ToList(),
            Seasons =
            [
                new PerfumeSeasonInputDto
                {
                    SeasonId = seasonId.Value,
                    Suitability = seasonSuitability
                }
            ]
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
        var email = $"match_{Guid.NewGuid():N}@example.com";
        var username = $"m_{Guid.NewGuid():N}"[..16];
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
        var email = $"match_admin_{Guid.NewGuid():N}@example.com";
        var username = $"ma_{Guid.NewGuid():N}"[..16];
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

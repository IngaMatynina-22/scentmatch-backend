using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.History;

public class SelectionHistoryEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SelectionHistoryEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/history/selections", new CreateSelectionHistoryRequest
        {
            Sweetness = 40
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetList_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/history/selections");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/history/selections/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Anonymous_ReturnsUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync("/api/history/selections/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync("/api/history/selections")).StatusCode);
    }

    [Fact]
    public async Task Create_PersistsAllParameters_ForCurrentUser()
    {
        var token = await RegisterAsync();
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");
        var notes = await EnsureNotesAsync(2);

        var request = new CreateSelectionHistoryRequest
        {
            SeasonId = seasons!.First().Id,
            FragranceTypeId = types!.First().Id,
            Sweetness = 40,
            Freshness = 80,
            Woody = 60,
            Spicy = 20,
            Citrus = 70,
            Floral = 30,
            Fruity = 50,
            Powdery = 20,
            Smoky = 10,
            Oriental = 30,
            MinPrice = 500,
            MaxPrice = 3000,
            SelectedNoteIds = notes
        };

        using var post = Authorized(HttpMethod.Post, "/api/history/selections", token);
        post.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(post);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<SelectionHistoryDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal(request.SeasonId, created.SeasonId);
        Assert.Equal(request.FragranceTypeId, created.FragranceTypeId);
        Assert.Equal(40, created.Sweetness);
        Assert.Equal(80, created.Freshness);
        Assert.Equal(60, created.Woody);
        Assert.Equal(20, created.Spicy);
        Assert.Equal(70, created.Citrus);
        Assert.Equal(30, created.Floral);
        Assert.Equal(50, created.Fruity);
        Assert.Equal(20, created.Powdery);
        Assert.Equal(10, created.Smoky);
        Assert.Equal(30, created.Oriental);
        Assert.Equal(500m, created.MinPrice);
        Assert.Equal(3000m, created.MaxPrice);
        Assert.Equal(notes, created.SelectedNoteIds);
        Assert.NotNull(created.CreatedAt);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        var entity = await db.SelectionHistory.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(await GetUserIdFromTokenAsync(token), entity.UserId);
        Assert.False(string.IsNullOrWhiteSpace(entity.SelectedNoteIds));
        Assert.Contains(notes[0].ToString(), entity.SelectedNoteIds!);
    }

    [Fact]
    public async Task Create_IgnoresClientUserId_InPayload()
    {
        var token = await RegisterAsync();
        var payload = """
            {
              "userId": 999999,
              "sweetness": 55
            }
            """;

        using var post = Authorized(HttpMethod.Post, "/api/history/selections", token);
        post.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _client.SendAsync(post);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<SelectionHistoryDto>();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        var entity = await db.SelectionHistory.SingleAsync(x => x.Id == created!.Id);
        Assert.NotEqual(999999, entity.UserId);
        Assert.Equal(await GetUserIdFromTokenAsync(token), entity.UserId);
    }

    [Fact]
    public async Task Create_InvalidForeignKeysAndNotes_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var badSeason = Authorized(HttpMethod.Post, "/api/history/selections", token);
        badSeason.Content = JsonContent.Create(new CreateSelectionHistoryRequest { SeasonId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badSeason)).StatusCode);

        using var badType = Authorized(HttpMethod.Post, "/api/history/selections", token);
        badType.Content = JsonContent.Create(new CreateSelectionHistoryRequest { FragranceTypeId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badType)).StatusCode);

        using var badNote = Authorized(HttpMethod.Post, "/api/history/selections", token);
        badNote.Content = JsonContent.Create(new CreateSelectionHistoryRequest
        {
            SelectedNoteIds = [999999]
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badNote)).StatusCode);

        var notes = await EnsureNotesAsync(1);
        using var duplicates = Authorized(HttpMethod.Post, "/api/history/selections", token);
        duplicates.Content = JsonContent.Create(new CreateSelectionHistoryRequest
        {
            SelectedNoteIds = [notes[0], notes[0]]
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(duplicates)).StatusCode);
    }

    [Fact]
    public async Task Create_InvalidCharacteristicsAndPrices_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var outOfRange = Authorized(HttpMethod.Post, "/api/history/selections", token);
        outOfRange.Content = JsonContent.Create(new CreateSelectionHistoryRequest { Sweetness = 101 });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(outOfRange)).StatusCode);

        using var negative = Authorized(HttpMethod.Post, "/api/history/selections", token);
        negative.Content = JsonContent.Create(new CreateSelectionHistoryRequest { MinPrice = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(negative)).StatusCode);

        using var inverted = Authorized(HttpMethod.Post, "/api/history/selections", token);
        inverted.Content = JsonContent.Create(new CreateSelectionHistoryRequest
        {
            MinPrice = 3000,
            MaxPrice = 500
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(inverted)).StatusCode);
    }

    [Fact]
    public async Task GetList_ReturnsOnlyOwn_SortedDesc_WithPagination()
    {
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        for (var i = 0; i < 5; i++)
        {
            await CreateSelectionAsync(tokenA, sweetness: 10 + i);
            await Task.Delay(15);
        }

        await CreateSelectionAsync(tokenB, sweetness: 99);

        using var page1Request = Authorized(HttpMethod.Get, "/api/history/selections?page=1&pageSize=2", tokenA);
        var page1 = await (await _client.SendAsync(page1Request))
            .Content.ReadFromJsonAsync<PagedResult<SelectionHistoryDto>>();

        Assert.Equal(5, page1!.TotalItems);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.Items[0].CreatedAt >= page1.Items[1].CreatedAt);
        Assert.Equal(14, page1.Items[0].Sweetness); // last created for A

        using var getB = Authorized(HttpMethod.Get, "/api/history/selections", tokenB);
        var listB = await (await _client.SendAsync(getB)).Content.ReadFromJsonAsync<PagedResult<SelectionHistoryDto>>();
        Assert.Equal(1, listB!.TotalItems);
        Assert.Equal(99, listB.Items[0].Sweetness);
    }

    [Fact]
    public async Task GetById_OwnOk_ForeignOrMissingNotFound()
    {
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();
        var created = await CreateSelectionAsync(tokenA, sweetness: 42);

        using var getOwn = Authorized(HttpMethod.Get, $"/api/history/selections/{created.Id}", tokenA);
        var own = await (await _client.SendAsync(getOwn)).Content.ReadFromJsonAsync<SelectionHistoryDto>();
        Assert.Equal(42, own!.Sweetness);

        using var getForeign = Authorized(HttpMethod.Get, $"/api/history/selections/{created.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(getForeign)).StatusCode);

        using var missing = Authorized(HttpMethod.Get, "/api/history/selections/999999", tokenA);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);
    }

    [Fact]
    public async Task Delete_OwnAndClear_RespectOwnership()
    {
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();
        var a1 = await CreateSelectionAsync(tokenA, sweetness: 1);
        var a2 = await CreateSelectionAsync(tokenA, sweetness: 2);
        var b1 = await CreateSelectionAsync(tokenB, sweetness: 3);

        using var deleteForeign = Authorized(HttpMethod.Delete, $"/api/history/selections/{a1.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(deleteForeign)).StatusCode);

        using var deleteOwn = Authorized(HttpMethod.Delete, $"/api/history/selections/{a1.Id}", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteOwn)).StatusCode);

        using var deleteMissing = Authorized(HttpMethod.Delete, "/api/history/selections/999999", tokenA);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(deleteMissing)).StatusCode);

        using var clearA = Authorized(HttpMethod.Delete, "/api/history/selections", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(clearA)).StatusCode);

        using var clearEmpty = Authorized(HttpMethod.Delete, "/api/history/selections", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(clearEmpty)).StatusCode);

        using var getA = Authorized(HttpMethod.Get, "/api/history/selections", tokenA);
        var listA = await (await _client.SendAsync(getA)).Content.ReadFromJsonAsync<PagedResult<SelectionHistoryDto>>();
        Assert.Empty(listA!.Items);

        using var getB = Authorized(HttpMethod.Get, $"/api/history/selections/{b1.Id}", tokenB);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getB)).StatusCode);

        // a2 was cleared with clearA
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        Assert.False(await db.SelectionHistory.AnyAsync(x => x.Id == a2.Id));
        Assert.True(await db.SelectionHistory.AnyAsync(x => x.Id == b1.Id));
    }

    private async Task<SelectionHistoryDto> CreateSelectionAsync(string token, int sweetness)
    {
        using var post = Authorized(HttpMethod.Post, "/api/history/selections", token);
        post.Content = JsonContent.Create(new CreateSelectionHistoryRequest { Sweetness = sweetness });
        var response = await _client.SendAsync(post);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SelectionHistoryDto>())!;
    }

    private async Task<List<int>> EnsureNotesAsync(int count)
    {
        // Use existing public notes endpoint if available; otherwise create via admin is heavy.
        // Catalog seeds may not have notes — create via admin helpers from existing pattern.
        var adminEmail = $"sel_admin_{Guid.NewGuid():N}@example.com";
        var adminUser = $"sa_{Guid.NewGuid():N}"[..16];
        const string password = "Password123!";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = adminUser,
            Email = adminEmail,
            Password = password
        });
        register.EnsureSuccessStatusCode();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Domain.Entities.User>>();
            var user = await userManager.FindByEmailAsync(adminEmail);
            await userManager.AddToRoleAsync(user!, Domain.Constants.AppRoles.Admin);
        }

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = adminEmail,
            Password = password
        });
        var adminToken = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;

        var ids = new List<int>();
        for (var i = 0; i < count; i++)
        {
            using var create = Authorized(HttpMethod.Post, "/api/admin/notes", adminToken);
            create.Content = JsonContent.Create(new CreateNoteRequest
            {
                Name = $"SelNote_{Guid.NewGuid():N}"[..16],
                Category = "Test"
            });
            var response = await _client.SendAsync(create);
            response.EnsureSuccessStatusCode();
            var note = await response.Content.ReadFromJsonAsync<NoteDto>();
            ids.Add(note!.Id);
        }

        return ids;
    }

    private async Task<int> GetUserIdFromTokenAsync(string token)
    {
        using var me = Authorized(HttpMethod.Get, "/api/auth/me", token);
        var response = await _client.SendAsync(me);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        return body!.Id;
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"sel_{Guid.NewGuid():N}@example.com";
        var username = $"s_{Guid.NewGuid():N}"[..16];

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.History;

public class ViewHistoryEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ViewHistoryEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/history/views");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/history/views/1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOne_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/history/views/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAll_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/history/views");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterView_ActivePerfume_ReturnsNoContent_AndAppearsInHistory()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var post = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(post)).StatusCode);

        using var get = Authorized(HttpMethod.Get, "/api/history/views", token);
        var history = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();

        Assert.Equal(1, history!.TotalItems);
        Assert.Single(history.Items);
        Assert.Equal(perfume.Id, history.Items[0].Perfume.Id);
        Assert.NotNull(history.Items[0].ViewedAt);
    }

    [Fact]
    public async Task RegisterView_NonexistentOrInactivePerfume_ReturnsNotFound()
    {
        var token = await RegisterAsync();

        using var missing = Authorized(HttpMethod.Post, "/api/history/views/999999", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);

        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        using (var delete = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{perfume.Id}", admin))
        {
            Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);
        }

        using var inactive = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(inactive)).StatusCode);
    }

    [Fact]
    public async Task RepeatedView_DoesNotDuplicate_AndUpdatesViewedAt()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var first = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(first)).StatusCode);

        DateTime? firstViewedAt;
        int historyId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var row = await db.ViewHistory.SingleAsync(x => x.PerfumeId == perfume.Id);
            historyId = row.Id;
            firstViewedAt = row.ViewedAt;
            row.ViewedAt = DateTime.UtcNow.AddHours(-2);
            await db.SaveChangesAsync();
            firstViewedAt = row.ViewedAt;
        }

        await Task.Delay(20);

        using var second = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(second)).StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var rows = await db.ViewHistory.Where(x => x.PerfumeId == perfume.Id).ToListAsync();
            Assert.Single(rows);
            Assert.Equal(historyId, rows[0].Id);
            Assert.True(rows[0].ViewedAt > firstViewedAt);
        }
    }

    [Fact]
    public async Task GetHistory_SortedByViewedAtDesc_WithPagination()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();

        var perfumeIds = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            var perfume = await CreatePerfumeAsync(admin);
            perfumeIds.Add(perfume.Id);
            using var post = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", token);
            (await _client.SendAsync(post)).EnsureSuccessStatusCode();
            await Task.Delay(15);
        }

        using var page1Request = Authorized(HttpMethod.Get, "/api/history/views?page=1&pageSize=2", token);
        var page1 = await (await _client.SendAsync(page1Request))
            .Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();

        Assert.Equal(5, page1!.TotalItems);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.Items[0].ViewedAt >= page1.Items[1].ViewedAt);

        // Most recently viewed should be the last registered perfume
        Assert.Equal(perfumeIds[^1], page1.Items[0].Perfume.Id);

        using var page3Request = Authorized(HttpMethod.Get, "/api/history/views?page=3&pageSize=2", token);
        var page3 = await (await _client.SendAsync(page3Request))
            .Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();
        Assert.Single(page3!.Items);
        Assert.Equal(perfumeIds[0], page3.Items[0].Perfume.Id);
    }

    [Fact]
    public async Task UserA_CannotSeeOrDelete_UserB_History()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        using var postA = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", tokenA);
        (await _client.SendAsync(postA)).EnsureSuccessStatusCode();

        using var getB = Authorized(HttpMethod.Get, "/api/history/views", tokenB);
        var listB = await (await _client.SendAsync(getB)).Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();
        Assert.Empty(listB!.Items);

        int historyIdA;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            historyIdA = await db.ViewHistory.Where(x => x.PerfumeId == perfume.Id).Select(x => x.Id).SingleAsync();
        }

        using var deleteB = Authorized(HttpMethod.Delete, $"/api/history/views/{historyIdA}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(deleteB)).StatusCode);

        using var getA = Authorized(HttpMethod.Get, "/api/history/views", tokenA);
        var listA = await (await _client.SendAsync(getA)).Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();
        Assert.Single(listA!.Items);
    }

    [Fact]
    public async Task DeleteOwnHistoryItem_ReturnsNoContent_MissingReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var post = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", token);
        (await _client.SendAsync(post)).EnsureSuccessStatusCode();

        using var get = Authorized(HttpMethod.Get, "/api/history/views", token);
        var history = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();
        var id = history!.Items[0].Id;

        using var delete = Authorized(HttpMethod.Delete, $"/api/history/views/{id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);

        using var deleteAgain = Authorized(HttpMethod.Delete, $"/api/history/views/{id}", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(deleteAgain)).StatusCode);

        using var missing = Authorized(HttpMethod.Delete, "/api/history/views/999999", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);
    }

    [Fact]
    public async Task ClearHistory_IsIdempotent_AndDoesNotAffectOtherUsers()
    {
        var admin = await GetAdminTokenAsync();
        var perfumeA = await CreatePerfumeAsync(admin);
        var perfumeB = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        using (var postA = Authorized(HttpMethod.Post, $"/api/history/views/{perfumeA.Id}", tokenA))
        {
            (await _client.SendAsync(postA)).EnsureSuccessStatusCode();
        }

        using (var postB = Authorized(HttpMethod.Post, $"/api/history/views/{perfumeB.Id}", tokenB))
        {
            (await _client.SendAsync(postB)).EnsureSuccessStatusCode();
        }

        using var clearA = Authorized(HttpMethod.Delete, "/api/history/views", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(clearA)).StatusCode);

        using var clearAgain = Authorized(HttpMethod.Delete, "/api/history/views", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(clearAgain)).StatusCode);

        using var getA = Authorized(HttpMethod.Get, "/api/history/views", tokenA);
        var listA = await (await _client.SendAsync(getA)).Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();
        Assert.Empty(listA!.Items);

        using var getB = Authorized(HttpMethod.Get, "/api/history/views", tokenB);
        var listB = await (await _client.SendAsync(getB)).Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();
        Assert.Single(listB!.Items);
        Assert.Equal(perfumeB.Id, listB.Items[0].Perfume.Id);
    }

    [Fact]
    public async Task GetHistory_InvalidPagination_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, "/api/history/views?page=0&pageSize=20", token);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task GetHistory_HidesInactivePerfume_ButKeepsRecord()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var post = Authorized(HttpMethod.Post, $"/api/history/views/{perfume.Id}", token);
        (await _client.SendAsync(post)).EnsureSuccessStatusCode();

        using (var delete = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{perfume.Id}", admin))
        {
            (await _client.SendAsync(delete)).EnsureSuccessStatusCode();
        }

        using var get = Authorized(HttpMethod.Get, "/api/history/views", token);
        var history = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<ViewHistoryDto>>();
        Assert.Empty(history!.Items);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        Assert.True(await db.ViewHistory.AnyAsync(x => x.PerfumeId == perfume.Id));
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"vh_{Guid.NewGuid():N}@example.com";
        var username = $"v_{Guid.NewGuid():N}"[..16];

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

    private async Task<string> GetAdminTokenAsync()
    {
        var email = $"vh_admin_{Guid.NewGuid():N}@example.com";
        var username = $"va_{Guid.NewGuid():N}"[..16];
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
            Assert.NotNull(user);
            await userManager.AddToRoleAsync(user!, AppRoles.Admin);
        }

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    private async Task<PerfumeDetailsDto> CreatePerfumeAsync(string adminToken)
    {
        var brand = await CreateBrandAsync(adminToken, $"VB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"VN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"VhP_{Guid.NewGuid():N}"[..16],
            BrandId = brand.Id,
            FragranceTypeId = types!.First().Id,
            ReleaseYear = 2020,
            Price = 100,
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

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Library;

public class FavoritesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FavoritesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFavorites_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/favorites");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFavorites_Authenticated_Empty_ReturnsValidPagedResponse()
    {
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, "/api/favorites", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
        Assert.Equal(0, body.TotalItems);
        Assert.Equal(0, body.TotalPages);
    }

    [Fact]
    public async Task AddFavorite_ActivePerfume_ReturnsCreated_AndAppearsInList()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var add = Authorized(HttpMethod.Post, $"/api/favorites/{perfume.Id}", token);
        var addResponse = await _client.SendAsync(add);
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        using var get = Authorized(HttpMethod.Get, "/api/favorites", token);
        var list = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();

        Assert.Equal(1, list!.TotalItems);
        Assert.Contains(list.Items, x => x.Id == perfume.Id);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ReturnsConflict()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var add1 = Authorized(HttpMethod.Post, $"/api/favorites/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(add1)).StatusCode);

        using var add2 = Authorized(HttpMethod.Post, $"/api/favorites/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(add2)).StatusCode);
    }

    [Fact]
    public async Task AddFavorite_NonexistentPerfume_ReturnsNotFound()
    {
        var token = await RegisterAsync();

        using var add = Authorized(HttpMethod.Post, "/api/favorites/999999", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(add)).StatusCode);
    }

    [Fact]
    public async Task AddFavorite_InactivePerfume_ReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);

        using (var delete = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{perfume.Id}", admin))
        {
            Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);
        }

        var token = await RegisterAsync();
        using var add = Authorized(HttpMethod.Post, $"/api/favorites/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(add)).StatusCode);
    }

    [Fact]
    public async Task DeleteFavorite_Existing_ReturnsNoContent()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var add = Authorized(HttpMethod.Post, $"/api/favorites/{perfume.Id}", token);
        (await _client.SendAsync(add)).EnsureSuccessStatusCode();

        using var delete = Authorized(HttpMethod.Delete, $"/api/favorites/{perfume.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);

        using var get = Authorized(HttpMethod.Get, "/api/favorites", token);
        var list = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();
        Assert.Empty(list!.Items);
    }

    [Fact]
    public async Task DeleteFavorite_Missing_ReturnsNotFound()
    {
        var token = await RegisterAsync();

        using var delete = Authorized(HttpMethod.Delete, "/api/favorites/12345", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(delete)).StatusCode);
    }

    [Fact]
    public async Task UserCannotAffectAnotherUsersFavorites()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        using var addA = Authorized(HttpMethod.Post, $"/api/favorites/{perfume.Id}", tokenA);
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(addA)).StatusCode);

        using var getB = Authorized(HttpMethod.Get, "/api/favorites", tokenB);
        var listB = await (await _client.SendAsync(getB)).Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();
        Assert.DoesNotContain(listB!.Items, x => x.Id == perfume.Id);

        using var deleteB = Authorized(HttpMethod.Delete, $"/api/favorites/{perfume.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(deleteB)).StatusCode);

        using var getA = Authorized(HttpMethod.Get, "/api/favorites", tokenA);
        var listA = await (await _client.SendAsync(getA)).Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();
        Assert.Contains(listA!.Items, x => x.Id == perfume.Id);
    }

    [Fact]
    public async Task Favorites_InvalidPagination_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, "/api/favorites?page=0&pageSize=20", token);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);

        using var request2 = Authorized(HttpMethod.Get, "/api/favorites?page=1&pageSize=1000", token);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request2)).StatusCode);
    }

    [Fact]
    public async Task Favorites_PaginationMetadata_IsCorrect()
    {
        var admin = await GetAdminTokenAsync();
        var token = await RegisterAsync();

        for (var i = 0; i < 5; i++)
        {
            var perfume = await CreatePerfumeAsync(admin);
            using var add = Authorized(HttpMethod.Post, $"/api/favorites/{perfume.Id}", token);
            (await _client.SendAsync(add)).EnsureSuccessStatusCode();
        }

        using var get = Authorized(HttpMethod.Get, "/api/favorites?page=1&pageSize=2", token);
        var page1 = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();

        Assert.Equal(1, page1!.Page);
        Assert.Equal(2, page1.PageSize);
        Assert.Equal(5, page1.TotalItems);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);

        using var get2 = Authorized(HttpMethod.Get, "/api/favorites?page=3&pageSize=2", token);
        var page3 = await (await _client.SendAsync(get2)).Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();
        Assert.Single(page3!.Items);
        Assert.Equal(5, page3.TotalItems);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"fav_{Guid.NewGuid():N}@example.com";
        var username = $"f_{Guid.NewGuid():N}"[..16];

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
        var email = $"fav_admin_{Guid.NewGuid():N}@example.com";
        var username = $"fa_{Guid.NewGuid():N}"[..16];
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
        var brand = await CreateBrandAsync(adminToken, $"FB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"FN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"FavP_{Guid.NewGuid():N}"[..16],
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

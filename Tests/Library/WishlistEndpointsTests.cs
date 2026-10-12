using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Library;

public class WishlistEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WishlistEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWishlist_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/wishlist");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddListUpdateDeleteWishlist_HappyPath()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var add = Authorized(HttpMethod.Post, "/api/wishlist", token);
        add.Content = JsonContent.Create(new CreateWishlistItemRequest
        {
            PerfumeId = perfume.Id,
            Status = WishlistStatuses.Wanted,
            TargetPrice = 120,
            Currency = "usd",
            Note = "Want this"
        });

        var addResponse = await _client.SendAsync(add);
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        var created = await addResponse.Content.ReadFromJsonAsync<WishlistItemDto>();
        Assert.Equal(WishlistStatuses.Wanted, created!.Status);
        Assert.Equal("USD", created.Currency);

        using var addAgain = Authorized(HttpMethod.Post, "/api/wishlist", token);
        addAgain.Content = JsonContent.Create(new CreateWishlistItemRequest { PerfumeId = perfume.Id });
        var idempotent = await (await _client.SendAsync(addAgain)).Content.ReadFromJsonAsync<WishlistItemDto>();
        Assert.Equal(created.Id, idempotent!.Id);

        using var list = Authorized(HttpMethod.Get, "/api/wishlist", token);
        var page = await (await _client.SendAsync(list)).Content.ReadFromJsonAsync<PagedResult<WishlistItemDto>>();
        Assert.Contains(page!.Items, x => x.Id == created.Id);

        using var put = Authorized(HttpMethod.Put, $"/api/wishlist/{created.Id}", token);
        put.Content = JsonContent.Create(new UpdateWishlistItemRequest
        {
            Status = WishlistStatuses.Purchased,
            TargetPrice = 110,
            Currency = "USD"
        });
        var updated = await (await _client.SendAsync(put)).Content.ReadFromJsonAsync<WishlistItemDto>();
        Assert.Equal(WishlistStatuses.Purchased, updated!.Status);
        Assert.NotNull(updated.PurchasedAt);

        using var delete = Authorized(HttpMethod.Delete, $"/api/wishlist/{created.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);
    }

    [Fact]
    public async Task GetAnotherUsersWishlistItem_ReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        using var add = Authorized(HttpMethod.Post, "/api/wishlist", tokenA);
        add.Content = JsonContent.Create(new CreateWishlistItemRequest { PerfumeId = perfume.Id });
        var created = await (await _client.SendAsync(add)).Content.ReadFromJsonAsync<WishlistItemDto>();

        using var get = Authorized(HttpMethod.Get, $"/api/wishlist/{created!.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(get)).StatusCode);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"wl_{Guid.NewGuid():N}@example.com";
        var username = $"w_{Guid.NewGuid():N}"[..16];

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
        var email = $"wl_admin_{Guid.NewGuid():N}@example.com";
        var username = $"wa_{Guid.NewGuid():N}"[..16];
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
        var brand = await CreateBrandAsync(adminToken, $"WB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"WN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"WlP_{Guid.NewGuid():N}"[..16],
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

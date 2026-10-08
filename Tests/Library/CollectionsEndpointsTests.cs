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

public class CollectionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CollectionsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCollections_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/collections");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCollections_Authenticated_ReturnsOk()
    {
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, "/api/collections", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CollectionDto>>();
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
    }

    [Fact]
    public async Task CreateCollection_ReturnsCreated_AndBelongsToCurrentUser()
    {
        var token = await RegisterAsync();

        using var create = Authorized(HttpMethod.Post, "/api/collections", token);
        create.Content = JsonContent.Create(new CreateCollectionRequest
        {
            Name = "My Summer Scents",
            Description = "Fresh perfumes"
        });

        var createResponse = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CollectionDto>();
        Assert.NotNull(created);
        Assert.Equal("My Summer Scents", created!.Name);
        Assert.Equal("Fresh perfumes", created.Description);
        Assert.Equal(0, created.PerfumeCount);
        Assert.True(created.Id > 0);

        using var get = Authorized(HttpMethod.Get, $"/api/collections/{created.Id}", token);
        var details = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<CollectionDetailsDto>();
        Assert.Equal(created.Id, details!.Id);
        Assert.Equal("My Summer Scents", details.Name);
    }

    [Fact]
    public async Task GetAnotherUsersCollection_ReturnsNotFound()
    {
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        var collection = await CreateCollectionAsync(tokenA, "Private A");

        using var get = Authorized(HttpMethod.Get, $"/api/collections/{collection.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(get)).StatusCode);
    }

    [Fact]
    public async Task UpdateOwnCollection_ReturnsOk()
    {
        var token = await RegisterAsync();
        var collection = await CreateCollectionAsync(token, "Old Name", "Old desc");

        using var put = Authorized(HttpMethod.Put, $"/api/collections/{collection.Id}", token);
        put.Content = JsonContent.Create(new UpdateCollectionRequest
        {
            Name = "New Name",
            Description = "New desc"
        });

        var response = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<CollectionDto>();
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("New desc", updated.Description);
    }

    [Fact]
    public async Task UpdateAnotherUsersCollection_ReturnsNotFound()
    {
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();
        var collection = await CreateCollectionAsync(tokenA, "Owned by A");

        using var put = Authorized(HttpMethod.Put, $"/api/collections/{collection.Id}", tokenB);
        put.Content = JsonContent.Create(new UpdateCollectionRequest
        {
            Name = "Hijack",
            Description = "Nope"
        });

        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(put)).StatusCode);
    }

    [Fact]
    public async Task DeleteOwnCollection_ReturnsNoContent()
    {
        var token = await RegisterAsync();
        var collection = await CreateCollectionAsync(token, "To Delete");

        using var delete = Authorized(HttpMethod.Delete, $"/api/collections/{collection.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);

        using var get = Authorized(HttpMethod.Get, $"/api/collections/{collection.Id}", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(get)).StatusCode);
    }

    [Fact]
    public async Task DeleteAnotherUsersCollection_ReturnsNotFound()
    {
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();
        var collection = await CreateCollectionAsync(tokenA, "Stay");

        using var delete = Authorized(HttpMethod.Delete, $"/api/collections/{collection.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(delete)).StatusCode);

        using var get = Authorized(HttpMethod.Get, $"/api/collections/{collection.Id}", tokenA);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(get)).StatusCode);
    }

    [Fact]
    public async Task AddPerfumeToOwnCollection_AppearsInDetails()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();
        var collection = await CreateCollectionAsync(token, "Summer");

        using var add = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            token);
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(add)).StatusCode);

        using var get = Authorized(HttpMethod.Get, $"/api/collections/{collection.Id}", token);
        var details = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<CollectionDetailsDto>();

        Assert.Equal(1, details!.PerfumeCount);
        Assert.Contains(details.Perfumes, x => x.Id == perfume.Id);
    }

    [Fact]
    public async Task AddDuplicatePerfumeToCollection_ReturnsConflict()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();
        var collection = await CreateCollectionAsync(token, "Dup");

        using var add1 = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            token);
        (await _client.SendAsync(add1)).EnsureSuccessStatusCode();

        using var add2 = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            token);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(add2)).StatusCode);
    }

    [Fact]
    public async Task AddNonexistentOrInactivePerfume_ReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();
        var collection = await CreateCollectionAsync(token, "Missing");

        using var missing = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/999999",
            token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);

        using (var deletePerfume = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{perfume.Id}", admin))
        {
            Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deletePerfume)).StatusCode);
        }

        using var inactive = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(inactive)).StatusCode);
    }

    [Fact]
    public async Task RemovePerfumeFromCollection_ReturnsNoContent_AndMissingReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();
        var collection = await CreateCollectionAsync(token, "Remove");

        using var add = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            token);
        (await _client.SendAsync(add)).EnsureSuccessStatusCode();

        using var remove = Authorized(
            HttpMethod.Delete,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(remove)).StatusCode);

        using var removeAgain = Authorized(
            HttpMethod.Delete,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(removeAgain)).StatusCode);
    }

    [Fact]
    public async Task CannotModifyAnotherUsersCollectionPerfumes()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();
        var collection = await CreateCollectionAsync(tokenA, "A only");

        using var addB = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(addB)).StatusCode);

        using var addA = Authorized(
            HttpMethod.Post,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            tokenA);
        (await _client.SendAsync(addA)).EnsureSuccessStatusCode();

        using var removeB = Authorized(
            HttpMethod.Delete,
            $"/api/collections/{collection.Id}/perfumes/{perfume.Id}",
            tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(removeB)).StatusCode);

        using var getA = Authorized(HttpMethod.Get, $"/api/collections/{collection.Id}", tokenA);
        var details = await (await _client.SendAsync(getA)).Content.ReadFromJsonAsync<CollectionDetailsDto>();
        Assert.Contains(details!.Perfumes, x => x.Id == perfume.Id);
    }

    [Fact]
    public async Task CreateCollection_InvalidName_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var empty = Authorized(HttpMethod.Post, "/api/collections", token);
        empty.Content = JsonContent.Create(new CreateCollectionRequest { Name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(empty)).StatusCode);

        using var whitespace = Authorized(HttpMethod.Post, "/api/collections", token);
        whitespace.Content = JsonContent.Create(new CreateCollectionRequest { Name = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(whitespace)).StatusCode);
    }

    [Fact]
    public async Task Collections_InvalidPagination_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, "/api/collections?page=-1&pageSize=20", token);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Collections_PaginationMetadata_IsCorrect()
    {
        var token = await RegisterAsync();

        for (var i = 0; i < 5; i++)
        {
            await CreateCollectionAsync(token, $"Col {i}");
        }

        using var get = Authorized(HttpMethod.Get, "/api/collections?page=2&pageSize=2", token);
        var page = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<CollectionDto>>();

        Assert.Equal(2, page!.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(5, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task UserA_DoesNotSeeUserB_CollectionsInList()
    {
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        await CreateCollectionAsync(tokenA, "Only A");
        await CreateCollectionAsync(tokenB, "Only B");

        using var getA = Authorized(HttpMethod.Get, "/api/collections", tokenA);
        var listA = await (await _client.SendAsync(getA)).Content.ReadFromJsonAsync<PagedResult<CollectionDto>>();

        Assert.All(listA!.Items, x => Assert.Equal("Only A", x.Name));
        Assert.DoesNotContain(listA.Items, x => x.Name == "Only B");
    }

    private async Task<CollectionDto> CreateCollectionAsync(string token, string name, string? description = null)
    {
        using var create = Authorized(HttpMethod.Post, "/api/collections", token);
        create.Content = JsonContent.Create(new CreateCollectionRequest
        {
            Name = name,
            Description = description
        });

        var response = await _client.SendAsync(create);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CollectionDto>())!;
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"col_{Guid.NewGuid():N}@example.com";
        var username = $"c_{Guid.NewGuid():N}"[..16];

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
        var email = $"col_admin_{Guid.NewGuid():N}@example.com";
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
        var brand = await CreateBrandAsync(adminToken, $"CB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"CN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"ColP_{Guid.NewGuid():N}"[..16],
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

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

namespace ScentMatch.Tests.Catalog;

public class CatalogEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CatalogEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBrands_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/brands");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBrandById_ReturnsOk()
    {
        var adminToken = await GetAdminTokenAsync();
        var created = await CreateBrandAsync(adminToken, $"Brand_{Guid.NewGuid():N}"[..16]);

        var response = await _client.GetAsync($"/api/brands/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BrandDto>();
        Assert.Equal(created.Id, body!.Id);
    }

    [Fact]
    public async Task GetNonexistentBrand_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/brands/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateBrand()
    {
        var adminToken = await GetAdminTokenAsync();
        var name = $"Brand_{Guid.NewGuid():N}"[..16];

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/brands");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreateBrandRequest
        {
            Name = name,
            Country = "France"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BrandDto>();
        Assert.Equal(name, body!.Name);
    }

    [Fact]
    public async Task User_CannotCreateBrand_ReturnsForbidden()
    {
        var userToken = await GetUserTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/brands");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        request.Content = JsonContent.Create(new CreateBrandRequest { Name = $"UserBrand_{Guid.NewGuid():N}"[..16] });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetNotes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/notes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateNote()
    {
        var adminToken = await GetAdminTokenAsync();
        var name = $"Note_{Guid.NewGuid():N}"[..16];

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/notes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreateNoteRequest { Name = name, Category = "Floral" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Admin_DuplicateNote_ReturnsConflict()
    {
        var adminToken = await GetAdminTokenAsync();
        var name = $"DupNote_{Guid.NewGuid():N}"[..16];
        await CreateNoteAsync(adminToken, name);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/notes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreateNoteRequest { Name = name });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetPerfumes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/perfumes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();
        Assert.NotNull(body);
        Assert.True(body!.Page >= 1);
        Assert.True(body.PageSize >= 1);
        Assert.NotNull(body.Items);
    }

    [Fact]
    public async Task GetPerfumeById_ReturnsOk_WithRelationships()
    {
        var adminToken = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeWithRelationsAsync(adminToken);

        var response = await _client.GetAsync($"/api/perfumes/{perfume.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PerfumeDetailsDto>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Notes);
        Assert.NotEmpty(body.Seasons);
        Assert.Equal(NoteTypes.Top, body.Notes.First().NoteType);
        Assert.True(body.Seasons.First().Suitability >= 0);
    }

    [Fact]
    public async Task GetNonexistentPerfume_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/perfumes/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreatePerfume()
    {
        var adminToken = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeWithRelationsAsync(adminToken);

        Assert.True(perfume.Id > 0);
        Assert.NotEmpty(perfume.Notes);
        Assert.NotEmpty(perfume.Seasons);
    }

    [Fact]
    public async Task User_CannotCreatePerfume_ReturnsForbidden()
    {
        var userToken = await GetUserTokenAsync();
        var adminToken = await GetAdminTokenAsync();
        var brand = await CreateBrandAsync(adminToken, $"B_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"N_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/perfumes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"P_{Guid.NewGuid():N}"[..12],
            BrandId = brand.Id,
            FragranceTypeId = types!.First().Id,
            Notes = [new PerfumeNoteInputDto { NoteId = note.Id, NoteType = NoteTypes.Top }],
            Seasons = [new PerfumeSeasonInputDto { SeasonId = seasons!.First().Id, Suitability = 50 }]
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_InvalidPerfumeData_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/perfumes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = "",
            BrandId = 0,
            FragranceTypeId = 0,
            Price = -10,
            Notes = [new PerfumeNoteInputDto { NoteId = 1, NoteType = "Invalid" }]
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotAccessAdminEndpoint_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/brands", new CreateBrandRequest
        {
            Name = $"AnonBrand_{Guid.NewGuid():N}"[..16]
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_CannotAccessAdminEndpoint_ReturnsForbidden()
    {
        var userToken = await GetUserTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/notes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        request.Content = JsonContent.Create(new CreateNoteRequest { Name = $"UNote_{Guid.NewGuid():N}"[..16] });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAccessAdminEndpoint()
    {
        var adminToken = await GetAdminTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/fragrance-types");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreateFragranceTypeRequest
        {
            Name = $"Type_{Guid.NewGuid():N}"[..16]
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetSeasonsAndFragranceTypes_ReturnOk()
    {
        var seasons = await _client.GetAsync("/api/seasons");
        var types = await _client.GetAsync("/api/fragrance-types");

        Assert.Equal(HttpStatusCode.OK, seasons.StatusCode);
        Assert.Equal(HttpStatusCode.OK, types.StatusCode);

        var seasonBody = await seasons.Content.ReadFromJsonAsync<List<SeasonDto>>();
        var typeBody = await types.Content.ReadFromJsonAsync<List<FragranceTypeDto>>();
        Assert.NotEmpty(seasonBody!);
        Assert.NotEmpty(typeBody!);
    }

    private async Task<PerfumeDetailsDto> CreatePerfumeWithRelationsAsync(string adminToken)
    {
        var brand = await CreateBrandAsync(adminToken, $"PB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"PN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/perfumes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"Perfume_{Guid.NewGuid():N}"[..16],
            BrandId = brand.Id,
            FragranceTypeId = types!.First().Id,
            Description = "Test perfume",
            ReleaseYear = 2020,
            Price = 99.99m,
            Characteristics = new PerfumeCharacteristicsDto
            {
                Sweetness = 40,
                Freshness = 60,
                Woody = 70
            },
            Notes =
            [
                new PerfumeNoteInputDto { NoteId = note.Id, NoteType = NoteTypes.Top }
            ],
            Seasons =
            [
                new PerfumeSeasonInputDto { SeasonId = seasons!.First().Id, Suitability = 80 }
            ]
        });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PerfumeDetailsDto>())!;
    }

    private async Task<BrandDto> CreateBrandAsync(string adminToken, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/brands");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreateBrandRequest { Name = name, Country = "Italy" });

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

    private async Task<string> GetUserTokenAsync()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var username = $"u_{Guid.NewGuid():N}"[..16];
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!",
            FirstName = "Catalog",
            LastName = "User"
        });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        var username = $"a_{Guid.NewGuid():N}"[..16];
        const string password = "Password123!";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            FirstName = "Catalog",
            LastName = "Admin"
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
}

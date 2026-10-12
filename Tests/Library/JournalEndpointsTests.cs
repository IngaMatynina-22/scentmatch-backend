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

public class JournalEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public JournalEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetJournal_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/journal");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndGetJournalEntry_HappyPath()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var create = Authorized(HttpMethod.Post, "/api/journal", token);
        create.Content = JsonContent.Create(new CreateJournalEntryRequest
        {
            PerfumeId = perfume.Id,
            TestedOn = new DateOnly(2026, 10, 1),
            Rating = 8,
            Notes = "Warm opening",
            WearMinutes = 240,
            Tags = ["office", "fall"]
        });

        var createResponse = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JournalEntryDto>();
        Assert.NotNull(created);
        Assert.Equal(perfume.Id, created!.PerfumeId);
        Assert.Equal(8, created.Rating);
        Assert.Equal(240, created.WearMinutes);
        Assert.Contains("office", created.Tags);

        using var get = Authorized(HttpMethod.Get, $"/api/journal/{created.Id}", token);
        var fetched = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<JournalEntryDto>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Warm opening", fetched.Notes);

        using var list = Authorized(HttpMethod.Get, $"/api/journal?perfumeId={perfume.Id}", token);
        var page = await (await _client.SendAsync(list)).Content.ReadFromJsonAsync<PagedResult<JournalEntryDto>>();
        Assert.Contains(page!.Items, x => x.Id == created.Id);
    }

    [Fact]
    public async Task GetAnotherUsersJournalEntry_ReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        using var create = Authorized(HttpMethod.Post, "/api/journal", tokenA);
        create.Content = JsonContent.Create(new CreateJournalEntryRequest
        {
            PerfumeId = perfume.Id,
            TestedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            Rating = 7
        });
        var created = await (await _client.SendAsync(create)).Content.ReadFromJsonAsync<JournalEntryDto>();

        using var get = Authorized(HttpMethod.Get, $"/api/journal/{created!.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(get)).StatusCode);
    }

    [Fact]
    public async Task UpdateAndDeleteOwnJournalEntry_Works()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var create = Authorized(HttpMethod.Post, "/api/journal", token);
        create.Content = JsonContent.Create(new CreateJournalEntryRequest
        {
            PerfumeId = perfume.Id,
            TestedOn = new DateOnly(2026, 9, 1),
            Rating = 5
        });
        var created = await (await _client.SendAsync(create)).Content.ReadFromJsonAsync<JournalEntryDto>();

        using var put = Authorized(HttpMethod.Put, $"/api/journal/{created!.Id}", token);
        put.Content = JsonContent.Create(new UpdateJournalEntryRequest
        {
            TestedOn = new DateOnly(2026, 9, 2),
            Rating = 9,
            Notes = "Updated"
        });
        var updated = await (await _client.SendAsync(put)).Content.ReadFromJsonAsync<JournalEntryDto>();
        Assert.Equal(9, updated!.Rating);
        Assert.Equal("Updated", updated.Notes);

        using var delete = Authorized(HttpMethod.Delete, $"/api/journal/{created.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);

        using var get = Authorized(HttpMethod.Get, $"/api/journal/{created.Id}", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(get)).StatusCode);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"jrnl_{Guid.NewGuid():N}@example.com";
        var username = $"j_{Guid.NewGuid():N}"[..16];

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
        var email = $"jrnl_admin_{Guid.NewGuid():N}@example.com";
        var username = $"ja_{Guid.NewGuid():N}"[..16];
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
        var brand = await CreateBrandAsync(adminToken, $"JB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"JN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"JrnlP_{Guid.NewGuid():N}"[..16],
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

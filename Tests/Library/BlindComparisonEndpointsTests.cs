using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Library;

public class BlindComparisonEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BlindComparisonEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRateCompleteBlindComparison_HappyPath()
    {
        var admin = await GetAdminTokenAsync();
        var perfumeA = await CreatePerfumeAsync(admin);
        var perfumeB = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var create = Authorized(HttpMethod.Post, "/api/blind-comparison", token);
        create.Content = JsonContent.Create(new CreateBlindComparisonRequest
        {
            PerfumeIds = [perfumeA.Id, perfumeB.Id]
        });

        var createResponse = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var session = await createResponse.Content.ReadFromJsonAsync<BlindComparisonSessionDto>();
        Assert.NotNull(session);
        Assert.Equal(BlindComparisonStatuses.Active, session!.Status);
        Assert.Equal(2, session.Slots.Count);
        Assert.All(session.Slots, s => Assert.False(string.IsNullOrWhiteSpace(s.Label)));

        using var getActive = Authorized(HttpMethod.Get, $"/api/blind-comparison/{session.Id}", token);
        var activeBody = await (await _client.SendAsync(getActive)).Content.ReadAsStringAsync();
        Assert.DoesNotContain($"\"perfumeId\"", activeBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(perfumeA.Name, activeBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(perfumeB.Name, activeBody, StringComparison.OrdinalIgnoreCase);

        var label = session.Slots[0].Label;
        using var rate = Authorized(HttpMethod.Put, $"/api/blind-comparison/{session.Id}/ratings", token);
        rate.Content = JsonContent.Create(new BlindComparisonRatingsRequest
        {
            Ratings =
            [
                new BlindComparisonRatingItem { Label = label, Rating = 9, Comment = "Favorite" }
            ]
        });
        var rated = await (await _client.SendAsync(rate)).Content.ReadFromJsonAsync<BlindComparisonSessionDto>();
        Assert.Equal(9, rated!.Slots.First(x => x.Label == label).Rating);

        using var complete = Authorized(HttpMethod.Post, $"/api/blind-comparison/{session.Id}/complete", token);
        complete.Content = JsonContent.Create(new CompleteBlindComparisonRequest { WinnerLabel = label });
        var results = await (await _client.SendAsync(complete)).Content.ReadFromJsonAsync<BlindComparisonResultsDto>();
        Assert.Equal(BlindComparisonStatuses.Completed, results!.Status);
        Assert.Equal(label, results.WinnerLabel);
        Assert.All(results.Slots, s => Assert.True(s.PerfumeId > 0));
        Assert.Contains(results.Slots, s => s.PerfumeId == perfumeA.Id);
        Assert.Contains(results.Slots, s => s.PerfumeId == perfumeB.Id);

        using var getResults = Authorized(HttpMethod.Get, $"/api/blind-comparison/{session.Id}/results", token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getResults)).StatusCode);
    }

    [Fact]
    public async Task GetAnotherUsersBlindSession_ReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfumeA = await CreatePerfumeAsync(admin);
        var perfumeB = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        using var create = Authorized(HttpMethod.Post, "/api/blind-comparison", tokenA);
        create.Content = JsonContent.Create(new CreateBlindComparisonRequest
        {
            PerfumeIds = [perfumeA.Id, perfumeB.Id]
        });
        var session = await (await _client.SendAsync(create)).Content.ReadFromJsonAsync<BlindComparisonSessionDto>();

        using var get = Authorized(HttpMethod.Get, $"/api/blind-comparison/{session!.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(get)).StatusCode);

        using var results = Authorized(HttpMethod.Get, $"/api/blind-comparison/{session.Id}/results", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(results)).StatusCode);
    }

    [Fact]
    public async Task ResultsBeforeComplete_ReturnsForbidden()
    {
        var admin = await GetAdminTokenAsync();
        var perfumeA = await CreatePerfumeAsync(admin);
        var perfumeB = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var create = Authorized(HttpMethod.Post, "/api/blind-comparison", token);
        create.Content = JsonContent.Create(new CreateBlindComparisonRequest
        {
            PerfumeIds = [perfumeA.Id, perfumeB.Id]
        });
        var session = await (await _client.SendAsync(create)).Content.ReadFromJsonAsync<BlindComparisonSessionDto>();

        using var results = Authorized(HttpMethod.Get, $"/api/blind-comparison/{session!.Id}/results", token);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(results)).StatusCode);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"bc_{Guid.NewGuid():N}@example.com";
        var username = $"b_{Guid.NewGuid():N}"[..16];

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
        var email = $"bc_admin_{Guid.NewGuid():N}@example.com";
        var username = $"ba_{Guid.NewGuid():N}"[..16];
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
        var brand = await CreateBrandAsync(adminToken, $"BB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"BN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"BcP_{Guid.NewGuid():N}"[..16],
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

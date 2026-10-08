using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Catalog;

public class PerfumeQueryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PerfumeQueryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_ByPerfumeName_IsCaseInsensitive()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var created = await CreatePerfumeAsync(admin, name: $"Bleu Unique {suffix}", brandName: $"BrandA_{suffix}");

        var response = await _client.GetAsync($"/api/perfumes?search=BLEU%20UNIQUE%20{suffix}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();
        Assert.Contains(body!.Items, x => x.Id == created.Id);
    }

    [Fact]
    public async Task Search_ByBrandName_Works()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var brandName = $"ChanelHouse_{suffix}";
        var created = await CreatePerfumeAsync(admin, name: $"Perfume_{suffix}", brandName: brandName);

        var response = await _client.GetAsync($"/api/perfumes?search=chanelhouse_{suffix}");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Items, x => x.Id == created.Id && x.Brand.Name == brandName);
    }

    [Fact]
    public async Task Filter_ByBrand_FragranceType_Season_AndNotes()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var noteA = await CreateNoteAsync(admin, $"NoteA_{suffix}");
        var noteB = await CreateNoteAsync(admin, $"NoteB_{suffix}");
        var noteC = await CreateNoteAsync(admin, $"NoteC_{suffix}");

        var match = await CreatePerfumeAsync(
            admin,
            name: $"Match_{suffix}",
            brandName: $"BrandMatch_{suffix}",
            noteIds: [noteA.Id, noteB.Id],
            price: 1500,
            rating: 4.5m,
            releaseYear: 2018);

        var other = await CreatePerfumeAsync(
            admin,
            name: $"Other_{suffix}",
            brandName: $"BrandOther_{suffix}",
            noteIds: [noteA.Id, noteC.Id],
            price: 900,
            rating: 3.0m,
            releaseYear: 2010);

        var response = await _client.GetAsync(
            $"/api/perfumes?brandId={match.Brand.Id}&fragranceTypeId={match.FragranceType.Id}&seasonId={match.Seasons.First().SeasonId}&noteIds={noteA.Id},{noteB.Id}");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Items, x => x.Id == match.Id);
        Assert.DoesNotContain(body.Items, x => x.Id == other.Id);
    }

    [Fact]
    public async Task Filter_ByMultipleNotes_UsesAndSemantics()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var note1 = await CreateNoteAsync(admin, $"N1_{suffix}");
        var note2 = await CreateNoteAsync(admin, $"N2_{suffix}");

        var both = await CreatePerfumeAsync(admin, $"Both_{suffix}", $"BBoth_{suffix}", [note1.Id, note2.Id]);
        var one = await CreatePerfumeAsync(admin, $"One_{suffix}", $"BOne_{suffix}", [note1.Id]);

        var response = await _client.GetAsync($"/api/perfumes?noteIds={note1.Id},{note2.Id}");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();

        Assert.Contains(body!.Items, x => x.Id == both.Id);
        Assert.DoesNotContain(body.Items, x => x.Id == one.Id);
    }

    [Fact]
    public async Task Filter_ByPriceRange_AndMinRating()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var inRange = await CreatePerfumeAsync(admin, $"In_{suffix}", $"Bin_{suffix}", price: 2000, rating: 4.2m);
        var cheap = await CreatePerfumeAsync(admin, $"Cheap_{suffix}", $"Bcheap_{suffix}", price: 500, rating: 4.8m);
        var expensive = await CreatePerfumeAsync(admin, $"Exp_{suffix}", $"Bexp_{suffix}", price: 6000, rating: 4.9m);
        var lowRating = await CreatePerfumeAsync(admin, $"Low_{suffix}", $"Blow_{suffix}", price: 2500, rating: 3.1m);

        var response = await _client.GetAsync("/api/perfumes?minPrice=1000&maxPrice=5000&minRating=4");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();

        Assert.Contains(body!.Items, x => x.Id == inRange.Id);
        Assert.DoesNotContain(body.Items, x => x.Id == cheap.Id);
        Assert.DoesNotContain(body.Items, x => x.Id == expensive.Id);
        Assert.DoesNotContain(body.Items, x => x.Id == lowRating.Id);
    }

    [Fact]
    public async Task Sort_ByNameAsc_PriceDesc_RatingDesc_ReleaseYear()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var marker = $"SortMark_{suffix}";

        await CreatePerfumeAsync(admin, $"{marker}_B", $"SB_{suffix}", price: 100, rating: 3, releaseYear: 2015);
        await CreatePerfumeAsync(admin, $"{marker}_A", $"SA_{suffix}", price: 300, rating: 5, releaseYear: 2020);
        await CreatePerfumeAsync(admin, $"{marker}_C", $"SC_{suffix}", price: 200, rating: 4, releaseYear: 2018);

        var byName = await GetPagedAsync($"search={marker}&sortBy=name&sortOrder=asc");
        Assert.Equal(
            new[] { $"{marker}_A", $"{marker}_B", $"{marker}_C" },
            byName.Items.Select(x => x.Name).ToArray());

        var byPriceDesc = await GetPagedAsync($"search={marker}&sortBy=price&sortOrder=desc");
        Assert.True(new decimal?[] { 300m, 200m, 100m }
            .SequenceEqual(byPriceDesc.Items.Select(x => x.Price)));

        var byRatingDesc = await GetPagedAsync($"search={marker}&sortBy=rating&sortOrder=desc");
        Assert.True(new[] { 5m, 4m, 3m }
            .SequenceEqual(byRatingDesc.Items.Select(x => x.Rating)));

        var byYear = await GetPagedAsync($"search={marker}&sortBy=releaseYear&sortOrder=asc");
        Assert.True(new int?[] { 2015, 2018, 2020 }
            .SequenceEqual(byYear.Items.Select(x => x.ReleaseYear)));
    }

    [Fact]
    public async Task Pagination_Defaults_And_Custom_And_BeyondRange()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var marker = $"PageMark_{suffix}";

        for (var i = 0; i < 5; i++)
        {
            await CreatePerfumeAsync(admin, $"{marker}_{i}", $"PB_{suffix}_{i}", price: 100 + i);
        }

        var defaults = await GetPagedAsync($"search={marker}");
        Assert.Equal(1, defaults.Page);
        Assert.Equal(20, defaults.PageSize);
        Assert.Equal(5, defaults.TotalItems);
        Assert.Equal(1, defaults.TotalPages);
        Assert.Equal(5, defaults.Items.Count);

        var page1 = await GetPagedAsync($"search={marker}&page=1&pageSize=2&sortBy=name&sortOrder=asc");
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page1.TotalItems);
        Assert.Equal(3, page1.TotalPages);

        var page3 = await GetPagedAsync($"search={marker}&page=3&pageSize=2&sortBy=name&sortOrder=asc");
        Assert.Single(page3.Items);
        Assert.Equal(5, page3.TotalItems);
        Assert.Equal(3, page3.TotalPages);

        var beyond = await GetPagedAsync($"search={marker}&page=10&pageSize=2");
        Assert.Empty(beyond.Items);
        Assert.Equal(5, beyond.TotalItems);
        Assert.Equal(3, beyond.TotalPages);
        Assert.Equal(10, beyond.Page);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=1000")]
    [InlineData("minPrice=-1")]
    [InlineData("maxPrice=-5")]
    [InlineData("minPrice=5000&maxPrice=1000")]
    [InlineData("minRating=-1")]
    [InlineData("minRating=6")]
    [InlineData("sortBy=foobar")]
    [InlineData("sortOrder=sideways")]
    public async Task Validation_InvalidQuery_ReturnsBadRequest(string query)
    {
        var response = await _client.GetAsync($"/api/perfumes?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Combination_FilterSortPagination_Works()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var marker = $"Combo_{suffix}";
        var note = await CreateNoteAsync(admin, $"ComboNote_{suffix}");

        await CreatePerfumeAsync(admin, $"{marker}_1", $"CB1_{suffix}", [note.Id], price: 1200, rating: 4.1m);
        await CreatePerfumeAsync(admin, $"{marker}_2", $"CB2_{suffix}", [note.Id], price: 1800, rating: 4.8m);
        await CreatePerfumeAsync(admin, $"{marker}_3", $"CB3_{suffix}", [note.Id], price: 2200, rating: 4.5m);
        await CreatePerfumeAsync(admin, $"{marker}_out", $"CBout_{suffix}", [note.Id], price: 800, rating: 4.9m);

        var response = await _client.GetAsync(
            $"/api/perfumes?search={marker}&noteIds={note.Id}&minPrice=1000&maxPrice=2500&minRating=4&sortBy=rating&sortOrder=desc&page=1&pageSize=2");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, body!.TotalItems);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(2, body.TotalPages);
        Assert.Equal(4.8m, body.Items[0].Rating);
        Assert.Equal(4.5m, body.Items[1].Rating);
        Assert.DoesNotContain(body.Items, x => x.Name.Contains("_out"));
    }

    [Fact]
    public async Task GetById_StillWorks_And_InactiveNotInList()
    {
        var admin = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var perfume = await CreatePerfumeAsync(admin, $"Active_{suffix}", $"BAct_{suffix}");

        var details = await _client.GetAsync($"/api/perfumes/{perfume.Id}");
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);

        using (var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/perfumes/{perfume.Id}"))
        {
            deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            var deleteResponse = await _client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        var list = await GetPagedAsync($"search=Active_{suffix}");
        Assert.DoesNotContain(list.Items, x => x.Id == perfume.Id);

        var missing = await _client.GetAsync($"/api/perfumes/{perfume.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private async Task<PagedResult<PerfumeListItemDto>> GetPagedAsync(string query)
    {
        var response = await _client.GetAsync($"/api/perfumes?{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<PerfumeListItemDto>>())!;
    }

    private async Task<PerfumeDetailsDto> CreatePerfumeAsync(
        string adminToken,
        string name,
        string brandName,
        IReadOnlyList<int>? noteIds = null,
        decimal? price = 100,
        decimal rating = 0,
        int releaseYear = 2020)
    {
        var brand = await CreateBrandAsync(adminToken, brandName);
        noteIds ??= [(await CreateNoteAsync(adminToken, $"AutoNote_{Guid.NewGuid():N}"[..16])).Id];

        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/perfumes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = name,
            BrandId = brand.Id,
            FragranceTypeId = types!.First().Id,
            ReleaseYear = releaseYear,
            Price = price,
            Notes = noteIds.Select(id => new PerfumeNoteInputDto
            {
                NoteId = id,
                NoteType = NoteTypes.Top
            }).ToList(),
            Seasons =
            [
                new PerfumeSeasonInputDto
                {
                    SeasonId = seasons!.First().Id,
                    Suitability = 70
                }
            ]
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<PerfumeDetailsDto>())!;

        if (rating > 0)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            var entity = await db.Perfumes.FirstAsync(x => x.Id == created.Id);
            entity.Rating = rating;
            await db.SaveChangesAsync();

            var refreshed = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{created.Id}");
            return refreshed!;
        }

        return created;
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
        request.Content = JsonContent.Create(new CreateNoteRequest { Name = name });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NoteDto>())!;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var email = $"qadmin_{Guid.NewGuid():N}@example.com";
        var username = $"qa_{Guid.NewGuid():N}"[..16];
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

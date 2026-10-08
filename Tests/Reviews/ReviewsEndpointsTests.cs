using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Reviews.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Reviews;

public class ReviewsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReviewsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetReviews_Anonymous_ReturnsOk()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);

        var response = await _client.GetAsync($"/api/perfumes/{perfume.Id}/reviews");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReviewDto>>();
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
        Assert.Equal(0, body.TotalItems);
    }

    [Fact]
    public async Task GetReviews_NonexistentOrInactivePerfume_ReturnsNotFound()
    {
        var missing = await _client.GetAsync("/api/perfumes/999999/reviews");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        using (var delete = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{perfume.Id}", admin))
        {
            Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);
        }

        var inactive = await _client.GetAsync($"/api/perfumes/{perfume.Id}/reviews");
        Assert.Equal(HttpStatusCode.NotFound, inactive.StatusCode);
    }

    [Fact]
    public async Task GetReviews_OnlyApprovedAreVisible_AndPaginationWorks()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);

        for (var i = 0; i < 5; i++)
        {
            var token = await RegisterAsync();
            var review = await CreateReviewAsync(token, perfume.Id, rating: 5);
            if (i < 3)
            {
                await ApproveAsync(admin, review.Id);
            }
        }

        var page1 = await _client.GetFromJsonAsync<PagedResult<ReviewDto>>(
            $"/api/perfumes/{perfume.Id}/reviews?page=1&pageSize=2");

        Assert.Equal(3, page1!.TotalItems);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);

        var page2 = await _client.GetFromJsonAsync<PagedResult<ReviewDto>>(
            $"/api/perfumes/{perfume.Id}/reviews?page=2&pageSize=2");
        Assert.Single(page2!.Items);
    }

    [Fact]
    public async Task CreateReview_Authenticated_StartsAsPending()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Post, $"/api/perfumes/{perfume.Id}/reviews", token);
        request.Content = JsonContent.Create(new CreateReviewRequest
        {
            Rating = 5,
            Text = "Excellent fragrance"
        });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<OwnReviewDto>();
        Assert.Equal(ReviewStatuses.Pending, created!.Status);
        Assert.Equal(5, created.Rating);
        Assert.Equal("Excellent fragrance", created.Text);

        using var my = Authorized(HttpMethod.Get, $"/api/perfumes/{perfume.Id}/my-review", token);
        var own = await (await _client.SendAsync(my)).Content.ReadFromJsonAsync<OwnReviewDto>();
        Assert.Equal(created.Id, own!.Id);
        Assert.Equal(ReviewStatuses.Pending, own.Status);
    }

    [Fact]
    public async Task CreateReview_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/perfumes/1/reviews", new CreateReviewRequest
        {
            Rating = 5
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public async Task CreateReview_InvalidRating_ReturnsBadRequest(int rating)
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Post, $"/api/perfumes/{perfume.Id}/reviews", token);
        request.Content = JsonContent.Create(new CreateReviewRequest { Rating = rating });

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task CreateReview_Duplicate_ReturnsConflict()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        await CreateReviewAsync(token, perfume.Id);

        using var duplicate = Authorized(HttpMethod.Post, $"/api/perfumes/{perfume.Id}/reviews", token);
        duplicate.Content = JsonContent.Create(new CreateReviewRequest { Rating = 4 });
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(duplicate)).StatusCode);
    }

    [Fact]
    public async Task CreateReview_NonexistentOrInactivePerfume_ReturnsNotFound()
    {
        var token = await RegisterAsync();

        using var missing = Authorized(HttpMethod.Post, "/api/perfumes/999999/reviews", token);
        missing.Content = JsonContent.Create(new CreateReviewRequest { Rating = 4 });
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);

        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        using (var delete = Authorized(HttpMethod.Delete, $"/api/admin/perfumes/{perfume.Id}", admin))
        {
            (await _client.SendAsync(delete)).EnsureSuccessStatusCode();
        }

        using var inactive = Authorized(HttpMethod.Post, $"/api/perfumes/{perfume.Id}/reviews", token);
        inactive.Content = JsonContent.Create(new CreateReviewRequest { Rating = 4 });
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(inactive)).StatusCode);
    }

    [Fact]
    public async Task UserCanUpdateAndDeleteOwnReview_ButNotOthers()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var tokenA = await RegisterAsync();
        var tokenB = await RegisterAsync();

        var reviewA = await CreateReviewAsync(tokenA, perfume.Id, rating: 5, text: "A");
        await ApproveAsync(admin, reviewA.Id);

        using var updateB = Authorized(HttpMethod.Put, $"/api/reviews/{reviewA.Id}", tokenB);
        updateB.Content = JsonContent.Create(new UpdateReviewRequest { Rating = 1, Text = "Hijack" });
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(updateB)).StatusCode);

        using var updateA = Authorized(HttpMethod.Put, $"/api/reviews/{reviewA.Id}", tokenA);
        updateA.Content = JsonContent.Create(new UpdateReviewRequest { Rating = 4, Text = "Updated" });
        var updateResponse = await _client.SendAsync(updateA);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<OwnReviewDto>();
        Assert.Equal(4, updated!.Rating);
        Assert.Equal("Updated", updated.Text);
        Assert.Equal(ReviewStatuses.Pending, updated.Status);

        var perfumeAfterEdit = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{perfume.Id}");
        Assert.Equal(0, perfumeAfterEdit!.ReviewsCount);
        Assert.Equal(0m, perfumeAfterEdit.Rating);

        using var deleteB = Authorized(HttpMethod.Delete, $"/api/reviews/{reviewA.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(deleteB)).StatusCode);

        using var deleteA = Authorized(HttpMethod.Delete, $"/api/reviews/{reviewA.Id}", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteA)).StatusCode);
    }

    [Fact]
    public async Task RatingCalculation_ApprovedOnly_WithRoundingAndReset()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);

        var r1 = await CreateReviewAsync(await RegisterAsync(), perfume.Id, rating: 5);
        var r2 = await CreateReviewAsync(await RegisterAsync(), perfume.Id, rating: 4);
        var r3 = await CreateReviewAsync(await RegisterAsync(), perfume.Id, rating: 3);
        var pending = await CreateReviewAsync(await RegisterAsync(), perfume.Id, rating: 1);

        var before = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{perfume.Id}");
        Assert.Equal(0, before!.ReviewsCount);
        Assert.Equal(0m, before.Rating);

        await ApproveAsync(admin, r1.Id);
        await ApproveAsync(admin, r2.Id);
        await ApproveAsync(admin, r3.Id);

        // Reject one pending to ensure it still does not count
        await RejectAsync(admin, pending.Id);

        var afterApprove = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{perfume.Id}");
        Assert.Equal(3, afterApprove!.ReviewsCount);
        Assert.Equal(4.00m, afterApprove.Rating);

        // Average 5,4,2 = 3.666... → 3.67
        var r4 = await CreateReviewAsync(await RegisterAsync(), perfume.Id, rating: 2);
        await ApproveAsync(admin, r4.Id);

        var withFour = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{perfume.Id}");
        Assert.Equal(4, withFour!.ReviewsCount);
        Assert.Equal(3.50m, withFour.Rating);

        await RejectAsync(admin, r1.Id);
        var afterReject = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{perfume.Id}");
        Assert.Equal(3, afterReject!.ReviewsCount);
        Assert.Equal(3.00m, afterReject.Rating); // 4,3,2

        using var delete = Authorized(HttpMethod.Delete, $"/api/admin/reviews/{r2.Id}", admin);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);

        using var delete2 = Authorized(HttpMethod.Delete, $"/api/admin/reviews/{r3.Id}", admin);
        (await _client.SendAsync(delete2)).EnsureSuccessStatusCode();
        using var delete3 = Authorized(HttpMethod.Delete, $"/api/admin/reviews/{r4.Id}", admin);
        (await _client.SendAsync(delete3)).EnsureSuccessStatusCode();

        var empty = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{perfume.Id}");
        Assert.Equal(0, empty!.ReviewsCount);
        Assert.Equal(0m, empty.Rating);
    }

    [Fact]
    public async Task RatingAverage_RoundsToTwoDecimalPlaces()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);

        // 5, 5, 4 → 4.666... → 4.67
        foreach (var rating in new[] { 5, 5, 4 })
        {
            var review = await CreateReviewAsync(await RegisterAsync(), perfume.Id, rating);
            await ApproveAsync(admin, review.Id);
        }

        var details = await _client.GetFromJsonAsync<PerfumeDetailsDto>($"/api/perfumes/{perfume.Id}");
        Assert.Equal(3, details!.ReviewsCount);
        Assert.Equal(4.67m, details.Rating);
    }

    [Fact]
    public async Task AdminModerationEndpoints_AuthorizationAndBehavior()
    {
        var admin = await GetAdminTokenAsync();
        var user = await RegisterAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var review = await CreateReviewAsync(user, perfume.Id, rating: 5, text: "Pending one");

        var anonymous = await _client.GetAsync("/api/admin/reviews");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var forbidden = Authorized(HttpMethod.Get, "/api/admin/reviews", user);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(forbidden)).StatusCode);

        using var list = Authorized(HttpMethod.Get, "/api/admin/reviews?status=Pending", admin);
        var pendingList = await (await _client.SendAsync(list)).Content.ReadFromJsonAsync<PagedResult<AdminReviewDto>>();
        Assert.Contains(pendingList!.Items, x => x.Id == review.Id);
        Assert.All(pendingList.Items, x => Assert.Equal(ReviewStatuses.Pending, x.Status));

        using var badStatus = Authorized(HttpMethod.Get, "/api/admin/reviews?status=Unknown", admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(badStatus)).StatusCode);

        var approved = await ApproveAsync(admin, review.Id);
        Assert.Equal(ReviewStatuses.Approved, approved.Status);

        // idempotent approve
        var approvedAgain = await ApproveAsync(admin, review.Id);
        Assert.Equal(ReviewStatuses.Approved, approvedAgain.Status);

        var rejected = await RejectAsync(admin, review.Id);
        Assert.Equal(ReviewStatuses.Rejected, rejected.Status);

        using var delete = Authorized(HttpMethod.Delete, $"/api/admin/reviews/{review.Id}", admin);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);

        using var missing = Authorized(HttpMethod.Put, "/api/admin/reviews/999999/approve", admin);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);
    }

    [Fact]
    public async Task GetMyReview_Missing_ReturnsNotFound()
    {
        var admin = await GetAdminTokenAsync();
        var perfume = await CreatePerfumeAsync(admin);
        var token = await RegisterAsync();

        using var request = Authorized(HttpMethod.Get, $"/api/perfumes/{perfume.Id}/my-review", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(request)).StatusCode);
    }

    private async Task<OwnReviewDto> CreateReviewAsync(
        string token,
        int perfumeId,
        int rating = 5,
        string? text = null)
    {
        using var request = Authorized(HttpMethod.Post, $"/api/perfumes/{perfumeId}/reviews", token);
        request.Content = JsonContent.Create(new CreateReviewRequest
        {
            Rating = rating,
            Text = text
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OwnReviewDto>())!;
    }

    private async Task<AdminReviewDto> ApproveAsync(string adminToken, int reviewId)
    {
        using var request = Authorized(HttpMethod.Put, $"/api/admin/reviews/{reviewId}/approve", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AdminReviewDto>())!;
    }

    private async Task<AdminReviewDto> RejectAsync(string adminToken, int reviewId)
    {
        using var request = Authorized(HttpMethod.Put, $"/api/admin/reviews/{reviewId}/reject", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AdminReviewDto>())!;
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"rev_{Guid.NewGuid():N}@example.com";
        var username = $"r_{Guid.NewGuid():N}"[..16];

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
        var email = $"rev_admin_{Guid.NewGuid():N}@example.com";
        var username = $"ra_{Guid.NewGuid():N}"[..16];
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
        var brand = await CreateBrandAsync(adminToken, $"RB_{Guid.NewGuid():N}"[..12]);
        var note = await CreateNoteAsync(adminToken, $"RN_{Guid.NewGuid():N}"[..12]);
        var seasons = await _client.GetFromJsonAsync<List<SeasonDto>>("/api/seasons");
        var types = await _client.GetFromJsonAsync<List<FragranceTypeDto>>("/api/fragrance-types");

        using var request = Authorized(HttpMethod.Post, "/api/admin/perfumes", adminToken);
        request.Content = JsonContent.Create(new CreatePerfumeRequest
        {
            Name = $"RevP_{Guid.NewGuid():N}"[..16],
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

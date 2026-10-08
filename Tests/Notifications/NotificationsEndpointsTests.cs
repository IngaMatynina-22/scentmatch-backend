using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.DTOs;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Notifications.DTOs;
using ScentMatch.Application.Notifications.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Tests.Infrastructure;

namespace ScentMatch.Tests.Notifications;

public class NotificationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Anonymous_Access_ReturnsUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/notifications/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PutAsync("/api/notifications/1/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PutAsync("/api/notifications/read-all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync("/api/notifications/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync("/api/notifications")).StatusCode);
    }

    [Fact]
    public async Task List_SortedDesc_WithPagination_AndOwnership()
    {
        var (tokenA, userIdA) = await RegisterAsync();
        var (tokenB, userIdB) = await RegisterAsync();

        for (var i = 0; i < 5; i++)
        {
            await CreateNotificationAsync(userIdA, $"A{i}", $"Message A{i}");
            await Task.Delay(15);
        }

        await CreateNotificationAsync(userIdB, "B only", "Private");

        using var page1 = Authorized(HttpMethod.Get, "/api/notifications?page=1&pageSize=2", tokenA);
        var body = await (await _client.SendAsync(page1)).Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();

        Assert.Equal(5, body!.TotalItems);
        Assert.Equal(3, body.TotalPages);
        Assert.Equal(2, body.Items.Count);
        Assert.True(body.Items[0].CreatedAt >= body.Items[1].CreatedAt);
        Assert.All(body.Items, x => Assert.StartsWith("A", x.Title));

        using var listB = Authorized(HttpMethod.Get, "/api/notifications", tokenB);
        var bodyB = await (await _client.SendAsync(listB)).Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        Assert.Equal(1, bodyB!.TotalItems);
        Assert.Equal("B only", bodyB.Items[0].Title);
    }

    [Fact]
    public async Task UnreadCount_IsCorrect_AndScopedToUser()
    {
        var (tokenA, userIdA) = await RegisterAsync();
        var (tokenB, userIdB) = await RegisterAsync();

        var n1 = await CreateNotificationAsync(userIdA, "U1", "m1");
        await CreateNotificationAsync(userIdA, "U2", "m2");
        await CreateNotificationAsync(userIdB, "BU", "bm");

        using var countA = Authorized(HttpMethod.Get, "/api/notifications/unread-count", tokenA);
        var unreadA = await (await _client.SendAsync(countA)).Content.ReadFromJsonAsync<UnreadNotificationCountDto>();
        Assert.Equal(2, unreadA!.Count);

        using var read = Authorized(HttpMethod.Put, $"/api/notifications/{n1.Id}/read", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(read)).StatusCode);

        using var countA2 = Authorized(HttpMethod.Get, "/api/notifications/unread-count", tokenA);
        Assert.Equal(1, (await (await _client.SendAsync(countA2)).Content.ReadFromJsonAsync<UnreadNotificationCountDto>())!.Count);

        using var countB = Authorized(HttpMethod.Get, "/api/notifications/unread-count", tokenB);
        Assert.Equal(1, (await (await _client.SendAsync(countB)).Content.ReadFromJsonAsync<UnreadNotificationCountDto>())!.Count);
    }

    [Fact]
    public async Task MarkRead_IsIdempotent_AndForeignReturnsNotFound()
    {
        var (tokenA, userIdA) = await RegisterAsync();
        var (tokenB, _) = await RegisterAsync();
        var notification = await CreateNotificationAsync(userIdA, "Read me", "msg");

        using var foreign = Authorized(HttpMethod.Put, $"/api/notifications/{notification.Id}/read", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(foreign)).StatusCode);

        using var read1 = Authorized(HttpMethod.Put, $"/api/notifications/{notification.Id}/read", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(read1)).StatusCode);

        using var read2 = Authorized(HttpMethod.Put, $"/api/notifications/{notification.Id}/read", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(read2)).StatusCode);

        using var get = Authorized(HttpMethod.Get, "/api/notifications", tokenA);
        var list = await (await _client.SendAsync(get)).Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        Assert.True(list!.Items.Single(x => x.Id == notification.Id).IsRead);

        using var missing = Authorized(HttpMethod.Put, "/api/notifications/999999/read", tokenA);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);
    }

    [Fact]
    public async Task MarkAllAsRead_AffectsOnlyCurrentUser()
    {
        var (tokenA, userIdA) = await RegisterAsync();
        var (tokenB, userIdB) = await RegisterAsync();

        await CreateNotificationAsync(userIdA, "A1", "m");
        await CreateNotificationAsync(userIdA, "A2", "m");
        await CreateNotificationAsync(userIdB, "B1", "m");

        using var readAll = Authorized(HttpMethod.Put, "/api/notifications/read-all", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(readAll)).StatusCode);

        using var readAllAgain = Authorized(HttpMethod.Put, "/api/notifications/read-all", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(readAllAgain)).StatusCode);

        using var countA = Authorized(HttpMethod.Get, "/api/notifications/unread-count", tokenA);
        Assert.Equal(0, (await (await _client.SendAsync(countA)).Content.ReadFromJsonAsync<UnreadNotificationCountDto>())!.Count);

        using var countB = Authorized(HttpMethod.Get, "/api/notifications/unread-count", tokenB);
        Assert.Equal(1, (await (await _client.SendAsync(countB)).Content.ReadFromJsonAsync<UnreadNotificationCountDto>())!.Count);
    }

    [Fact]
    public async Task Delete_OwnAndClear_RespectOwnership()
    {
        var (tokenA, userIdA) = await RegisterAsync();
        var (tokenB, userIdB) = await RegisterAsync();

        var a1 = await CreateNotificationAsync(userIdA, "Del1", "m");
        await CreateNotificationAsync(userIdA, "Del2", "m");
        var b1 = await CreateNotificationAsync(userIdB, "Keep", "m");

        using var foreignDelete = Authorized(HttpMethod.Delete, $"/api/notifications/{a1.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(foreignDelete)).StatusCode);

        using var deleteOwn = Authorized(HttpMethod.Delete, $"/api/notifications/{a1.Id}", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteOwn)).StatusCode);

        using var deleteMissing = Authorized(HttpMethod.Delete, "/api/notifications/999999", tokenA);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(deleteMissing)).StatusCode);

        using var clearA = Authorized(HttpMethod.Delete, "/api/notifications", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(clearA)).StatusCode);

        using var clearEmpty = Authorized(HttpMethod.Delete, "/api/notifications", tokenA);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(clearEmpty)).StatusCode);

        using var listA = Authorized(HttpMethod.Get, "/api/notifications", tokenA);
        Assert.Empty((await (await _client.SendAsync(listA)).Content.ReadFromJsonAsync<PagedResult<NotificationDto>>())!.Items);

        using var listB = Authorized(HttpMethod.Get, "/api/notifications", tokenB);
        var bodyB = await (await _client.SendAsync(listB)).Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        Assert.Single(bodyB!.Items);
        Assert.Equal(b1.Id, bodyB.Items[0].Id);
    }

    [Fact]
    public async Task CreateAsync_StoresUtcAndValidatesInput()
    {
        var (_, userId) = await RegisterAsync();

        var before = DateTime.UtcNow.AddSeconds(-5);
        var created = await CreateNotificationAsync(userId, " Created ", " Hello ");
        var after = DateTime.UtcNow.AddSeconds(5);

        Assert.Equal(NotificationTypes.Info, created.Type);
        Assert.Equal("Created", created.Title);
        Assert.Equal("Hello", created.Message);
        Assert.False(created.IsRead);
        Assert.NotNull(created.CreatedAt);
        Assert.True(created.CreatedAt >= before && created.CreatedAt <= after);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt!.Value.Kind);

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await Assert.ThrowsAsync<Application.Common.Exceptions.BadRequestException>(() =>
            service.CreateAsync(userId, new CreateNotificationRequest
            {
                Type = "Unknown",
                Title = "T",
                Message = "M"
            }));

        await Assert.ThrowsAsync<Application.Common.Exceptions.BadRequestException>(() =>
            service.CreateAsync(userId, new CreateNotificationRequest
            {
                Type = NotificationTypes.System,
                Title = "   ",
                Message = "M"
            }));
    }

    private async Task<NotificationDto> CreateNotificationAsync(int userId, string title, string message)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        return await service.CreateAsync(userId, new CreateNotificationRequest
        {
            Type = NotificationTypes.Info,
            Title = title,
            Message = message
        });
    }

    private async Task<(string Token, int UserId)> RegisterAsync()
    {
        var email = $"notif_{Guid.NewGuid():N}@example.com";
        var username = $"n_{Guid.NewGuid():N}"[..16];

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        using var me = Authorized(HttpMethod.Get, "/api/auth/me", body!.AccessToken);
        var meResponse = await _client.SendAsync(me);
        meResponse.EnsureSuccessStatusCode();
        var user = await meResponse.Content.ReadFromJsonAsync<UserResponse>();

        return (body.AccessToken, user!.Id);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

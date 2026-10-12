using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Analytics.DTOs;
using ScentMatch.Application.Analytics.Interfaces;
using ScentMatch.Application.Common.Exceptions;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IPreferenceAnalyticsService _preferenceAnalyticsService;

    public AnalyticsController(IPreferenceAnalyticsService preferenceAnalyticsService)
    {
        _preferenceAnalyticsService = preferenceAnalyticsService;
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(PreferenceAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PreferenceAnalyticsDto>> GetPreferences(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _preferenceAnalyticsService.GetPreferencesAsync(userId, cancellationToken));
    }

    [HttpPost("personalization/reset")]
    [ProducesResponseType(typeof(ResetPersonalizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResetPersonalizationResponse>> ResetPersonalization(
        [FromBody] ResetPersonalizationRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _preferenceAnalyticsService.ResetPersonalizationAsync(
            userId,
            request ?? new ResetPersonalizationRequest(),
            cancellationToken));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("User identity is invalid.");
        }

        return userId;
    }
}

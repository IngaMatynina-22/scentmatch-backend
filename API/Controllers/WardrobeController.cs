using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/wardrobe")]
[Authorize]
public class WardrobeController : ControllerBase
{
    private readonly IWardrobeService _wardrobeService;

    public WardrobeController(IWardrobeService wardrobeService)
    {
        _wardrobeService = wardrobeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(WardrobeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WardrobeResponse>> GetWardrobe(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _wardrobeService.GetWardrobeAsync(userId, cancellationToken));
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(WardrobeStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WardrobeStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _wardrobeService.GetStatsAsync(userId, cancellationToken));
    }

    [HttpPut("assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        [FromBody] AssignWardrobeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _wardrobeService.AssignAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{scenarioKey}/perfumes/{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePerfume(
        string scenarioKey,
        int perfumeId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _wardrobeService.RemovePerfumeAsync(userId, scenarioKey, perfumeId, cancellationToken);
        return NoContent();
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

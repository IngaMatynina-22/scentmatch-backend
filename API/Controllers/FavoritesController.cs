using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favoritesService;

    public FavoritesController(IFavoritesService favoritesService)
    {
        _favoritesService = favoritesService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PerfumeListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PerfumeListItemDto>>> GetFavorites(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _favoritesService.GetFavoritesAsync(userId, pagination, cancellationToken));
    }

    [HttpPost("{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavorite(int perfumeId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _favoritesService.AddFavoriteAsync(userId, perfumeId, cancellationToken);
        return Created($"/api/favorites/{perfumeId}", null);
    }

    [HttpDelete("{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(int perfumeId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _favoritesService.RemoveFavoriteAsync(userId, perfumeId, cancellationToken);
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

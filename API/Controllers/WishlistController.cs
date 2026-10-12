using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WishlistItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WishlistItemDto>>> GetItems(
        [FromQuery] WishlistQueryParameters query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _wishlistService.GetItemsAsync(userId, query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishlistItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _wishlistService.GetByIdAsync(userId, id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishlistItemDto>> Add(
        [FromBody] CreateWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var item = await _wishlistService.AddAsync(userId, request, cancellationToken);
        return Created($"/api/wishlist/{item.Id}", item);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishlistItemDto>> Update(
        int id,
        [FromBody] UpdateWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _wishlistService.UpdateAsync(userId, id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _wishlistService.DeleteAsync(userId, id, cancellationToken);
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

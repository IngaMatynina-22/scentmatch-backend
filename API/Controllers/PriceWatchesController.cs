using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.PriceWatches.DTOs;
using ScentMatch.Application.PriceWatches.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/price-watches")]
[Authorize]
public class PriceWatchesController : ControllerBase
{
    private readonly IPriceWatchService _priceWatchService;

    public PriceWatchesController(IPriceWatchService priceWatchService)
    {
        _priceWatchService = priceWatchService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PriceWatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceWatchDto>> Create(
        [FromBody] CreatePriceWatchRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _priceWatchService.CreateAsync(GetCurrentUserId(), request, cancellationToken);
        return Created($"/api/price-watches/{created.Id}", created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PriceWatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PriceWatchDto>>> List(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        return Ok(await _priceWatchService.ListAsync(GetCurrentUserId(), pagination, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PriceWatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceWatchDto>> Get(int id, CancellationToken cancellationToken)
    {
        return Ok(await _priceWatchService.GetAsync(GetCurrentUserId(), id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PriceWatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceWatchDto>> Update(
        int id,
        [FromBody] UpdatePriceWatchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _priceWatchService.UpdateAsync(GetCurrentUserId(), id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _priceWatchService.DeleteAsync(GetCurrentUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<PriceSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PriceSnapshotDto>>> History(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await _priceWatchService.GetHistoryAsync(GetCurrentUserId(), id, cancellationToken));
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

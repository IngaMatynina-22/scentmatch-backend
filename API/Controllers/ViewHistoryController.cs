using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Application.History.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/history/views")]
[Authorize]
public class ViewHistoryController : ControllerBase
{
    private readonly IViewHistoryService _viewHistoryService;

    public ViewHistoryController(IViewHistoryService viewHistoryService)
    {
        _viewHistoryService = viewHistoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ViewHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ViewHistoryDto>>> Get(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _viewHistoryService.GetAsync(userId, pagination, cancellationToken));
    }

    [HttpPost("{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterView(int perfumeId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _viewHistoryService.RegisterViewAsync(userId, perfumeId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _viewHistoryService.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _viewHistoryService.ClearAsync(userId, cancellationToken);
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

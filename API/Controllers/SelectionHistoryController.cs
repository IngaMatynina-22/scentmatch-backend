using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Application.History.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/history/selections")]
[Authorize]
public class SelectionHistoryController : ControllerBase
{
    private readonly ISelectionHistoryService _selectionHistoryService;

    public SelectionHistoryController(ISelectionHistoryService selectionHistoryService)
    {
        _selectionHistoryService = selectionHistoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SelectionHistoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelectionHistoryDto>> Create(
        [FromBody] CreateSelectionHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var created = await _selectionHistoryService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/history/selections/{created.Id}", created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SelectionHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SelectionHistoryDto>>> Get(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _selectionHistoryService.GetAsync(userId, pagination, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SelectionHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SelectionHistoryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _selectionHistoryService.GetByIdAsync(userId, id, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _selectionHistoryService.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _selectionHistoryService.ClearAsync(userId, cancellationToken);
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

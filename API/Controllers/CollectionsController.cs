using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/collections")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionsService _collectionsService;

    public CollectionsController(ICollectionsService collectionsService)
    {
        _collectionsService = collectionsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollections(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _collectionsService.GetCollectionsAsync(userId, pagination, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CollectionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _collectionsService.GetByIdAsync(userId, id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionDto>> Create(
        [FromBody] CreateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var created = await _collectionsService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/collections/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDto>> Update(
        int id,
        [FromBody] UpdateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _collectionsService.UpdateAsync(userId, id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _collectionsService.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{collectionId:int}/perfumes/{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPerfume(
        int collectionId,
        int perfumeId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _collectionsService.AddPerfumeAsync(userId, collectionId, perfumeId, cancellationToken);
        return Created($"/api/collections/{collectionId}/perfumes/{perfumeId}", null);
    }

    [HttpDelete("{collectionId:int}/perfumes/{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePerfume(
        int collectionId,
        int perfumeId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _collectionsService.RemovePerfumeAsync(userId, collectionId, perfumeId, cancellationToken);
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

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.Interfaces;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/perfumes")]
[AllowAnonymous]
public class PerfumesController : ControllerBase
{
    private readonly IPerfumeService _perfumeService;
    private readonly IViewHistoryService _viewHistoryService;
    private readonly ISimilarityService _similarityService;

    public PerfumesController(
        IPerfumeService perfumeService,
        IViewHistoryService viewHistoryService,
        ISimilarityService similarityService)
    {
        _perfumeService = perfumeService;
        _viewHistoryService = viewHistoryService;
        _similarityService = similarityService;
    }

    /// <summary>
    /// Get paginated perfume catalog with search, filters and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PerfumeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PerfumeListItemDto>>> GetAll(
        [FromQuery] PerfumeQueryParameters query,
        CancellationToken cancellationToken)
    {
        return Ok(await _perfumeService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PerfumeDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfumeDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var details = await _perfumeService.GetByIdAsync(id, cancellationToken);

        if (TryGetCurrentUserId(out var userId))
        {
            await _viewHistoryService.RegisterViewAsync(userId, id, cancellationToken);
        }

        return Ok(details);
    }

    /// <summary>
    /// Similar perfumes ranked by shared scoring (notes, type, seasons, characteristics, intensity, price).
    /// Use <c>limit</c> for a simple top-N list, or <c>page</c>/<c>pageSize</c> for pagination.
    /// </summary>
    [HttpGet("{id:int}/similar")]
    [ProducesResponseType(typeof(PagedResult<SimilarPerfumeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<SimilarPerfumeDto>>> GetSimilar(
        int id,
        [FromQuery] SimilarPerfumesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _similarityService.GetSimilarAsync(id, query, cancellationToken));
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(userIdClaim, out userId);
    }
}

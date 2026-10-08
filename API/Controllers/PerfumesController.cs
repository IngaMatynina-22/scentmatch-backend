using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/perfumes")]
[AllowAnonymous]
public class PerfumesController : ControllerBase
{
    private readonly IPerfumeService _perfumeService;

    public PerfumesController(IPerfumeService perfumeService)
    {
        _perfumeService = perfumeService;
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
    public async Task<ActionResult<PerfumeDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _perfumeService.GetByIdAsync(id, cancellationToken));
    }
}

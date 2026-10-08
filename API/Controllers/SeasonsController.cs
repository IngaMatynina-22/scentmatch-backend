using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/seasons")]
[AllowAnonymous]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonService _seasonService;

    public SeasonsController(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SeasonDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _seasonService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SeasonDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _seasonService.GetByIdAsync(id, cancellationToken));
    }
}

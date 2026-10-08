using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/fragrance-types")]
[AllowAnonymous]
public class FragranceTypesController : ControllerBase
{
    private readonly IFragranceTypeService _fragranceTypeService;

    public FragranceTypesController(IFragranceTypeService fragranceTypeService)
    {
        _fragranceTypeService = fragranceTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FragranceTypeDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _fragranceTypeService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FragranceTypeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _fragranceTypeService.GetByIdAsync(id, cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/brands")]
[AllowAnonymous]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _brandService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BrandDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _brandService.GetByIdAsync(id, cancellationToken));
    }
}

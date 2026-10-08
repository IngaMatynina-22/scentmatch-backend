using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/brands")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminBrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public AdminBrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpPost]
    public async Task<ActionResult<BrandDto>> Create(
        [FromBody] CreateBrandRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _brandService.CreateAsync(request, cancellationToken);
        return Created($"/api/brands/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BrandDto>> Update(
        int id,
        [FromBody] UpdateBrandRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _brandService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _brandService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

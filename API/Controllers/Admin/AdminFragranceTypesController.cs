using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/fragrance-types")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminFragranceTypesController : ControllerBase
{
    private readonly IFragranceTypeService _fragranceTypeService;

    public AdminFragranceTypesController(IFragranceTypeService fragranceTypeService)
    {
        _fragranceTypeService = fragranceTypeService;
    }

    [HttpPost]
    public async Task<ActionResult<FragranceTypeDto>> Create(
        [FromBody] CreateFragranceTypeRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _fragranceTypeService.CreateAsync(request, cancellationToken);
        return Created($"/api/fragrance-types/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FragranceTypeDto>> Update(
        int id,
        [FromBody] UpdateFragranceTypeRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _fragranceTypeService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _fragranceTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

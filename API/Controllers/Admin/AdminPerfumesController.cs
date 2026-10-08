using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/perfumes")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminPerfumesController : ControllerBase
{
    private readonly IPerfumeService _perfumeService;

    public AdminPerfumesController(IPerfumeService perfumeService)
    {
        _perfumeService = perfumeService;
    }

    [HttpPost]
    public async Task<ActionResult<PerfumeDetailsDto>> Create(
        [FromBody] CreatePerfumeRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _perfumeService.CreateAsync(request, cancellationToken);
        return Created($"/api/perfumes/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PerfumeDetailsDto>> Update(
        int id,
        [FromBody] UpdatePerfumeRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _perfumeService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _perfumeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

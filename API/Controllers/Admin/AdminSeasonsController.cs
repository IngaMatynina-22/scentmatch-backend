using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/seasons")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminSeasonsController : ControllerBase
{
    private readonly ISeasonService _seasonService;

    public AdminSeasonsController(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    [HttpPost]
    public async Task<ActionResult<SeasonDto>> Create(
        [FromBody] CreateSeasonRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _seasonService.CreateAsync(request, cancellationToken);
        return Created($"/api/seasons/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SeasonDto>> Update(
        int id,
        [FromBody] UpdateSeasonRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _seasonService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _seasonService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

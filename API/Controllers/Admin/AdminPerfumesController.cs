using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/perfumes")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminPerfumesController : ControllerBase
{
    private readonly IPerfumeService _perfumeService;
    private readonly IAdminAuditService _auditService;

    public AdminPerfumesController(IPerfumeService perfumeService, IAdminAuditService auditService)
    {
        _perfumeService = perfumeService;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<ActionResult<PerfumeDetailsDto>> Create(
        [FromBody] CreatePerfumeRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _perfumeService.CreateAsync(request, cancellationToken);
        await _auditService.LogAsync(
            GetCurrentUserId(),
            "CreatePerfume",
            "Perfume",
            created.Id.ToString(),
            details: $"name={created.Name}",
            cancellationToken: cancellationToken);
        return Created($"/api/perfumes/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PerfumeDetailsDto>> Update(
        int id,
        [FromBody] UpdatePerfumeRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _perfumeService.UpdateAsync(id, request, cancellationToken);
        await _auditService.LogAsync(
            GetCurrentUserId(),
            "UpdatePerfume",
            "Perfume",
            id.ToString(),
            details: $"name={updated.Name}",
            cancellationToken: cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _perfumeService.DeleteAsync(id, cancellationToken);
        await _auditService.LogAsync(
            GetCurrentUserId(),
            "DeletePerfume",
            "Perfume",
            id.ToString(),
            cancellationToken: cancellationToken);
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

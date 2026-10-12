using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/catalog")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminCatalogController : ControllerBase
{
    private readonly IAdminCatalogService _catalogService;

    public AdminCatalogController(IAdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("quality")]
    [ProducesResponseType(typeof(CatalogQualityReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogQualityReportDto>> Quality(CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetQualityReportAsync(cancellationToken));
    }

    [HttpGet("duplicates")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogDuplicateGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CatalogDuplicateGroupDto>>> Duplicates(
        CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetDuplicatesAsync(cancellationToken));
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(CatalogImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogImportResultDto>> Import(
        [FromBody] CatalogImportRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.ImportAsync(request, GetCurrentUserId(), cancellationToken));
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

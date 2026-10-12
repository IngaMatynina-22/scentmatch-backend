using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Common.Models;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminAuditService _auditService;

    public AdminAuditLogsController(IAdminAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminAuditLogDto>>> Get(
        [FromQuery] AdminAuditLogQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _auditService.GetAsync(query, cancellationToken));
    }
}

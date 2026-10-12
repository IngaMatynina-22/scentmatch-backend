using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/integrations")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminIntegrationsController : ControllerBase
{
    private readonly IAdminIntegrationsService _integrationsService;

    public AdminIntegrationsController(IAdminIntegrationsService integrationsService)
    {
        _integrationsService = integrationsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminIntegrationsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminIntegrationsDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _integrationsService.GetAsync(cancellationToken));
    }
}

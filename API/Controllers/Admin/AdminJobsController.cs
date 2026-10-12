using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Common.Models;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/jobs")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminJobsController : ControllerBase
{
    private readonly IAdminJobsService _jobsService;

    public AdminJobsController(IAdminJobsService jobsService)
    {
        _jobsService = jobsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BackgroundJobLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BackgroundJobLogDto>>> Get(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        return Ok(await _jobsService.GetRecentAsync(pagination, cancellationToken));
    }
}

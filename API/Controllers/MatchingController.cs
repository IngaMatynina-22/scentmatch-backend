using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/matching")]
[Authorize]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;

    public MatchingController(IMatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MatchingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchingResponse>> Match(
        [FromBody] MatchingRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _matchingService.MatchAsync(request, cancellationToken));
    }
}

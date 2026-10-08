using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Comparison.DTOs;
using ScentMatch.Application.Comparison.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/comparison")]
[AllowAnonymous]
public class ComparisonController : ControllerBase
{
    private readonly IComparisonService _comparisonService;

    public ComparisonController(IComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComparisonResponse>> Compare(
        [FromBody] ComparisonRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _comparisonService.CompareAsync(request, cancellationToken));
    }
}

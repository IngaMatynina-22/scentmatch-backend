using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;

namespace ScentMatch.API.Controllers;

/// <summary>
/// Dedicated barcode lookup controller so the route never conflicts with api/perfumes/{id}.
/// </summary>
[ApiController]
[Route("api/perfumes")]
[AllowAnonymous]
public class PerfumesBarcodeController : ControllerBase
{
    private readonly IPerfumeService _perfumeService;

    public PerfumesBarcodeController(IPerfumeService perfumeService)
    {
        _perfumeService = perfumeService;
    }

    [HttpGet("by-barcode/{barcode}")]
    [ProducesResponseType(typeof(BarcodeLookupResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BarcodeLookupResultDto>> GetByBarcode(
        string barcode,
        CancellationToken cancellationToken)
    {
        return Ok(await _perfumeService.GetByBarcodeAsync(barcode, cancellationToken));
    }
}

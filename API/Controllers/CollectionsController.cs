using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/collections")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private static readonly JsonSerializerOptions ImportJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ICollectionsService _collectionsService;
    private readonly ICollectionImportExportService _importExportService;

    public CollectionsController(
        ICollectionsService collectionsService,
        ICollectionImportExportService importExportService)
    {
        _collectionsService = collectionsService;
        _importExportService = importExportService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollections(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _collectionsService.GetCollectionsAsync(userId, pagination, cancellationToken));
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var (content, contentType, fileName) =
            await _importExportService.ExportAsync(userId, format, cancellationToken);
        return File(Encoding.UTF8.GetBytes(content), contentType, fileName);
    }

    [HttpPost("import")]
    [RequestSizeLimit(CollectionImportLimits.MaxFileBytes)]
    [ProducesResponseType(typeof(CollectionImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionImportResult>> Import(
        [FromQuery] bool preview = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (Request.HasFormContentType)
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
            {
                throw new BadRequestException("A file is required for multipart import.");
            }

            if (file.Length > CollectionImportLimits.MaxFileBytes)
            {
                throw new BadRequestException(
                    $"File must be at most {CollectionImportLimits.MaxFileBytes} bytes.");
            }

            var fileName = file.FileName ?? string.Empty;
            await using var stream = file.OpenReadStream();

            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(file.ContentType, "text/csv", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(await _importExportService.ImportCsvAsync(
                    userId, stream, preview, cancellationToken));
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var json = await reader.ReadToEndAsync(cancellationToken);
            var document = JsonSerializer.Deserialize<CollectionExportDocument>(json, ImportJsonOptions)
                ?? throw new BadRequestException("Invalid JSON import payload.");
            return Ok(await _importExportService.ImportJsonAsync(
                userId, document, preview, cancellationToken));
        }

        using var bodyReader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var body = await bodyReader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new BadRequestException("Import body is required.");
        }

        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            CollectionExportDocument document;
            if (trimmed.StartsWith('['))
            {
                var items = JsonSerializer.Deserialize<List<CollectionExportItem>>(body, ImportJsonOptions)
                    ?? throw new BadRequestException("Invalid JSON import payload.");
                document = new CollectionExportDocument { Collections = items };
            }
            else
            {
                document = JsonSerializer.Deserialize<CollectionExportDocument>(body, ImportJsonOptions)
                    ?? throw new BadRequestException("Invalid JSON import payload.");
            }

            return Ok(await _importExportService.ImportJsonAsync(
                userId, document, preview, cancellationToken));
        }

        await using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return Ok(await _importExportService.ImportCsvAsync(
            userId, csvStream, preview, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CollectionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _collectionsService.GetByIdAsync(userId, id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionDto>> Create(
        [FromBody] CreateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var created = await _collectionsService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/collections/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDto>> Update(
        int id,
        [FromBody] UpdateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _collectionsService.UpdateAsync(userId, id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _collectionsService.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{collectionId:int}/perfumes/{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPerfume(
        int collectionId,
        int perfumeId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _collectionsService.AddPerfumeAsync(userId, collectionId, perfumeId, cancellationToken);
        return Created($"/api/collections/{collectionId}/perfumes/{perfumeId}", null);
    }

    [HttpDelete("{collectionId:int}/perfumes/{perfumeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePerfume(
        int collectionId,
        int perfumeId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _collectionsService.RemovePerfumeAsync(userId, collectionId, perfumeId, cancellationToken);
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

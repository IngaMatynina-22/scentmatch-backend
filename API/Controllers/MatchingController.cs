using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.History.Interfaces;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/matching")]
[Authorize]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly ITextMatchingService _textMatchingService;
    private readonly IGiftMatchingService _giftMatchingService;
    private readonly ISelectionHistoryService _selectionHistoryService;

    public MatchingController(
        IMatchingService matchingService,
        ITextMatchingService textMatchingService,
        IGiftMatchingService giftMatchingService,
        ISelectionHistoryService selectionHistoryService)
    {
        _matchingService = matchingService;
        _textMatchingService = textMatchingService;
        _giftMatchingService = giftMatchingService;
        _selectionHistoryService = selectionHistoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MatchingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchingResponse>> Match(
        [FromBody] MatchingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _matchingService.MatchAsync(request, cancellationToken);
        await _selectionHistoryService.CreateFromMatchingAsync(
            userId,
            request,
            result.Items,
            MatchingScenarioTypes.Personal,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Free-text matching (UA/EN). Persists selection history with ScenarioType=Text.
    /// </summary>
    [HttpPost("text")]
    [ProducesResponseType(typeof(TextMatchingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TextMatchingResponse>> MatchText(
        [FromBody] TextMatchingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _textMatchingService.MatchAsync(request, cancellationToken);

        var matchingRequest = TextCriteriaToMatchingRequest(result.ParsedCriteria);
        if (HasAnyCriterion(matchingRequest))
        {
            await _selectionHistoryService.CreateFromMatchingAsync(
                userId,
                matchingRequest,
                result.Items,
                MatchingScenarioTypes.Text,
                cancellationToken);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gift matching. Authenticated users get ScenarioType=Gift history (excluded from personalization).
    /// Anonymous callers are allowed and skip history persistence.
    /// </summary>
    [HttpPost("gift")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GiftMatchingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GiftMatchingResponse>> MatchGift(
        [FromBody] GiftMatchingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _giftMatchingService.MatchAsync(request, cancellationToken);

        if (TryGetCurrentUserId(out var userId))
        {
            var matchingRequest = new MatchingRequest
            {
                SeasonId = request.SeasonId,
                FragranceTypeId = request.FragranceTypeId,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                Intensity = request.Intensity,
                Gender = request.Gender,
                SelectedNoteIds = request.PreferredNoteIds,
                ExcludedNoteIds = request.ExcludedNoteIds
            };

            await _selectionHistoryService.CreateFromMatchingAsync(
                userId,
                matchingRequest,
                result.Items,
                MatchingScenarioTypes.Gift,
                cancellationToken);
        }

        return Ok(result);
    }

    private static MatchingRequest TextCriteriaToMatchingRequest(ParsedTextCriteriaDto criteria) => new()
    {
        SeasonId = criteria.SeasonId,
        FragranceTypeId = criteria.FragranceTypeId,
        MinPrice = criteria.MinPrice,
        MaxPrice = criteria.MaxPrice,
        Intensity = criteria.Intensity,
        Concentration = criteria.Concentration,
        Gender = criteria.Gender,
        SelectedNoteIds = criteria.PreferredNotes.Select(x => x.Id).ToList(),
        ExcludedNoteIds = criteria.ExcludedNotes.Select(x => x.Id).ToList()
    };

    private static bool HasAnyCriterion(MatchingRequest request) =>
        request.SeasonId.HasValue
        || request.FragranceTypeId.HasValue
        || request.MinPrice.HasValue
        || request.MaxPrice.HasValue
        || request.Intensity.HasValue
        || !string.IsNullOrWhiteSpace(request.Concentration)
        || !string.IsNullOrWhiteSpace(request.Gender)
        || (request.SelectedNoteIds is { Count: > 0 })
        || (request.ExcludedNoteIds is { Count: > 0 });

    private int GetCurrentUserId()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            throw new UnauthorizedException("User identity is invalid.");
        }

        return userId;
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(userIdClaim, out userId);
    }
}

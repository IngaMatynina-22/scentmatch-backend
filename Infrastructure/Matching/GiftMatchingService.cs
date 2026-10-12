using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;

namespace ScentMatch.Infrastructure.Matching;

public class GiftMatchingService : IGiftMatchingService
{
    private readonly ScentMatchDbContext _dbContext;
    private readonly IMatchingService _matchingService;

    public GiftMatchingService(ScentMatchDbContext dbContext, IMatchingService matchingService)
    {
        _dbContext = dbContext;
        _matchingService = matchingService;
    }

    public async Task<GiftMatchingResponse> MatchAsync(
        GiftMatchingRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateForeignKeysAsync(request, cancellationToken);

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

        // Occasion is informational for clients; map a few known occasions to season when season omitted.
        if (!matchingRequest.SeasonId.HasValue && !string.IsNullOrWhiteSpace(request.Occasion))
        {
            matchingRequest.SeasonId = await TryMapOccasionToSeasonAsync(request.Occasion, cancellationToken);
        }

        var matching = await _matchingService.MatchAsync(matchingRequest, cancellationToken);
        var maxResults = Math.Clamp(request.MaxResults, 1, MatchingWeights.MaxResults);

        return new GiftMatchingResponse
        {
            Occasion = request.Occasion,
            Currency = request.Currency,
            Items = matching.Items.Take(maxResults).ToList()
        };
    }

    private async Task<int?> TryMapOccasionToSeasonAsync(string occasion, CancellationToken cancellationToken)
    {
        var key = occasion.Trim().ToLowerInvariant();
        string? seasonName = key switch
        {
            "summer" or "пляж" or "beach" or "vacation" => "Summer",
            "winter" or "christmas" or "новий рік" or "new year" => "Winter",
            "spring" or "easter" => "Spring",
            "autumn" or "fall" => "Autumn",
            _ => null
        };

        if (seasonName is null)
        {
            return null;
        }

        return await _dbContext.Seasons
            .AsNoTracking()
            .Where(x => x.Name == seasonName)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task ValidateForeignKeysAsync(GiftMatchingRequest request, CancellationToken cancellationToken)
    {
        if (request.SeasonId.HasValue)
        {
            var exists = await _dbContext.Seasons
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.SeasonId.Value, cancellationToken);
            if (!exists)
            {
                throw new BadRequestException($"Season with id {request.SeasonId.Value} was not found.");
            }
        }

        if (request.FragranceTypeId.HasValue)
        {
            var exists = await _dbContext.FragranceTypes
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.FragranceTypeId.Value, cancellationToken);
            if (!exists)
            {
                throw new BadRequestException($"Fragrance type with id {request.FragranceTypeId.Value} was not found.");
            }
        }

        async Task ValidateNotesAsync(IReadOnlyList<int>? ids, string label)
        {
            if (ids is not { Count: > 0 })
            {
                return;
            }

            var list = ids.ToList();
            var existingCount = await _dbContext.Notes
                .AsNoTracking()
                .CountAsync(x => list.Contains(x.Id), cancellationToken);
            if (existingCount != list.Count)
            {
                throw new BadRequestException($"One or more {label} were not found.");
            }
        }

        await ValidateNotesAsync(request.PreferredNoteIds, "preferred notes");
        await ValidateNotesAsync(request.ExcludedNoteIds, "excluded notes");
    }
}

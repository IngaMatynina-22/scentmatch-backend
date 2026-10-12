using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ScentMatch.Infrastructure.Matching;

public class MatchingService : IMatchingService
{
    private readonly ScentMatchDbContext _dbContext;

    public MatchingService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MatchingResponse> MatchAsync(
        MatchingRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateForeignKeysAsync(request, cancellationToken);

        var profiles = await PerfumeProfileLoader.LoadActiveAsync(_dbContext, cancellationToken);
        var excluded = request.ExcludedNoteIds is { Count: > 0 }
            ? request.ExcludedNoteIds.ToHashSet()
            : null;

        var results = profiles
            .Where(p => excluded is null
                        || !p.Notes.Any(n => excluded.Contains(n.NoteId)))
            .Select(p =>
            {
                var breakdown = MatchingCriteriaScorer.Score(p, request);
                if (breakdown.TotalScore <= 0m)
                {
                    return null;
                }

                return MatchingCriteriaScorer.ToMatchingResult(p, breakdown);
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PerfumeId)
            .Take(MatchingWeights.MaxResults)
            .ToList();

        return new MatchingResponse { Items = results };
    }

    private async Task ValidateForeignKeysAsync(MatchingRequest request, CancellationToken cancellationToken)
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

        if (request.SelectedNoteIds is { Count: > 0 })
        {
            var noteIds = request.SelectedNoteIds.ToList();
            var existingCount = await _dbContext.Notes
                .AsNoTracking()
                .CountAsync(x => noteIds.Contains(x.Id), cancellationToken);

            if (existingCount != noteIds.Count)
            {
                throw new BadRequestException("One or more selected notes were not found.");
            }
        }

        if (request.ExcludedNoteIds is { Count: > 0 })
        {
            var noteIds = request.ExcludedNoteIds.ToList();
            var existingCount = await _dbContext.Notes
                .AsNoTracking()
                .CountAsync(x => noteIds.Contains(x.Id), cancellationToken);

            if (existingCount != noteIds.Count)
            {
                throw new BadRequestException("One or more excluded notes were not found.");
            }
        }
    }
}

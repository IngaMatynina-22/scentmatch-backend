using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Matching;

public class TextMatchingService : ITextMatchingService
{
    private readonly ScentMatchDbContext _dbContext;
    private readonly IMatchingService _matchingService;
    private readonly ITextCriteriaEnricher _enricher;

    public TextMatchingService(
        ScentMatchDbContext dbContext,
        IMatchingService matchingService,
        ITextCriteriaEnricher enricher)
    {
        _dbContext = dbContext;
        _matchingService = matchingService;
        _enricher = enricher;
    }

    public async Task<TextMatchingResponse> MatchAsync(
        TextMatchingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new BadRequestException("Text is required.");
        }

        var catalog = await LoadCatalogAsync(cancellationToken);
        var parsed = TextCriteriaParser.Parse(request.Text, catalog);
        var ambiguous = parsed.AmbiguousCriteria.ToList();

        await _enricher.EnrichAsync(request.Text, parsed.Criteria, ambiguous, cancellationToken);

        var matchingRequest = TextCriteriaParser.ToMatchingRequest(parsed.Criteria);
        if (!HasAnyCriterion(matchingRequest))
        {
            return new TextMatchingResponse
            {
                ParsedCriteria = parsed.Criteria,
                AmbiguousCriteria = ambiguous,
                Items = Array.Empty<MatchingResultDto>()
            };
        }

        var matching = await _matchingService.MatchAsync(matchingRequest, cancellationToken);
        var maxResults = Math.Clamp(request.MaxResults, 1, MatchingWeights.MaxResults);

        return new TextMatchingResponse
        {
            ParsedCriteria = parsed.Criteria,
            AmbiguousCriteria = ambiguous,
            Items = matching.Items.Take(maxResults).ToList()
        };
    }

    private async Task<TextCriteriaCatalog> LoadCatalogAsync(CancellationToken cancellationToken)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Select(x => new CatalogName(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        var seasons = await _dbContext.Seasons
            .AsNoTracking()
            .Select(x => new CatalogName(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        var fragranceTypes = await _dbContext.FragranceTypes
            .AsNoTracking()
            .Select(x => new CatalogName(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        return new TextCriteriaCatalog
        {
            Notes = notes,
            Seasons = seasons,
            FragranceTypes = fragranceTypes
        };
    }

    private static bool HasAnyCriterion(MatchingRequest request) =>
        request.SeasonId.HasValue
        || request.FragranceTypeId.HasValue
        || request.MinPrice.HasValue
        || request.MaxPrice.HasValue
        || request.Intensity.HasValue
        || !string.IsNullOrWhiteSpace(request.Concentration)
        || !string.IsNullOrWhiteSpace(request.Gender)
        || (request.SelectedNoteIds is { Count: > 0 })
        || (request.ExcludedNoteIds is { Count: > 0 })
        || request.Sweetness.HasValue
        || request.Freshness.HasValue
        || request.Woody.HasValue
        || request.Spicy.HasValue
        || request.Citrus.HasValue
        || request.Floral.HasValue
        || request.Fruity.HasValue
        || request.Powdery.HasValue
        || request.Smoky.HasValue
        || request.Oriental.HasValue;
}

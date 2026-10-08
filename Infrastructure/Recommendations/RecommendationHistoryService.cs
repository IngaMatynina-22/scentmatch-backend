using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Recommendations.DTOs;
using ScentMatch.Application.Recommendations.Interfaces;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Recommendations;

public class RecommendationHistoryService : IRecommendationHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ScentMatchDbContext _dbContext;

    public RecommendationHistoryService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<RecommendationHistorySummaryDto>> GetBatchesAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        // Project minimal columns for the current user, then group in-memory.
        // Avoids InMemory provider GroupBy translation gaps while keeping ownership on SQL.
        var rows = await _dbContext.RecommendationHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.BatchId, x.CreatedAt, x.Score })
            .ToListAsync(cancellationToken);

        var batches = rows
            .GroupBy(x => x.BatchId)
            .Select(g => new RecommendationHistorySummaryDto
            {
                BatchId = g.Key,
                CreatedAt = g.Max(x => x.CreatedAt),
                ItemCount = g.Count(),
                TopScore = g.Max(x => x.Score)
            })
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.BatchId)
            .ToList();

        var totalItems = batches.Count;
        var pageItems = batches
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToList();

        return PagedResult<RecommendationHistorySummaryDto>.Create(
            pageItems,
            pagination.Page,
            pagination.PageSize,
            totalItems);
    }

    public async Task<RecommendationHistoryDetailsDto> GetBatchByIdAsync(
        int userId,
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.RecommendationHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.BatchId == batchId)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Perfume.Name)
            .ThenBy(x => x.PerfumeId)
            .Select(x => new
            {
                x.CreatedAt,
                x.PerfumeId,
                Name = x.Perfume.Name,
                BrandName = x.Perfume.Brand.Name,
                x.Perfume.Price,
                x.Score,
                x.Reasons
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            throw new NotFoundException($"Recommendation history batch '{batchId}' was not found.");
        }

        return new RecommendationHistoryDetailsDto
        {
            BatchId = batchId,
            CreatedAt = rows[0].CreatedAt,
            // Score/Reasons come from stored snapshot; perfume display fields are current catalog labels.
            Items = rows.Select(x => new RecommendationDto
            {
                PerfumeId = x.PerfumeId,
                Name = x.Name,
                BrandName = x.BrandName,
                Price = x.Price,
                Score = x.Score,
                Reasons = DeserializeReasons(x.Reasons)
            }).ToList()
        };
    }

    private static IReadOnlyList<string> DeserializeReasons(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }
}

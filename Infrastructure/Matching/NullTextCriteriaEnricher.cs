using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;

namespace ScentMatch.Infrastructure.Matching;

/// <summary>No-op enricher — keeps text matching fully deterministic without LLM.</summary>
public sealed class NullTextCriteriaEnricher : ITextCriteriaEnricher
{
    public Task EnrichAsync(
        string text,
        ParsedTextCriteriaDto criteria,
        List<string> ambiguousCriteria,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

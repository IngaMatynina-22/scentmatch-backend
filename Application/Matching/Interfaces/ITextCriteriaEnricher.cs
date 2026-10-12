using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Interfaces;

/// <summary>
/// Optional LLM/heuristic enricher for free-text matching. Default implementation is a no-op.
/// </summary>
public interface ITextCriteriaEnricher
{
    Task EnrichAsync(
        string text,
        ParsedTextCriteriaDto criteria,
        List<string> ambiguousCriteria,
        CancellationToken cancellationToken = default);
}

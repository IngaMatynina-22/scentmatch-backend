using ScentMatch.Application.Comparison.DTOs;

namespace ScentMatch.Application.Comparison.Interfaces;

public interface IComparisonService
{
    Task<ComparisonResponse> CompareAsync(
        ComparisonRequest request,
        CancellationToken cancellationToken = default);
}

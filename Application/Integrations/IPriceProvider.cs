namespace ScentMatch.Application.Integrations;

public interface IPriceProvider
{
    bool IsConfigured { get; }

    string GetName();

    /// <summary>
    /// Attempts to resolve a live price. Must never invent or fabricate prices.
    /// </summary>
    Task<PriceProviderResult> TryGetPriceAsync(
        PriceProviderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PriceProviderRequest
{
    public int PerfumeId { get; init; }
    public string PerfumeName { get; init; } = string.Empty;
    public string? BrandName { get; init; }
    public string? SourceUrl { get; init; }
    public string? SourceName { get; init; }
    public string Currency { get; init; } = "EUR";
}

public sealed class PriceProviderResult
{
    public bool IsConfigured { get; init; }
    public bool Success { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public string? SourceName { get; init; }
    public string? SourceUrl { get; init; }
    public string? Message { get; init; }

    public static PriceProviderResult NotConfigured(string providerName) => new()
    {
        IsConfigured = false,
        Success = false,
        Message = $"{providerName} is not configured."
    };

    public static PriceProviderResult Failed(string message) => new()
    {
        IsConfigured = true,
        Success = false,
        Message = message
    };

    public static PriceProviderResult Ok(
        decimal price,
        string currency,
        string? sourceName = null,
        string? sourceUrl = null) => new()
    {
        IsConfigured = true,
        Success = true,
        Price = price,
        Currency = currency,
        SourceName = sourceName,
        SourceUrl = sourceUrl
    };
}

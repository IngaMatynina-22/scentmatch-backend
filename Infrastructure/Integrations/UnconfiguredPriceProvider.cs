using ScentMatch.Application.Integrations;

namespace ScentMatch.Infrastructure.Integrations;

/// <summary>
/// Default price provider stub. Never returns fabricated prices.
/// </summary>
public class UnconfiguredPriceProvider : IPriceProvider
{
    public const string ProviderName = "Unconfigured";

    public bool IsConfigured => false;

    public string GetName() => ProviderName;

    public Task<PriceProviderResult> TryGetPriceAsync(
        PriceProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PriceProviderResult.NotConfigured(ProviderName));
    }
}

/// <summary>
/// Alias for <see cref="UnconfiguredPriceProvider"/>.
/// </summary>
public sealed class NullPriceProvider : UnconfiguredPriceProvider
{
}

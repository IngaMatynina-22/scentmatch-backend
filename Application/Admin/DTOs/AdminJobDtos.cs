namespace ScentMatch.Application.Admin.DTOs;

public class BackgroundJobLogDto
{
    public long Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? Message { get; set; }
}

public class AdminIntegrationsDto
{
    public PriceTrackingIntegrationDto PriceTracking { get; set; } = new();
}

public class PriceTrackingIntegrationDto
{
    public bool Enabled { get; set; }
    public int CheckIntervalMinutes { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool ProviderConfigured { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public string? LastSuccessMessage { get; set; }
    public string? LastErrorMessage { get; set; }
}

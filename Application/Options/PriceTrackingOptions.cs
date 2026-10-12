namespace ScentMatch.Application.Options;

public class PriceTrackingOptions
{
    public const string SectionName = "PriceTracking";

    public bool Enabled { get; set; }
    public int CheckIntervalMinutes { get; set; } = 60;
}

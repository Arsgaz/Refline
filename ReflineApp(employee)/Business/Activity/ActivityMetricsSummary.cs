using Refline.Models;

namespace Refline.Business.Activity;

public sealed class ActivityMetricsSummary
{
    public int TotalTrackedSeconds { get; init; }
    public int ActiveSeconds { get; init; }
    public int IdleSeconds { get; init; }
    public int ProductiveSeconds { get; init; }
    public string TopApplicationName { get; init; } = "—";
    public ActivityCategory TopCategory { get; init; } = ActivityCategory.Unknown;
}

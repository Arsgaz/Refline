namespace Refline.Business.Activity;

public sealed class ActivitySummary
{
    public int TotalSeconds { get; init; }
    public string TodayTotalString { get; init; } = "0 ч 00 мин";
    public string MostActiveAppName { get; init; } = "—";
    public ActivityMetricsSummary Metrics { get; init; } = new();
}

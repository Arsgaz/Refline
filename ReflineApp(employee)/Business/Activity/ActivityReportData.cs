using Refline.Business.Reports;
using Refline.Models;

namespace Refline.Business.Activity;

public sealed class ActivityReportData
{
    public ReportPeriodRange Range { get; init; }
    public IReadOnlyList<AppActivity> Activities { get; init; } = new List<AppActivity>();
    public ActivitySummary Summary { get; init; } = new();
    public IReadOnlyList<ActivityDailyBucket> DailyBuckets { get; init; } = new List<ActivityDailyBucket>();
}

namespace Refline.Admin.Models;

public sealed class EmployeeAnalyticsSnapshot
{
    public required UserAnalyticsSummary Summary { get; init; }

    public required UserActivityBreakdown Breakdown { get; init; }
}

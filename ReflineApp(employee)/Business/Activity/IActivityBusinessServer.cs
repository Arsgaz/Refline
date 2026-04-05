using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Activity;

public interface IActivityBusinessServer
{
    bool IsTracking { get; }
    OperationResult<IReadOnlyList<AppActivity>> LoadTodayActivities();
    OperationResult<IReadOnlyList<AppActivity>> LoadActivitiesByRange(DateTime startDate, DateTime endDate);
    OperationResult StartTracking();
    OperationResult StopTracking();
    OperationResult<ActivityTickResult> ProcessWindowActivity(string windowTitle, bool isIdle, DateTime timestamp);
    OperationResult<ActivitySummary> GetTodaySummary();
    OperationResult<ActivitySummary> GetSummary(DateTime startDate, DateTime endDate);
    OperationResult<IReadOnlyList<ActivityDailyBucket>> GetDailyBuckets(DateTime startDate, DateTime endDate);
    OperationResult<ActivityReportData> GetReportData(DateTime startDate, DateTime endDate);
    OperationResult SaveCurrentSession();
}

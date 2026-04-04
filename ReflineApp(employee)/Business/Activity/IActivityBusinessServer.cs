using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Activity;

public interface IActivityBusinessServer
{
    bool IsTracking { get; }
    OperationResult<IReadOnlyList<AppActivity>> LoadTodayActivities();
    OperationResult StartTracking();
    OperationResult StopTracking();
    OperationResult<ActivityTickResult> ProcessWindowActivity(string windowTitle, bool isIdle, DateTime timestamp);
    OperationResult<ActivitySummary> GetTodaySummary();
    OperationResult SaveCurrentSession();
}

using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Activity;

public interface IActivityDataService
{
    OperationResult<IReadOnlyList<AppActivity>> LoadByDate(DateTime activityDate);
    OperationResult<AppActivity?> GetByAppAndDate(string appName, DateTime activityDate);
    OperationResult SaveOrUpdate(AppActivity activity);
    OperationResult SaveAll(IEnumerable<AppActivity> activities, DateTime activityDate);
}

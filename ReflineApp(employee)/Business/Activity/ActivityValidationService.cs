using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Activity;

public class ActivityValidationService
{
    public OperationResult ValidateWindowTitle(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            return OperationResult.Failure("Название приложения не может быть пустым.", "INVALID_APP_NAME");
        }

        return OperationResult.Success();
    }

    public OperationResult ValidateEntity(AppActivity activity)
    {
        if (string.IsNullOrWhiteSpace(activity.AppName))
        {
            return OperationResult.Failure("Название приложения не может быть пустым.", "INVALID_APP_NAME");
        }

        if (activity.TimeSpentSeconds < 0)
        {
            return OperationResult.Failure("Время активности не может быть отрицательным.", "INVALID_TIME_SPENT");
        }

        if (activity.LastActive == default)
        {
            return OperationResult.Failure("Время последней активности указано некорректно.", "INVALID_LAST_ACTIVE");
        }

        if (activity.ActivityDate == default)
        {
            return OperationResult.Failure("Дата активности указана некорректно.", "INVALID_ACTIVITY_DATE");
        }

        return OperationResult.Success();
    }
}

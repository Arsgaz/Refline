using Refline.Data.Activity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Activity;

public class ActivityBusinessServer : IActivityBusinessServer
{
    private readonly IActivityDataService _activityDataService;
    private readonly ActivityValidationService _validationService;
    private readonly ActivityLockService _lockService;
    private readonly List<AppActivity> _todayActivities = new();

    public bool IsTracking { get; private set; }

    public ActivityBusinessServer(
        IActivityDataService activityDataService,
        ActivityValidationService validationService,
        ActivityLockService lockService)
    {
        _activityDataService = activityDataService;
        _validationService = validationService;
        _lockService = lockService;
    }

    public OperationResult<IReadOnlyList<AppActivity>> LoadTodayActivities()
    {
        return _lockService.ExecuteLocked(() =>
        {
            var loadResult = _activityDataService.LoadByDate(DateTime.Today);
            if (!loadResult.IsSuccess || loadResult.Value == null)
            {
                return OperationResult<IReadOnlyList<AppActivity>>.Failure(loadResult.Message, loadResult.ErrorCode);
            }

            _todayActivities.Clear();
            _todayActivities.AddRange(loadResult.Value.Select(Clone));

            return OperationResult<IReadOnlyList<AppActivity>>.Success(_todayActivities.Select(Clone).ToList());
        });
    }

    public OperationResult StartTracking()
    {
        IsTracking = true;
        return OperationResult.Success("Трекинг запущен.");
    }

    public OperationResult StopTracking()
    {
        IsTracking = false;
        return SaveCurrentSession();
    }

    public OperationResult<ActivityTickResult> ProcessWindowActivity(string windowTitle, bool isIdle, DateTime timestamp)
    {
        return _lockService.ExecuteLocked(() =>
        {
            if (!IsTracking)
            {
                return OperationResult<ActivityTickResult>.Success(new ActivityTickResult
                {
                    StatusText = "Статус: остановлено",
                    Summary = BuildSummary()
                });
            }

            if (isIdle)
            {
                return OperationResult<ActivityTickResult>.Success(new ActivityTickResult
                {
                    StatusText = "Status: User is Idle (Paused)",
                    Summary = BuildSummary()
                });
            }

            var titleValidation = _validationService.ValidateWindowTitle(windowTitle);
            if (!titleValidation.IsSuccess)
            {
                return OperationResult<ActivityTickResult>.Failure(titleValidation.Message, titleValidation.ErrorCode);
            }

            var activityDate = timestamp.Date;
            var existing = _todayActivities.FirstOrDefault(a =>
                string.Equals(a.AppName, windowTitle, StringComparison.Ordinal) &&
                a.ActivityDate.Date == activityDate);

            var isNew = false;
            if (existing == null)
            {
                existing = new AppActivity
                {
                    AppName = windowTitle,
                    TimeSpentSeconds = 1,
                    LastActive = timestamp,
                    ActivityDate = activityDate,
                    Version = 0
                };
                _todayActivities.Add(existing);
                isNew = true;
            }
            else
            {
                existing.TimeSpentSeconds++;
                existing.LastActive = timestamp;
            }

            var entityValidation = _validationService.ValidateEntity(existing);
            if (!entityValidation.IsSuccess)
            {
                return OperationResult<ActivityTickResult>.Failure(entityValidation.Message, entityValidation.ErrorCode);
            }

            var saveResult = _activityDataService.SaveOrUpdate(existing);
            if (!saveResult.IsSuccess)
            {
                return OperationResult<ActivityTickResult>.Failure(saveResult.Message, saveResult.ErrorCode);
            }

            var summary = BuildSummary();
            return OperationResult<ActivityTickResult>.Success(new ActivityTickResult
            {
                StatusText = $"Status: Tracking ({windowTitle})",
                UpdatedActivity = Clone(existing),
                IsNewActivity = isNew,
                Summary = summary
            });
        });
    }

    public OperationResult<ActivitySummary> GetTodaySummary()
    {
        return _lockService.ExecuteLocked(() => OperationResult<ActivitySummary>.Success(BuildSummary()));
    }

    public OperationResult SaveCurrentSession()
    {
        return _lockService.ExecuteLocked(() =>
        {
            // Приложение локальное и однопользовательское: синхронизация внутри процесса
            // закрывает риск параллельной записи одной и той же активности.
            var saveResult = _activityDataService.SaveAll(_todayActivities.Select(Clone), DateTime.Today);
            if (!saveResult.IsSuccess)
            {
                return OperationResult.Failure(saveResult.Message, saveResult.ErrorCode);
            }

            return OperationResult.Success("Данные сессии сохранены.");
        });
    }

    private ActivitySummary BuildSummary()
    {
        if (_todayActivities.Count == 0)
        {
            return new ActivitySummary();
        }

        var totalSeconds = _todayActivities.Sum(a => a.TimeSpentSeconds);
        var totalTs = TimeSpan.FromSeconds(totalSeconds);
        var mostActive = _todayActivities.OrderByDescending(a => a.TimeSpentSeconds).First();
        var mostActiveTitle = mostActive.AppName.Length > 25
            ? mostActive.AppName[..25] + "..."
            : mostActive.AppName;

        return new ActivitySummary
        {
            TotalSeconds = totalSeconds,
            TodayTotalString = $"{(int)totalTs.TotalHours} ч {totalTs.Minutes:D2} мин",
            MostActiveAppName = mostActiveTitle
        };
    }

    private static AppActivity Clone(AppActivity source)
    {
        return new AppActivity
        {
            Id = source.Id,
            AppName = source.AppName,
            TimeSpentSeconds = source.TimeSpentSeconds,
            LastActive = source.LastActive,
            ActivityDate = source.ActivityDate,
            Version = source.Version
        };
    }
}

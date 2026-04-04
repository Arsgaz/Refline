using Refline.Data.Activity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Activity;

public class ActivityBusinessServer : IActivityBusinessServer
{
    private readonly IActivityDataService _activityDataService;
    private readonly ActivityValidationService _validationService;
    private readonly ActivityLockService _lockService;
    private readonly IActivityClassificationService _classificationService;
    private readonly IActivityMetricsService _metricsService;
    private readonly List<AppActivity> _todayActivities = new();

    public bool IsTracking { get; private set; }

    public ActivityBusinessServer(
        IActivityDataService activityDataService,
        ActivityValidationService validationService,
        ActivityLockService lockService,
        IActivityClassificationService classificationService,
        IActivityMetricsService metricsService)
    {
        _activityDataService = activityDataService;
        _validationService = validationService;
        _lockService = lockService;
        _classificationService = classificationService;
        _metricsService = metricsService;
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

            foreach (var activity in _todayActivities)
            {
                EnrichActivity(activity, activity.WindowTitle, activity.IsIdle);
            }

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

            var appName = _classificationService.NormalizeApplicationName(windowTitle, isIdle);

            var titleValidation = _validationService.ValidateWindowTitle(appName);
            if (!titleValidation.IsSuccess)
            {
                return OperationResult<ActivityTickResult>.Failure(titleValidation.Message, titleValidation.ErrorCode);
            }

            var activityDate = timestamp.Date;
            var existing = _todayActivities.FirstOrDefault(a =>
                string.Equals(a.AppName, appName, StringComparison.Ordinal) &&
                a.ActivityDate.Date == activityDate);

            var isNew = false;
            if (existing == null)
            {
                existing = new AppActivity
                {
                    AppName = appName,
                    WindowTitle = windowTitle ?? string.Empty,
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
                existing.WindowTitle = windowTitle ?? string.Empty;
            }

            EnrichActivity(existing, windowTitle, isIdle);

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
                StatusText = isIdle
                    ? "Статус: простой"
                    : $"Статус: отслеживание ({appName})",
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
            EnsureActivitiesEnriched();

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

        EnsureActivitiesEnriched();

        var metrics = _metricsService.Calculate(_todayActivities.Select(Clone).ToList());
        var totalTs = TimeSpan.FromSeconds(metrics.TotalTrackedSeconds);
        var mostActiveTitle = metrics.TopApplicationName.Length > 25
            ? metrics.TopApplicationName[..25] + "..."
            : metrics.TopApplicationName;

        return new ActivitySummary
        {
            TotalSeconds = metrics.TotalTrackedSeconds,
            TodayTotalString = $"{(int)totalTs.TotalHours} ч {totalTs.Minutes:D2} мин",
            MostActiveAppName = mostActiveTitle,
            Metrics = metrics
        };
    }

    private void EnsureActivitiesEnriched()
    {
        foreach (var activity in _todayActivities)
        {
            EnrichActivity(activity, activity.WindowTitle, activity.IsIdle);
        }
    }

    private void EnrichActivity(AppActivity activity, string? windowTitle, bool isIdle)
    {
        if (string.IsNullOrWhiteSpace(activity.WindowTitle))
        {
            activity.WindowTitle = string.IsNullOrWhiteSpace(windowTitle)
                ? activity.AppName
                : windowTitle.Trim();
        }

        activity.IsIdle = isIdle ||
            string.Equals(activity.AppName, "Простой", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(activity.AppName, "Idle", StringComparison.OrdinalIgnoreCase);
        activity.Category = _classificationService.Classify(activity.AppName, activity.WindowTitle);
        activity.IsProductive = ActivityProductivityRules.IsProductive(activity);
    }

    private static AppActivity Clone(AppActivity source)
    {
        return new AppActivity
        {
            Id = source.Id,
            AppName = source.AppName,
            WindowTitle = source.WindowTitle,
            Category = source.Category,
            IsIdle = source.IsIdle,
            IsProductive = source.IsProductive,
            TimeSpentSeconds = source.TimeSpentSeconds,
            LastActive = source.LastActive,
            ActivityDate = source.ActivityDate,
            Version = source.Version
        };
    }
}

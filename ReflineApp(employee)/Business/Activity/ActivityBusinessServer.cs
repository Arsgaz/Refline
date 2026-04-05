using Refline.Data.Activity;
using Refline.Data.Infrastructure;
using Refline.Data.Identity;
using Refline.Business.Identity;
using Refline.Models;
using Refline.Utils;

namespace Refline.Business.Activity;

public class ActivityBusinessServer : IActivityBusinessServer
{
    private readonly IActivityDataService _activityDataService;
    private readonly ActivityValidationService _validationService;
    private readonly ActivityLockService _lockService;
    private readonly IActivityClassificationService _classificationService;
    private readonly IActivityMetricsService _metricsService;
    private readonly IPendingActivityStore _pendingActivityStore;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILocalActivationStateStore _activationStateStore;
    private readonly List<AppActivity> _todayActivities = new();
    private TrackedActivitySegment? _activeSegment;

    public bool IsTracking { get; private set; }

    public ActivityBusinessServer(
        IActivityDataService activityDataService,
        ActivityValidationService validationService,
        ActivityLockService lockService,
        IActivityClassificationService classificationService,
        IActivityMetricsService metricsService,
        IPendingActivityStore pendingActivityStore,
        ICurrentUserContext currentUserContext,
        ILocalActivationStateStore activationStateStore)
    {
        _activityDataService = activityDataService;
        _validationService = validationService;
        _lockService = lockService;
        _classificationService = classificationService;
        _metricsService = metricsService;
        _pendingActivityStore = pendingActivityStore;
        _currentUserContext = currentUserContext;
        _activationStateStore = activationStateStore;
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

            var hasMetadataChanges = false;
            foreach (var activity in _todayActivities)
            {
                hasMetadataChanges |= EnrichActivity(activity, activity.WindowTitle, activity.IsIdle);
            }

            if (hasMetadataChanges)
            {
                var saveResult = _activityDataService.SaveAll(_todayActivities.Select(Clone), DateTime.Today);
                if (!saveResult.IsSuccess)
                {
                    return OperationResult<IReadOnlyList<AppActivity>>.Failure(
                        saveResult.Message,
                        saveResult.ErrorCode);
                }
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
            TrackSegment(existing, timestamp);

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
            FinalizeActiveSegment();
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

    private bool EnrichActivity(AppActivity activity, string? windowTitle, bool isIdle)
    {
        var previousWindowTitle = activity.WindowTitle;
        var previousIsIdle = activity.IsIdle;
        var previousCategory = activity.Category;
        var previousIsProductive = activity.IsProductive;

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

        return !string.Equals(previousWindowTitle, activity.WindowTitle, StringComparison.Ordinal) ||
            previousIsIdle != activity.IsIdle ||
            previousCategory != activity.Category ||
            previousIsProductive != activity.IsProductive;
    }

    private void TrackSegment(AppActivity activity, DateTime timestamp)
    {
        var observedAt = new DateTimeOffset(timestamp);

        if (_activeSegment == null)
        {
            _activeSegment = CreateTrackedSegment(activity, observedAt);
            return;
        }

        if (IsSameSegment(_activeSegment, activity))
        {
            _activeSegment.DurationSeconds++;
            _activeSegment.LastObservedAt = observedAt;
            _activeSegment.WindowTitle = activity.WindowTitle;
            _activeSegment.Category = activity.Category;
            _activeSegment.IsProductive = activity.IsProductive;
            return;
        }

        FinalizeActiveSegment();
        _activeSegment = CreateTrackedSegment(activity, observedAt);
    }

    private void FinalizeActiveSegment()
    {
        if (_activeSegment == null)
        {
            return;
        }

        try
        {
            var pendingSegment = BuildPendingActivitySegment(_activeSegment);
            if (pendingSegment != null)
            {
                var addResult = _pendingActivityStore.AddAsync(pendingSegment).GetAwaiter().GetResult();
                if (!addResult.IsSuccess)
                {
                    AppLogger.Log(addResult.Message, "ERROR");
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log($"Ошибка сохранения activity segment в outbox: {ex.Message}", "ERROR");
        }
        finally
        {
            _activeSegment = null;
        }
    }

    private PendingActivitySegment? BuildPendingActivitySegment(TrackedActivitySegment segment)
    {
        if (segment.DurationSeconds <= 0)
        {
            return null;
        }

        var currentUserId = _currentUserContext.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            AppLogger.Log("Пропуск activity segment для sync: текущий пользователь не определён.", "ERROR");
            return null;
        }

        var serverUserId = ApiIdentityIdMapper.ToServerId(currentUserId.Value);
        if (serverUserId <= 0)
        {
            AppLogger.Log("Пропуск activity segment для sync: некорректный server user id.", "ERROR");
            return null;
        }

        var activationStateResult = _activationStateStore.LoadAsync().GetAwaiter().GetResult();
        if (!activationStateResult.IsSuccess || activationStateResult.Value == null)
        {
            AppLogger.Log(activationStateResult.Message, "ERROR");
            return null;
        }

        if (string.IsNullOrWhiteSpace(activationStateResult.Value.CurrentDeviceId))
        {
            AppLogger.Log("Пропуск activity segment для sync: device id отсутствует.", "ERROR");
            return null;
        }

        return new PendingActivitySegment
        {
            UserId = serverUserId,
            DeviceId = activationStateResult.Value.CurrentDeviceId,
            AppName = segment.AppName,
            WindowTitle = segment.WindowTitle,
            Category = segment.Category.ToString(),
            IsIdle = segment.IsIdle,
            IsProductive = segment.IsProductive,
            DurationSeconds = segment.DurationSeconds,
            ActivityDate = segment.ActivityDate,
            StartedAt = segment.StartedAt,
            EndedAt = segment.LastObservedAt.AddSeconds(1),
            IsSynced = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static bool IsSameSegment(TrackedActivitySegment current, AppActivity activity)
    {
        return current.ActivityDate.Date == activity.ActivityDate.Date &&
            string.Equals(current.AppName, activity.AppName, StringComparison.Ordinal) &&
            string.Equals(current.WindowTitle, activity.WindowTitle, StringComparison.Ordinal) &&
            current.IsIdle == activity.IsIdle &&
            current.Category == activity.Category &&
            current.IsProductive == activity.IsProductive;
    }

    private static TrackedActivitySegment CreateTrackedSegment(AppActivity activity, DateTimeOffset observedAt)
    {
        return new TrackedActivitySegment
        {
            AppName = activity.AppName,
            WindowTitle = activity.WindowTitle,
            Category = activity.Category,
            IsIdle = activity.IsIdle,
            IsProductive = activity.IsProductive,
            ActivityDate = activity.ActivityDate.Date,
            StartedAt = observedAt,
            LastObservedAt = observedAt,
            DurationSeconds = 1
        };
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

    private sealed class TrackedActivitySegment
    {
        public string AppName { get; init; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public ActivityCategory Category { get; set; }
        public bool IsIdle { get; init; }
        public bool IsProductive { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime ActivityDate { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset LastObservedAt { get; set; }
    }
}

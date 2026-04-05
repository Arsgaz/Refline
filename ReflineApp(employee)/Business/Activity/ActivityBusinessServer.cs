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
            var loadResult = LoadTodayActivitiesInternal();
            if (!loadResult.IsSuccess || loadResult.Value == null)
            {
                return OperationResult<IReadOnlyList<AppActivity>>.Failure(loadResult.Message, loadResult.ErrorCode);
            }

            return OperationResult<IReadOnlyList<AppActivity>>.Success(loadResult.Value);
        });
    }

    public OperationResult<IReadOnlyList<AppActivity>> LoadActivitiesByRange(DateTime startDate, DateTime endDate)
    {
        return _lockService.ExecuteLocked(() =>
        {
            var sourceResult = LoadRawActivitiesByRange(startDate, endDate);
            if (!sourceResult.IsSuccess || sourceResult.Value == null)
            {
                return OperationResult<IReadOnlyList<AppActivity>>.Failure(sourceResult.Message, sourceResult.ErrorCode);
            }

            return OperationResult<IReadOnlyList<AppActivity>>.Success(AggregateActivitiesByApplication(sourceResult.Value));
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

    public OperationResult<ActivityTickResult> PauseTrackingForServiceWindow(string statusText)
    {
        return _lockService.ExecuteLocked(() =>
        {
            if (!IsTracking)
            {
                return OperationResult<ActivityTickResult>.Success(new ActivityTickResult
                {
                    StatusText = "Статус: остановлено",
                    Summary = BuildSummary(),
                    IsTrackingSuppressed = true
                });
            }

            FinalizeActiveSegment();

            return OperationResult<ActivityTickResult>.Success(new ActivityTickResult
            {
                StatusText = statusText,
                Summary = BuildSummary(),
                IsTrackingSuppressed = true
            });
        });
    }

    public OperationResult<ActivitySummary> GetTodaySummary()
    {
        return _lockService.ExecuteLocked(() => OperationResult<ActivitySummary>.Success(BuildSummary()));
    }

    public OperationResult<ActivitySummary> GetSummary(DateTime startDate, DateTime endDate)
    {
        return _lockService.ExecuteLocked(() =>
        {
            var sourceResult = LoadRawActivitiesByRange(startDate, endDate);
            if (!sourceResult.IsSuccess || sourceResult.Value == null)
            {
                return OperationResult<ActivitySummary>.Failure(sourceResult.Message, sourceResult.ErrorCode);
            }

            return OperationResult<ActivitySummary>.Success(BuildSummary(sourceResult.Value));
        });
    }

    public OperationResult<IReadOnlyList<ActivityDailyBucket>> GetDailyBuckets(DateTime startDate, DateTime endDate)
    {
        return _lockService.ExecuteLocked(() =>
        {
            var sourceResult = LoadRawActivitiesByRange(startDate, endDate);
            if (!sourceResult.IsSuccess || sourceResult.Value == null)
            {
                return OperationResult<IReadOnlyList<ActivityDailyBucket>>.Failure(sourceResult.Message, sourceResult.ErrorCode);
            }

            return OperationResult<IReadOnlyList<ActivityDailyBucket>>.Success(BuildDailyBuckets(startDate, endDate, sourceResult.Value));
        });
    }

    public OperationResult<ActivityReportData> GetReportData(DateTime startDate, DateTime endDate)
    {
        return _lockService.ExecuteLocked(() =>
        {
            var sourceResult = LoadRawActivitiesByRange(startDate, endDate);
            if (!sourceResult.IsSuccess || sourceResult.Value == null)
            {
                return OperationResult<ActivityReportData>.Failure(sourceResult.Message, sourceResult.ErrorCode);
            }

            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate.Date;
            var rawActivities = sourceResult.Value;

            return OperationResult<ActivityReportData>.Success(new ActivityReportData
            {
                Range = new(normalizedStartDate, normalizedEndDate),
                Activities = AggregateActivitiesByApplication(rawActivities),
                Summary = BuildSummary(rawActivities),
                DailyBuckets = BuildDailyBuckets(normalizedStartDate, normalizedEndDate, rawActivities)
            });
        });
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
        EnsureActivitiesEnriched();
        return BuildSummary(_todayActivities.Select(Clone).ToList());
    }

    private ActivitySummary BuildSummary(IReadOnlyList<AppActivity> activities)
    {
        if (activities.Count == 0)
        {
            return new ActivitySummary();
        }

        var metrics = _metricsService.Calculate(activities);
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

    private OperationResult<IReadOnlyList<AppActivity>> LoadTodayActivitiesInternal()
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
                return OperationResult<IReadOnlyList<AppActivity>>.Failure(saveResult.Message, saveResult.ErrorCode);
            }
        }

        return OperationResult<IReadOnlyList<AppActivity>>.Success(_todayActivities.Select(Clone).ToList());
    }

    private OperationResult<IReadOnlyList<AppActivity>> LoadRawActivitiesByRange(DateTime startDate, DateTime endDate)
    {
        var normalizedStartDate = startDate.Date;
        var normalizedEndDate = endDate.Date;

        if (normalizedStartDate > normalizedEndDate)
        {
            (normalizedStartDate, normalizedEndDate) = (normalizedEndDate, normalizedStartDate);
        }

        if (_todayActivities.Count == 0)
        {
            var todayLoadResult = LoadTodayActivitiesInternal();
            if (!todayLoadResult.IsSuccess)
            {
                return OperationResult<IReadOnlyList<AppActivity>>.Failure(todayLoadResult.Message, todayLoadResult.ErrorCode);
            }
        }

        var loadResult = _activityDataService.LoadByDateRange(normalizedStartDate, normalizedEndDate);
        if (!loadResult.IsSuccess || loadResult.Value == null)
        {
            return OperationResult<IReadOnlyList<AppActivity>>.Failure(loadResult.Message, loadResult.ErrorCode);
        }

        var rawActivities = loadResult.Value.Select(Clone).ToList();

        if (normalizedStartDate <= DateTime.Today && normalizedEndDate >= DateTime.Today)
        {
            rawActivities.RemoveAll(activity => activity.ActivityDate.Date == DateTime.Today);
            rawActivities.AddRange(_todayActivities.Select(Clone));
        }

        return OperationResult<IReadOnlyList<AppActivity>>.Success(rawActivities
            .OrderByDescending(activity => activity.ActivityDate)
            .ThenByDescending(activity => activity.TimeSpentSeconds)
            .ToList());
    }

    private static IReadOnlyList<AppActivity> AggregateActivitiesByApplication(IReadOnlyList<AppActivity> activities)
    {
        return activities
            .GroupBy(activity => string.IsNullOrWhiteSpace(activity.AppName)
                ? "Неизвестное приложение"
                : activity.AppName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var orderedGroup = group
                    .OrderByDescending(activity => activity.LastActive)
                    .ThenByDescending(activity => activity.TimeSpentSeconds)
                    .ToList();
                var latestActivity = orderedGroup[0];
                var dominantCategory = group
                    .GroupBy(activity => activity.Category)
                    .OrderByDescending(categoryGroup => categoryGroup.Sum(activity => activity.TimeSpentSeconds))
                    .Select(categoryGroup => categoryGroup.Key)
                    .FirstOrDefault(ActivityCategory.Unknown);

                return new AppActivity
                {
                    AppName = latestActivity.AppName,
                    WindowTitle = latestActivity.WindowTitle,
                    Category = dominantCategory,
                    ClassificationSource = latestActivity.ClassificationSource,
                    MatchedRuleId = latestActivity.MatchedRuleId,
                    MatchedRuleDescription = latestActivity.MatchedRuleDescription,
                    IsIdle = group.All(activity => activity.IsIdle),
                    IsProductive = group.Any(activity => activity.IsProductive),
                    TimeSpentSeconds = group.Sum(activity => activity.TimeSpentSeconds),
                    LastActive = group.Max(activity => activity.LastActive),
                    ActivityDate = group.Max(activity => activity.ActivityDate),
                    Version = latestActivity.Version
                };
            })
            .OrderByDescending(activity => activity.TimeSpentSeconds)
            .ThenBy(activity => activity.AppName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<ActivityDailyBucket> BuildDailyBuckets(DateTime startDate, DateTime endDate, IReadOnlyList<AppActivity> activities)
    {
        var bucketsByDate = activities
            .GroupBy(activity => activity.ActivityDate.Date)
            .ToDictionary(
                group => group.Key,
                group => new ActivityDailyBucket
                {
                    Date = group.Key,
                    TotalTrackedSeconds = group.Sum(activity => activity.TimeSpentSeconds),
                    ProductiveSeconds = group
                        .Where(activity => activity.IsProductive)
                        .Sum(activity => activity.TimeSpentSeconds)
                });

        var buckets = new List<ActivityDailyBucket>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (bucketsByDate.TryGetValue(date, out var bucket))
            {
                buckets.Add(bucket);
                continue;
            }

            buckets.Add(new ActivityDailyBucket
            {
                Date = date,
                TotalTrackedSeconds = 0,
                ProductiveSeconds = 0
            });
        }

        return buckets;
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
        var previousClassificationSource = activity.ClassificationSource;
        var previousMatchedRuleId = activity.MatchedRuleId;
        var previousMatchedRuleDescription = activity.MatchedRuleDescription;
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
        var classificationDecision = _classificationService.ClassifyDetailed(activity.AppName, activity.WindowTitle);
        activity.Category = classificationDecision.Category;
        activity.ClassificationSource = classificationDecision.Source;
        activity.MatchedRuleId = classificationDecision.MatchedRuleId;
        activity.MatchedRuleDescription = classificationDecision.MatchedRuleDescription ?? string.Empty;
        activity.IsProductive = ActivityProductivityRules.IsProductive(activity);

        return !string.Equals(previousWindowTitle, activity.WindowTitle, StringComparison.Ordinal) ||
            previousIsIdle != activity.IsIdle ||
            previousCategory != activity.Category ||
            previousClassificationSource != activity.ClassificationSource ||
            previousMatchedRuleId != activity.MatchedRuleId ||
            !string.Equals(previousMatchedRuleDescription, activity.MatchedRuleDescription, StringComparison.Ordinal) ||
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

        AppLogger.Log(
            $"Activity segment split: {DescribeSplitReason(_activeSegment, activity)}. " +
            $"PreviousApp='{_activeSegment.AppName}', CurrentApp='{activity.AppName}', " +
            $"PreviousCategory='{_activeSegment.Category}', CurrentCategory='{activity.Category}', " +
            $"PreviousIdle={_activeSegment.IsIdle}, CurrentIdle={activity.IsIdle}.",
            "DEBUG");

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
            current.IsIdle == activity.IsIdle &&
            current.Category == activity.Category;
    }

    private static string DescribeSplitReason(TrackedActivitySegment current, AppActivity activity)
    {
        var reasons = new List<string>();

        if (current.ActivityDate.Date != activity.ActivityDate.Date)
        {
            reasons.Add("ActivityDate");
        }

        if (!string.Equals(current.AppName, activity.AppName, StringComparison.Ordinal))
        {
            reasons.Add("AppName");
        }

        if (current.IsIdle != activity.IsIdle)
        {
            reasons.Add("IsIdle");
        }

        if (current.Category != activity.Category)
        {
            reasons.Add("Category");
        }

        return reasons.Count == 0 ? "Other" : string.Join(", ", reasons);
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
            ClassificationSource = source.ClassificationSource,
            MatchedRuleId = source.MatchedRuleId,
            MatchedRuleDescription = source.MatchedRuleDescription,
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

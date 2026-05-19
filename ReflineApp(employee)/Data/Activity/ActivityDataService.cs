using System.Text.Json;
using Refline.Data.Infrastructure;
using Refline.Models;
using System.IO;

namespace Refline.Data.Activity;

public class ActivityDataService : IActivityDataService
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ActivityDataService(string filePath = "activity_log.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public OperationResult<IReadOnlyList<AppActivity>> LoadByDate(DateTime activityDate)
    {
        try
        {
            lock (FileSync)
            {
                var all = ReadAllUnsafe();
                var filtered = all
                    .Where(a => a.ActivityDate.Date == activityDate.Date)
                    .OrderByDescending(a => a.TimeSpentSeconds)
                    .ToList();

                return OperationResult<IReadOnlyList<AppActivity>>.Success(filtered);
            }
        }
        catch (Exception ex)
        {
            return OperationResult<IReadOnlyList<AppActivity>>.Failure(
                $"Ошибка чтения активностей: {ex.Message}",
                "ACTIVITY_READ_ERROR");
        }
    }

    public OperationResult<IReadOnlyList<AppActivity>> LoadByDateRange(DateTime startDate, DateTime endDate)
    {
        try
        {
            lock (FileSync)
            {
                var normalizedStartDate = startDate.Date;
                var normalizedEndDate = endDate.Date;

                var filtered = ReadAllUnsafe()
                    .Where(a => a.ActivityDate.Date >= normalizedStartDate && a.ActivityDate.Date <= normalizedEndDate)
                    .OrderByDescending(a => a.ActivityDate)
                    .ThenByDescending(a => a.TimeSpentSeconds)
                    .ToList();

                return OperationResult<IReadOnlyList<AppActivity>>.Success(filtered);
            }
        }
        catch (Exception ex)
        {
            return OperationResult<IReadOnlyList<AppActivity>>.Failure(
                $"Ошибка чтения активностей за период: {ex.Message}",
                "ACTIVITY_RANGE_READ_ERROR");
        }
    }

    public OperationResult<AppActivity?> GetByAppAndDate(string appName, DateTime activityDate)
    {
        try
        {
            lock (FileSync)
            {
                var entity = ReadAllUnsafe().FirstOrDefault(a =>
                    string.Equals(a.AppName, appName, StringComparison.Ordinal) &&
                    a.ActivityDate.Date == activityDate.Date);

                return OperationResult<AppActivity?>.Success(entity);
            }
        }
        catch (Exception ex)
        {
            return OperationResult<AppActivity?>.Failure(
                $"Ошибка поиска активности: {ex.Message}",
                "ACTIVITY_LOOKUP_ERROR");
        }
    }

    public OperationResult SaveOrUpdate(AppActivity activity)
    {
        try
        {
            lock (FileSync)
            {
                var all = ReadAllUnsafe();
                var existing = all.FirstOrDefault(a =>
                    string.Equals(a.AppName, activity.AppName, StringComparison.Ordinal) &&
                    a.ActivityDate.Date == activity.ActivityDate.Date);

                if (existing == null)
                {
                    activity.Id = all.Count == 0 ? 1 : all.Max(a => a.Id) + 1;
                    activity.Version = 1;
                    all.Add(activity);
                }
                else
                {
                    if (activity.Version > 0 && existing.Version > 0 && activity.Version != existing.Version)
                    {
                        return OperationResult.Failure(
                            "Конфликт обновления активности. Попробуйте выполнить операцию повторно.",
                            "ACTIVITY_CONFLICT");
                    }

                    existing.TimeSpentSeconds = activity.TimeSpentSeconds;
                    existing.LastActive = activity.LastActive;
                    existing.ActivityDate = activity.ActivityDate.Date;
                    existing.WindowTitle = activity.WindowTitle;
                    existing.Category = activity.Category;
                    existing.ClassificationSource = activity.ClassificationSource;
                    existing.MatchedRuleId = activity.MatchedRuleId;
                    existing.MatchedRuleDescription = activity.MatchedRuleDescription;
                    existing.IsIdle = activity.IsIdle;
                    existing.IsProductive = activity.IsProductive;
                    existing.Version++;
                    activity.Version = existing.Version;
                    activity.Id = existing.Id;
                }

                WriteAllUnsafe(all);
                return OperationResult.Success();
            }
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(
                $"Ошибка сохранения активности: {ex.Message}",
                "ACTIVITY_SAVE_ERROR");
        }
    }

    public OperationResult SaveAll(IEnumerable<AppActivity> activities, DateTime activityDate)
    {
        try
        {
            lock (FileSync)
            {
                var all = ReadAllUnsafe();
                all.RemoveAll(a => a.ActivityDate.Date == activityDate.Date);

                var maxId = all.Count == 0 ? 0 : all.Max(a => a.Id);
                foreach (var activity in activities)
                {
                    maxId++;
                    activity.Id = maxId;
                    activity.ActivityDate = activityDate.Date;
                    activity.Version = Math.Max(activity.Version, 1);
                    all.Add(activity);
                }

                WriteAllUnsafe(all);
                return OperationResult.Success();
            }
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(
                $"Ошибка пакетного сохранения активностей: {ex.Message}",
                "ACTIVITY_BULK_SAVE_ERROR");
        }
    }

    private List<AppActivity> ReadAllUnsafe()
    {
        if (!File.Exists(_filePath))
        {
            return new List<AppActivity>();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<AppActivity>>(json) ?? new List<AppActivity>();
    }

    private void WriteAllUnsafe(List<AppActivity> activities)
    {
        var json = JsonSerializer.Serialize(activities, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}

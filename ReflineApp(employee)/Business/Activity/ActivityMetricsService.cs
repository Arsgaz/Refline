using Refline.Models;

namespace Refline.Business.Activity;

public class ActivityMetricsService : IActivityMetricsService
{
    public ActivityMetricsSummary Calculate(IReadOnlyList<AppActivity> activities)
    {
        if (activities.Count == 0)
        {
            return new ActivityMetricsSummary();
        }

        var totalTrackedSeconds = activities.Sum(activity => activity.TimeSpentSeconds);
        var idleSeconds = activities
            .Where(activity => activity.IsIdle)
            .Sum(activity => activity.TimeSpentSeconds);
        var productiveSeconds = activities
            .Where(activity => activity.IsProductive)
            .Sum(activity => activity.TimeSpentSeconds);

        var topApplications = activities
            .GroupBy(activity => string.IsNullOrWhiteSpace(activity.AppName)
                ? "Неизвестное приложение"
                : activity.AppName)
            .Select(group => new ActivityApplicationUsage
            {
                ApplicationName = group.Key,
                TotalSeconds = group.Sum(activity => activity.TimeSpentSeconds)
            })
            .OrderByDescending(item => item.TotalSeconds)
            .ThenBy(item => item.ApplicationName, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        var categorySeconds = activities
            .GroupBy(activity => activity.Category)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(activity => activity.TimeSpentSeconds));

        var knownCategorySeconds = categorySeconds
            .Where(item => item.Key != ActivityCategory.Unknown && item.Value > 0)
            .ToDictionary(item => item.Key, item => item.Value);

        var effectiveCategorySeconds = knownCategorySeconds.Count > 0
            ? knownCategorySeconds
            : categorySeconds;

        var topCategory = effectiveCategorySeconds
            .OrderByDescending(item => item.Value)
            .Select(item => item.Key)
            .FirstOrDefault(ActivityCategory.Unknown);

        return new ActivityMetricsSummary
        {
            TotalTrackedSeconds = totalTrackedSeconds,
            ActiveSeconds = Math.Max(0, totalTrackedSeconds - idleSeconds),
            IdleSeconds = idleSeconds,
            ProductiveSeconds = productiveSeconds,
            TopApplicationName = topApplications.FirstOrDefault()?.ApplicationName ?? "—",
            TopCategory = topCategory,
            CategorySeconds = effectiveCategorySeconds,
            TopApplications = topApplications
        };
    }
}

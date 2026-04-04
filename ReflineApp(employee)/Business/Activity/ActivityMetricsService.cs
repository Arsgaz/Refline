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

        var topApplicationName = activities
            .GroupBy(activity => string.IsNullOrWhiteSpace(activity.AppName)
                ? "Неизвестное приложение"
                : activity.AppName)
            .OrderByDescending(group => group.Sum(activity => activity.TimeSpentSeconds))
            .First()
            .Key;

        var topCategory = activities
            .GroupBy(activity => activity.Category)
            .OrderByDescending(group => group.Sum(activity => activity.TimeSpentSeconds))
            .First()
            .Key;

        return new ActivityMetricsSummary
        {
            TotalTrackedSeconds = totalTrackedSeconds,
            ActiveSeconds = Math.Max(0, totalTrackedSeconds - idleSeconds),
            IdleSeconds = idleSeconds,
            ProductiveSeconds = productiveSeconds,
            TopApplicationName = topApplicationName,
            TopCategory = topCategory
        };
    }
}

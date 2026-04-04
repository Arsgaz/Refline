using Refline.Models;

namespace Refline.Business.Activity;

public static class ActivityProductivityRules
{
    public const int MinProductiveActivitySeconds = 15;

    private static readonly HashSet<ActivityCategory> ProductiveCategories = new()
    {
        ActivityCategory.Work,
        ActivityCategory.Communication,
        ActivityCategory.ConditionalWork
    };

    public static bool IsProductive(AppActivity activity)
    {
        if (activity.IsIdle)
        {
            return false;
        }

        if (activity.TimeSpentSeconds < MinProductiveActivitySeconds)
        {
            return false;
        }

        return ProductiveCategories.Contains(activity.Category);
    }
}

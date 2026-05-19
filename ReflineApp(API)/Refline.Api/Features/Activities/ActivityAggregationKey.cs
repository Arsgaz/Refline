using Refline.Api.Entities;
using Refline.Api.Enums;

namespace Refline.Api.Features.Activities;

public readonly record struct ActivityAggregationKey(
    long UserId,
    string DeviceId,
    DateOnly ActivityDate,
    string AppName,
    ActivityCategory Category,
    bool IsIdle,
    bool IsProductive,
    string WindowTitleKey)
{
    public static ActivityAggregationKey FromRecord(ActivityRecord record)
    {
        return new ActivityAggregationKey(
            record.UserId,
            Normalize(record.DeviceId),
            record.ActivityDate,
            Normalize(record.AppName),
            record.Category,
            record.IsIdle,
            record.IsProductive,
            ActivityAggregationPolicy.ShouldUseWindowTitleInAggregation(record.AppName)
                ? Normalize(record.WindowTitle)
                : string.Empty);
    }

    public static ActivityAggregationKey Create(
        long userId,
        string deviceId,
        DateOnly activityDate,
        string appName,
        ActivityCategory category,
        bool isIdle,
        bool isProductive,
        string? windowTitle)
    {
        return new ActivityAggregationKey(
            userId,
            Normalize(deviceId),
            activityDate,
            Normalize(appName),
            category,
            isIdle,
            isProductive,
            ActivityAggregationPolicy.ShouldUseWindowTitleInAggregation(appName)
                ? Normalize(windowTitle)
                : string.Empty);
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim();
    }
}

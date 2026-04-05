namespace Refline.Api.Services.Admin;

public static class AdminAnalyticsPeriodValidator
{
    private const int MaxRangeDays = 366;

    public static string? Validate(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            return "'from' must be less than or equal to 'to'.";
        }

        var days = to.DayNumber - from.DayNumber;
        if (days > MaxRangeDays)
        {
            return "Date range cannot exceed one year.";
        }

        return null;
    }
}

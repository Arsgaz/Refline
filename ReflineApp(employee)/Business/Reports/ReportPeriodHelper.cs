using System.Globalization;
using Refline.Models;

namespace Refline.Business.Reports;

public static class ReportPeriodHelper
{
    private static readonly CultureInfo RussianCulture = new("ru-RU");

    public static ReportPeriodRange GetRange(ReportPeriod period, DateTime selectedDate)
    {
        var anchorDate = selectedDate.Date;

        return period switch
        {
            ReportPeriod.Day => new ReportPeriodRange(anchorDate, anchorDate),
            ReportPeriod.Week => CreateWeekRange(anchorDate),
            ReportPeriod.Month => CreateMonthRange(anchorDate),
            _ => new ReportPeriodRange(anchorDate, anchorDate)
        };
    }

    public static DateTime Move(ReportPeriod period, DateTime selectedDate, int offset)
    {
        var anchorDate = selectedDate.Date;

        return period switch
        {
            ReportPeriod.Day => anchorDate.AddDays(offset),
            ReportPeriod.Week => anchorDate.AddDays(offset * 7),
            ReportPeriod.Month => anchorDate.AddMonths(offset),
            _ => anchorDate
        };
    }

    public static string FormatLabel(ReportPeriod period, DateTime selectedDate)
    {
        var range = GetRange(period, selectedDate);

        return period switch
        {
            ReportPeriod.Day => range.StartDate.ToString("dd MMMM yyyy", RussianCulture),
            ReportPeriod.Week => $"{range.StartDate:dd.MM.yyyy} - {range.EndDate:dd.MM.yyyy}",
            ReportPeriod.Month => range.StartDate.ToString("MMMM yyyy", RussianCulture),
            _ => range.StartDate.ToString("dd.MM.yyyy", RussianCulture)
        };
    }

    public static string FormatNavigationCaption(ReportPeriod period)
    {
        return period switch
        {
            ReportPeriod.Day => "Сегодня",
            ReportPeriod.Week => "Текущая неделя",
            ReportPeriod.Month => "Текущий месяц",
            _ => "Сегодня"
        };
    }

    private static ReportPeriodRange CreateWeekRange(DateTime selectedDate)
    {
        var offset = ((int)selectedDate.DayOfWeek + 6) % 7;
        var startDate = selectedDate.AddDays(-offset);
        return new ReportPeriodRange(startDate, startDate.AddDays(6));
    }

    private static ReportPeriodRange CreateMonthRange(DateTime selectedDate)
    {
        var startDate = new DateTime(selectedDate.Year, selectedDate.Month, 1);
        return new ReportPeriodRange(startDate, startDate.AddMonths(1).AddDays(-1));
    }
}

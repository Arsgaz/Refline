namespace Refline.Business.Reports;

public readonly record struct ReportPeriodRange(DateTime StartDate, DateTime EndDate)
{
    public bool Contains(DateTime date)
    {
        var value = date.Date;
        return value >= StartDate.Date && value <= EndDate.Date;
    }
}

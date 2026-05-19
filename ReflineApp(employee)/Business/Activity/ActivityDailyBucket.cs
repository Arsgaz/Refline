namespace Refline.Business.Activity;

public sealed class ActivityDailyBucket
{
    public DateTime Date { get; init; }
    public int TotalTrackedSeconds { get; init; }
    public int ProductiveSeconds { get; init; }
}

namespace Refline.Admin.Models;

public sealed class UserAnalyticsSummary
{
    public long UserId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public int TotalTrackedSeconds { get; init; }

    public int ProductiveSeconds { get; init; }

    public int IdleSeconds { get; init; }

    public int ActiveSeconds { get; init; }

    public string? TopApplicationName { get; init; }

    public string? TopCategory { get; init; }

    public int TotalRecordsCount { get; init; }
}

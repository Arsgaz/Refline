namespace Refline.Admin.Models;

public sealed class TeamDashboardSnapshot
{
    public required IReadOnlyList<CompanyUserListItem> Users { get; init; }

    public required IReadOnlyList<TeamMemberAnalyticsItem> Members { get; init; }

    public required IReadOnlyList<TeamDailyAggregate> Days { get; init; }

    public required IReadOnlyList<TeamAggregateListItem> Applications { get; init; }

    public required IReadOnlyList<TeamAggregateListItem> Categories { get; init; }

    public int TotalTrackedSeconds { get; init; }

    public int TotalProductiveSeconds { get; init; }

    public int TotalIdleSeconds { get; init; }

    public int AverageProductiveSecondsPerEmployee { get; init; }

    public TeamMemberAnalyticsItem? TopEmployeeByProductiveTime { get; init; }
}

public sealed class TeamMemberAnalyticsItem
{
    public required CompanyUserListItem User { get; init; }

    public int TotalTrackedSeconds { get; init; }

    public int ProductiveSeconds { get; init; }

    public int IdleSeconds { get; init; }

    public int ActiveSeconds { get; init; }
}

public sealed class TeamDailyAggregate
{
    public DateOnly Date { get; init; }

    public int TotalSeconds { get; init; }

    public int ProductiveSeconds { get; init; }

    public int IdleSeconds { get; init; }
}

public sealed class TeamAggregateListItem
{
    public string Name { get; init; } = string.Empty;

    public int TotalSeconds { get; init; }
}

namespace Refline.Admin.Models;

public sealed class UserActivityBreakdown
{
    public long UserId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public IReadOnlyList<UserActivityApplication> Applications { get; init; } = [];

    public IReadOnlyList<UserActivityCategory> Categories { get; init; } = [];

    public IReadOnlyList<UserActivityDay> Days { get; init; } = [];
}

public sealed class UserActivityApplication
{
    public string ApplicationName { get; init; } = string.Empty;

    public int TotalSeconds { get; init; }
}

public sealed class UserActivityCategory
{
    public string Category { get; init; } = string.Empty;

    public int TotalSeconds { get; init; }
}

public sealed class UserActivityDay
{
    public DateOnly Date { get; init; }

    public int TotalSeconds { get; init; }

    public int ProductiveSeconds { get; init; }

    public int IdleSeconds { get; init; }
}

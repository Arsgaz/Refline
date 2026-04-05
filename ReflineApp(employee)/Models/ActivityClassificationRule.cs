namespace Refline.Models;

public sealed class ActivityClassificationRule
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string AppNamePattern { get; set; } = string.Empty;

    public string? WindowTitlePattern { get; set; }

    public ActivityCategory Category { get; set; } = ActivityCategory.Unknown;

    public int Priority { get; set; }

    public bool IsEnabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

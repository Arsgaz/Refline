using Refline.Api.Enums;

namespace Refline.Api.Entities;

public sealed class ActivityClassificationRule
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string AppNamePattern { get; set; } = string.Empty;

    public string? WindowTitlePattern { get; set; }

    public ActivityCategory Category { get; set; }

    public int Priority { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

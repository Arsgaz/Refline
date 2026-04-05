namespace Refline.Admin.Models;

public sealed class ActivityClassificationRuleCreateRequest
{
    public long CompanyId { get; init; }

    public string AppNamePattern { get; init; } = string.Empty;

    public string? WindowTitlePattern { get; init; }

    public ActivityCategory Category { get; init; }

    public int Priority { get; init; }

    public bool IsEnabled { get; init; }
}

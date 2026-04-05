using Refline.Api.Enums;

namespace Refline.Api.Contracts.Admin;

public sealed class UpdateActivityClassificationRuleRequestDto
{
    public long CompanyId { get; set; }

    public string AppNamePattern { get; set; } = string.Empty;

    public string? WindowTitlePattern { get; set; }

    public ActivityCategory Category { get; set; }

    public int Priority { get; set; }

    public bool IsEnabled { get; set; }
}

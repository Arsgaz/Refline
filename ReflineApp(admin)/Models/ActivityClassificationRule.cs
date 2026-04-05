namespace Refline.Admin.Models;

public sealed class ActivityClassificationRule
{
    public long Id { get; init; }

    public long CompanyId { get; init; }

    public string AppNamePattern { get; init; } = string.Empty;

    public string? WindowTitlePattern { get; init; }

    public ActivityCategory Category { get; init; }

    public int Priority { get; init; }

    public bool IsEnabled { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string WindowTitlePatternDisplay => string.IsNullOrWhiteSpace(WindowTitlePattern)
        ? "Любой заголовок"
        : WindowTitlePattern;

    public string CategoryDisplay => Category switch
    {
        ActivityCategory.Work => "Работа",
        ActivityCategory.Communication => "Коммуникация",
        ActivityCategory.ConditionalWork => "Условная работа",
        ActivityCategory.Entertainment => "Развлечения",
        ActivityCategory.System => "Системная",
        _ => "Неизвестно"
    };

    public string StatusDisplay => IsEnabled ? "Включено" : "Выключено";

    public string ToggleActionDisplay => IsEnabled ? "Выключить" : "Включить";

    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}

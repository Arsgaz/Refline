using Refline.Models;

namespace Refline.Business.Activity;

public class ActivityClassificationService : IActivityClassificationService
{
    private static readonly string[] WorkMarkers =
    {
        "refline", "refline employee", "аналитика рабочего времени",
        "analytics", "activity tracker",
        "visual studio", "visual studio code", "vs code", "code",
        "figma", "word", "excel", "powerpoint", "notion", "jira", "trello",
        "github desktop", "github", "cmd", "powershell", "windows terminal", "terminal",
        "chatgpt", "gemini", "copilot", "stack overflow",
        "google docs", "docs.google.com", "google sheets", "sheets.google.com",
        "google drive", "drive.google.com",
        "rider", "intellij", "pycharm", "webstorm"
    };

    private static readonly string[] CommunicationMarkers =
    {
        "telegram", "slack", "microsoft teams", "teams", "outlook", "discord", "zoom"
    };

    private static readonly string[] ConditionalWorkMarkers =
    {
        "chrome", "google chrome", "microsoft edge", "firefox",
        "opera", "explorer", "проводник", "browser"
    };

    private static readonly string[] EntertainmentMarkers =
    {
        "steam", "youtube", "youtu.be", "twitch", "netflix", "spotify", "vlc",
        "media player", "windows media player", "game", "games", "launcher",
        "epic games", "battle.net", "riot client", "dota", "counter-strike",
        "valorant", "minecraft", "roblox"
    };

    private static readonly string[] SystemMarkers =
    {
        "idle", "простой", "unknown/desktop", "lock screen", "screen saver",
        "заставка", "windows input experience", "program manager",
        "windows search", "windows settings", "параметры", "служебное окно"
    };

    public ActivityCategory Classify(string appName, string? windowTitle)
    {
        return ClassifyDetailed(appName, windowTitle).Category;
    }

    public ActivityClassificationDecision ClassifyDetailed(string appName, string? windowTitle)
    {
        var normalizedAppName = NormalizeText(appName);
        var normalizedTitle = NormalizeText(windowTitle);

        if (string.IsNullOrWhiteSpace(normalizedAppName) && string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return CreateDecision(ActivityCategory.Unknown, ActivityClassificationSource.FallbackUnknown);
        }

        if (ContainsReflineWorkContext(normalizedAppName, normalizedTitle))
        {
            return CreateDecision(ActivityCategory.Work, ActivityClassificationSource.BuiltIn);
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, SystemMarkers))
        {
            return CreateDecision(ActivityCategory.System, ActivityClassificationSource.BuiltIn);
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, WorkMarkers))
        {
            return CreateDecision(ActivityCategory.Work, ActivityClassificationSource.BuiltIn);
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, CommunicationMarkers))
        {
            return CreateDecision(ActivityCategory.Communication, ActivityClassificationSource.BuiltIn);
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, EntertainmentMarkers))
        {
            return CreateDecision(ActivityCategory.Entertainment, ActivityClassificationSource.BuiltIn);
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, ConditionalWorkMarkers))
        {
            return CreateDecision(ActivityCategory.ConditionalWork, ActivityClassificationSource.BuiltIn);
        }

        return CreateDecision(ActivityCategory.Unknown, ActivityClassificationSource.FallbackUnknown);
    }

    private static bool ContainsReflineWorkContext(string appName, string windowTitle)
    {
        return appName.Contains("refline", StringComparison.OrdinalIgnoreCase) ||
            appName.Contains("аналитика рабочего времени", StringComparison.OrdinalIgnoreCase) ||
            windowTitle.Contains("refline", StringComparison.OrdinalIgnoreCase) ||
            windowTitle.Contains("аналитика рабочего времени", StringComparison.OrdinalIgnoreCase);
    }

    public string NormalizeApplicationName(string windowTitle, bool isIdle)
    {
        if (isIdle)
        {
            return "Простой";
        }

        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            return "Неизвестное приложение";
        }

        var trimmedTitle = windowTitle.Trim();
        var separators = new[] { " - ", " — ", " | ", " :: " };

        foreach (var separator in separators)
        {
            var parts = trimmedTitle
                .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length >= 2)
            {
                return parts[^1];
            }
        }

        return trimmedTitle;
    }

    private static bool ContainsAny(string appName, string windowTitle, IEnumerable<string> markers)
    {
        return markers.Any(marker =>
            appName.Contains(marker, StringComparison.OrdinalIgnoreCase) ||
            windowTitle.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
                ' ',
                value.Trim().ToLowerInvariant()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Trim();
    }

    private static ActivityClassificationDecision CreateDecision(ActivityCategory category, ActivityClassificationSource source)
    {
        return new ActivityClassificationDecision
        {
            Category = category,
            Source = source
        };
    }
}

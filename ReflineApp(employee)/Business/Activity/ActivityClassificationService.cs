using Refline.Models;

namespace Refline.Business.Activity;

public class ActivityClassificationService : IActivityClassificationService
{
    private static readonly string[] WorkMarkers =
    {
        "refline", "refline employee", "аналитика рабочего времени",
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
        var normalizedAppName = NormalizeText(appName);
        var normalizedTitle = NormalizeText(windowTitle);

        if (string.IsNullOrWhiteSpace(normalizedAppName) && string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return ActivityCategory.Unknown;
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, SystemMarkers))
        {
            return ActivityCategory.System;
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, WorkMarkers))
        {
            return ActivityCategory.Work;
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, CommunicationMarkers))
        {
            return ActivityCategory.Communication;
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, EntertainmentMarkers))
        {
            return ActivityCategory.Entertainment;
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, ConditionalWorkMarkers))
        {
            return ActivityCategory.ConditionalWork;
        }

        return ActivityCategory.Unknown;
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
}

using Refline.Models;

namespace Refline.Business.Activity;

public class ActivityClassificationService : IActivityClassificationService
{
    private static readonly string[] WorkMarkers =
    {
        "visual studio", "visual studio code", "vs code", "figma", "word", "excel",
        "powerpoint", "notion", "jira", "terminal", "github desktop",
        "rider", "intellij", "pycharm", "webstorm"
    };

    private static readonly string[] CommunicationMarkers =
    {
        "telegram", "slack", "microsoft teams", "teams", "outlook", "discord", "zoom"
    };

    private static readonly string[] ConditionalWorkMarkers =
    {
        "chrome", "microsoft edge", "firefox", "explorer", "browser"
    };

    private static readonly string[] EntertainmentMarkers =
    {
        "steam", "spotify", "youtube", "netflix", "vlc", "game", "dota", "counter-strike"
    };

    private static readonly string[] SystemMarkers =
    {
        "idle", "unknown/desktop", "lock screen", "windows input experience",
        "program manager", "windows search", "windows settings"
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

        if (ContainsAny(normalizedAppName, normalizedTitle, ConditionalWorkMarkers))
        {
            return ActivityCategory.ConditionalWork;
        }

        if (ContainsAny(normalizedAppName, normalizedTitle, EntertainmentMarkers))
        {
            return ActivityCategory.Entertainment;
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
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}

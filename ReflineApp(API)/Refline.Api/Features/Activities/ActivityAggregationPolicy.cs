namespace Refline.Api.Features.Activities;

public static class ActivityAggregationPolicy
{
    private static readonly HashSet<string> BrowserLikeApplications = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "google chrome",
        "msedge",
        "microsoft edge",
        "edge",
        "firefox",
        "mozilla firefox",
        "opera",
        "opera gx",
        "brave",
        "brave browser",
        "browser"
    };

    public static bool ShouldUseWindowTitleInAggregation(string? appName)
    {
        var normalized = (appName ?? string.Empty).Trim();
        return BrowserLikeApplications.Contains(normalized);
    }

    public static string SelectStoredWindowTitle(string existingWindowTitle, string incomingWindowTitle)
    {
        return string.IsNullOrWhiteSpace(incomingWindowTitle)
            ? existingWindowTitle
            : incomingWindowTitle.Trim();
    }
}

using Refline.Models;

namespace Refline.Business.Activity;

public sealed class ActivityTickResult
{
    public string StatusText { get; init; } = string.Empty;
    public AppActivity? UpdatedActivity { get; init; }
    public bool IsNewActivity { get; init; }
    public ActivitySummary Summary { get; init; } = new();
    public bool IsTrackingSuppressed { get; init; }
}

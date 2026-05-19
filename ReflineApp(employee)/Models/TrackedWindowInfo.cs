namespace Refline.Models;

public sealed class TrackedWindowInfo
{
    public string WindowTitle { get; init; } = string.Empty;

    public string ProcessName { get; init; } = string.Empty;

    public string ExecutableName { get; init; } = string.Empty;

    public bool IsIdle { get; init; }

    public bool IsReflineOwnedWindow { get; init; }

    public string? IgnoreReason { get; init; }
}

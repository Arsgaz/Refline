using Refline.Api.Enums;

namespace Refline.Api.Entities;

public sealed class ActivityRecord
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public string DeviceId { get; set; } = string.Empty;

    public string AppName { get; set; } = string.Empty;

    public string WindowTitle { get; set; } = string.Empty;

    public ActivityCategory Category { get; set; }

    public bool IsIdle { get; set; }

    public bool IsProductive { get; set; }

    public int DurationSeconds { get; set; }

    public DateOnly ActivityDate { get; set; }

    public DateTimeOffset LastActiveAt { get; set; }
}

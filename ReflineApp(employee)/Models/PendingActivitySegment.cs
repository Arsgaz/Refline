namespace Refline.Models;

public sealed class PendingActivitySegment
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsIdle { get; set; }
    public bool IsProductive { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime ActivityDate { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public bool IsSynced { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSyncAttemptAt { get; set; }
    public int SyncAttemptCount { get; set; }
}

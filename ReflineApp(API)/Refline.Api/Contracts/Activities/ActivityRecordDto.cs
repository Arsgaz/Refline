namespace Refline.Api.Contracts.Activities;

public sealed class ActivityRecordDto
{
    public long UserId { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public string AppName { get; set; } = string.Empty;

    public string WindowTitle { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool IsIdle { get; set; }

    public bool IsProductive { get; set; }

    public int DurationSeconds { get; set; }

    public DateOnly ActivityDate { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset EndedAt { get; set; }
}

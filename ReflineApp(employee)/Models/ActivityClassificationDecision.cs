namespace Refline.Models;

public sealed class ActivityClassificationDecision
{
    public ActivityCategory Category { get; init; } = ActivityCategory.Unknown;

    public ActivityClassificationSource Source { get; init; } = ActivityClassificationSource.FallbackUnknown;

    public long? MatchedRuleId { get; init; }

    public string? MatchedRuleDescription { get; init; }
}

namespace Refline.Contracts.Activities;

public sealed class ActivityBatchRequestDto
{
    public List<ActivitySegmentDto> Records { get; set; } = new();
}

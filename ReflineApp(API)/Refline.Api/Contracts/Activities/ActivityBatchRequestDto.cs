namespace Refline.Api.Contracts.Activities;

public sealed class ActivityBatchRequestDto
{
    public List<ActivityRecordDto> Records { get; set; } = new();
}

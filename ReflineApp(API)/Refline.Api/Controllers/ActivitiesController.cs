using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Activities;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Enums;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/activities")]
public sealed class ActivitiesController(
    ReflineDbContext dbContext,
    ILogger<ActivitiesController> logger) : ControllerBase
{
    [HttpPost("batch")]
    public async Task<IActionResult> UploadBatch([FromBody] ActivityBatchRequestDto request)
    {
        if (request is null)
        {
            logger.LogWarning("Rejected activity batch: request body is null.");
            return BadRequest(new { message = "Request body is required." });
        }

        if (request.Records.Count == 0)
        {
            logger.LogWarning("Rejected activity batch: records collection is empty.");
            return BadRequest(new { message = "Records collection is empty." });
        }

        if (request.Records.Count > 1000)
        {
            logger.LogWarning(
                "Rejected activity batch: batch size {RecordCount} exceeds limit.",
                request.Records.Count);
            return BadRequest(new { message = "Batch size limit is 1000 records." });
        }

        var invalidRecord = request.Records.FirstOrDefault(record =>
            record.UserId <= 0 ||
            string.IsNullOrWhiteSpace(record.DeviceId) ||
            string.IsNullOrWhiteSpace(record.AppName) ||
            record.DurationSeconds < 0 ||
            record.EndedAt < record.StartedAt);

        if (invalidRecord is not null)
        {
            logger.LogWarning("Rejected activity batch: one or more records failed basic validation.");
            return BadRequest(new
            {
                message = "One or more records have invalid UserId, DeviceId, AppName, DurationSeconds or time range."
            });
        }

        logger.LogInformation("Received activity batch with {RecordCount} records.", request.Records.Count);

        var records = request.Records.Select(record => new ActivityRecord
        {
            UserId = record.UserId,
            DeviceId = record.DeviceId,
            AppName = record.AppName,
            WindowTitle = record.WindowTitle,
            Category = ParseCategory(record.Category),
            IsIdle = record.IsIdle,
            IsProductive = record.IsProductive,
            DurationSeconds = record.DurationSeconds,
            ActivityDate = record.ActivityDate,
            StartedAt = record.StartedAt,
            EndedAt = record.EndedAt
        }).ToList();

        dbContext.ActivityRecords.AddRange(records);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Saved {RecordCount} activity records.", records.Count);

        return Ok(new { insertedCount = request.Records.Count, message = "Activity batch saved." });
    }

    private static ActivityCategory ParseCategory(string category)
    {
        return Enum.TryParse<ActivityCategory>(category, true, out var parsedCategory)
            ? parsedCategory
            : ActivityCategory.Unknown;
    }
}

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

        var normalizedRecords = request.Records
            .Select(record =>
            {
                var startedAtUtc = record.StartedAt.ToUniversalTime();
                var endedAtUtc = record.EndedAt.ToUniversalTime();

                return new
                {
                    Record = record,
                    StartedAtUtc = startedAtUtc,
                    EndedAtUtc = endedAtUtc,
                    WasNormalized = record.StartedAt.Offset != TimeSpan.Zero || record.EndedAt.Offset != TimeSpan.Zero
                };
            })
            .ToList();

        var normalizedCount = normalizedRecords.Count(item => item.WasNormalized);
        if (normalizedCount > 0)
        {
            logger.LogWarning(
                "Normalized activity batch timestamps to UTC for {NormalizedCount} record(s).",
                normalizedCount);
        }

        var invalidRecord = normalizedRecords.FirstOrDefault(item =>
            item.Record.UserId <= 0 ||
            string.IsNullOrWhiteSpace(item.Record.DeviceId) ||
            string.IsNullOrWhiteSpace(item.Record.AppName) ||
            item.Record.DurationSeconds < 0 ||
            item.EndedAtUtc < item.StartedAtUtc);

        if (invalidRecord is not null)
        {
            logger.LogWarning("Rejected activity batch: one or more records failed basic validation after UTC normalization.");
            return BadRequest(new
            {
                message = "One or more records have invalid UserId, DeviceId, AppName, DurationSeconds or time range."
            });
        }

        logger.LogInformation("Received activity batch with {RecordCount} records.", request.Records.Count);

        var records = normalizedRecords.Select(item => new ActivityRecord
        {
            UserId = item.Record.UserId,
            DeviceId = item.Record.DeviceId,
            AppName = item.Record.AppName,
            WindowTitle = item.Record.WindowTitle,
            Category = ParseCategory(item.Record.Category),
            IsIdle = item.Record.IsIdle,
            IsProductive = item.Record.IsProductive,
            DurationSeconds = item.Record.DurationSeconds,
            ActivityDate = item.Record.ActivityDate,
            StartedAt = item.StartedAtUtc,
            EndedAt = item.EndedAtUtc
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

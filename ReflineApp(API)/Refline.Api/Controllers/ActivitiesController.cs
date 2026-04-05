using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Activities;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Enums;
using Refline.Api.Features.Activities;

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

        var incomingCategories = normalizedRecords
            .Select(item => item.Record.Category?.Trim())
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (incomingCategories.Length > 0)
        {
            logger.LogInformation(
                "Received activity batch categories: {Categories}.",
                string.Join(", ", incomingCategories));
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

        var incomingRecords = normalizedRecords.Select(item => new IncomingActivityRecord(
            item.Record.UserId,
            item.Record.DeviceId,
            item.Record.AppName,
            item.Record.WindowTitle,
            ParseCategory(item.Record.Category, logger),
            item.Record.IsIdle,
            item.Record.IsProductive,
            item.Record.DurationSeconds,
            item.Record.ActivityDate,
            item.StartedAtUtc,
            item.EndedAtUtc))
            .ToList();

        var userIds = incomingRecords.Select(item => item.UserId).Distinct().ToArray();
        var deviceIds = incomingRecords.Select(item => item.DeviceId).Distinct(StringComparer.Ordinal).ToArray();
        var activityDates = incomingRecords.Select(item => item.ActivityDate).Distinct().ToArray();

        var existingRecords = await dbContext.ActivityRecords
            .Where(record =>
                userIds.Contains(record.UserId) &&
                deviceIds.Contains(record.DeviceId) &&
                activityDates.Contains(record.ActivityDate))
            .ToListAsync();

        var recordsByKey = existingRecords
            .GroupBy(ActivityAggregationKey.FromRecord)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(record => record.EndedAt).First());

        var createdCount = 0;
        var mergedCount = 0;

        foreach (var incomingRecord in incomingRecords)
        {
            var aggregationKey = incomingRecord.GetAggregationKey();
            if (recordsByKey.TryGetValue(aggregationKey, out var existingRecord))
            {
                MergeIncomingRecord(existingRecord, incomingRecord);
                mergedCount++;

                logger.LogDebug(
                    "Merged activity record for app '{AppName}' with strategy '{Strategy}'.",
                    incomingRecord.AppName,
                    ActivityAggregationPolicy.ShouldUseWindowTitleInAggregation(incomingRecord.AppName)
                        ? "BrowserLikeWithWindowTitle"
                        : "DefaultWithoutWindowTitle");
                continue;
            }

            var createdRecord = incomingRecord.ToEntity();
            dbContext.ActivityRecords.Add(createdRecord);
            recordsByKey[aggregationKey] = createdRecord;
            createdCount++;

            logger.LogDebug(
                "Created activity record for app '{AppName}' with strategy '{Strategy}'.",
                incomingRecord.AppName,
                ActivityAggregationPolicy.ShouldUseWindowTitleInAggregation(incomingRecord.AppName)
                    ? "BrowserLikeWithWindowTitle"
                    : "DefaultWithoutWindowTitle");
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Processed activity batch: received {ReceivedCount}, created {CreatedCount}, merged {MergedCount}.",
            request.Records.Count,
            createdCount,
            mergedCount);

        return Ok(new { insertedCount = request.Records.Count, message = "Activity batch saved." });
    }

    private static ActivityCategory ParseCategory(string? category, ILogger logger)
    {
        var normalized = (category ?? string.Empty).Trim();

        return normalized switch
        {
            nameof(ActivityCategory.Work) => ActivityCategory.Work,
            nameof(ActivityCategory.Communication) => ActivityCategory.Communication,
            nameof(ActivityCategory.ConditionalWork) => ActivityCategory.ConditionalWork,
            nameof(ActivityCategory.Entertainment) => ActivityCategory.Entertainment,
            nameof(ActivityCategory.System) => ActivityCategory.System,
            nameof(ActivityCategory.Unknown) => ActivityCategory.Unknown,
            _ => LogUnknownCategory(normalized, logger)
        };
    }

    private static ActivityCategory LogUnknownCategory(string category, ILogger logger)
    {
        logger.LogWarning(
            "Unknown activity category '{Category}' received in batch. Falling back to Unknown.",
            string.IsNullOrWhiteSpace(category) ? "<empty>" : category);

        return ActivityCategory.Unknown;
    }

    private static void MergeIncomingRecord(ActivityRecord existingRecord, IncomingActivityRecord incomingRecord)
    {
        existingRecord.DurationSeconds += incomingRecord.DurationSeconds;
        existingRecord.StartedAt = incomingRecord.StartedAt < existingRecord.StartedAt
            ? incomingRecord.StartedAt
            : existingRecord.StartedAt;
        existingRecord.EndedAt = incomingRecord.EndedAt > existingRecord.EndedAt
            ? incomingRecord.EndedAt
            : existingRecord.EndedAt;
        existingRecord.WindowTitle = ActivityAggregationPolicy.SelectStoredWindowTitle(
            existingRecord.WindowTitle,
            incomingRecord.WindowTitle);
    }

    private sealed record IncomingActivityRecord(
        long UserId,
        string DeviceId,
        string AppName,
        string WindowTitle,
        ActivityCategory Category,
        bool IsIdle,
        bool IsProductive,
        int DurationSeconds,
        DateOnly ActivityDate,
        DateTimeOffset StartedAt,
        DateTimeOffset EndedAt)
    {
        public ActivityAggregationKey GetAggregationKey()
        {
            return ActivityAggregationKey.Create(
                UserId,
                DeviceId,
                ActivityDate,
                AppName,
                Category,
                IsIdle,
                IsProductive,
                WindowTitle);
        }

        public ActivityRecord ToEntity()
        {
            return new ActivityRecord
            {
                UserId = UserId,
                DeviceId = DeviceId,
                AppName = AppName,
                WindowTitle = string.IsNullOrWhiteSpace(WindowTitle) ? AppName : WindowTitle.Trim(),
                Category = Category,
                IsIdle = IsIdle,
                IsProductive = IsProductive,
                DurationSeconds = DurationSeconds,
                ActivityDate = ActivityDate,
                StartedAt = StartedAt,
                EndedAt = EndedAt
            };
        }
    }
}

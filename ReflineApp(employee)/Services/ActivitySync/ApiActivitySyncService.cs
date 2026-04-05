using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Refline.Contracts.Activities;
using Refline.Data.Activity;
using Refline.Data.Infrastructure;
using Refline.Models;
using Refline.Utils;

namespace Refline.Services.ActivitySync;

public sealed class ApiActivitySyncService : IActivitySyncService
{
    private const int MaxBatchSize = 500;

    private readonly HttpClient _httpClient;
    private readonly IPendingActivityStore _pendingActivityStore;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public ApiActivitySyncService(HttpClient httpClient, IPendingActivityStore pendingActivityStore)
    {
        _httpClient = httpClient;
        _pendingActivityStore = pendingActivityStore;
    }

    public async Task<OperationResult<int>> TrySyncPendingAsync(CancellationToken cancellationToken = default)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            return OperationResult<int>.Success(0, "Синхронизация уже выполняется.");
        }

        try
        {
            var pendingResult = await _pendingActivityStore.GetPendingAsync();
            if (!pendingResult.IsSuccess || pendingResult.Value == null)
            {
                AppLogger.Log(pendingResult.Message, "ERROR");
                return OperationResult<int>.Failure(pendingResult.Message, pendingResult.ErrorCode);
            }

            var pendingSegments = pendingResult.Value;
            if (pendingSegments.Count == 0)
            {
                return OperationResult<int>.Success(0, "Нет pending activity segments для sync.");
            }

            var syncedCount = 0;
            foreach (var batch in pendingSegments.Chunk(MaxBatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchList = batch.ToList();
                var batchIds = batchList.Select(item => item.Id).ToArray();
                var markAttemptResult = await _pendingActivityStore.RegisterSyncAttemptAsync(batchIds, DateTimeOffset.UtcNow);
                if (!markAttemptResult.IsSuccess)
                {
                    AppLogger.Log(markAttemptResult.Message, "ERROR");
                    return OperationResult<int>.Failure(markAttemptResult.Message, markAttemptResult.ErrorCode);
                }

                try
                {
                    using var response = await _httpClient.PostAsJsonAsync(
                        "api/activities/batch",
                        new ActivityBatchRequestDto
                        {
                            Records = batchList.Select(MapToDto).ToList()
                        },
                        _jsonOptions,
                        cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync(cancellationToken);
                        var message = string.IsNullOrWhiteSpace(error)
                            ? $"Ошибка sync activity batch: HTTP {(int)response.StatusCode}."
                            : $"Ошибка sync activity batch: HTTP {(int)response.StatusCode}. {error}";
                        AppLogger.Log(message, "ERROR");
                        return OperationResult<int>.Failure(message, $"HTTP_{(int)response.StatusCode}");
                    }

                    var markSyncedResult = await _pendingActivityStore.MarkAsSyncedAsync(batchIds);
                    if (!markSyncedResult.IsSuccess)
                    {
                        AppLogger.Log(markSyncedResult.Message, "ERROR");
                        return OperationResult<int>.Failure(markSyncedResult.Message, markSyncedResult.ErrorCode);
                    }

                    syncedCount += batchList.Count;
                    AppLogger.Log($"Activity sync succeeded for {batchList.Count} segment(s).");
                }
                catch (HttpRequestException ex)
                {
                    var message = $"Activity sync skipped: API недоступен. {ex.Message}";
                    AppLogger.Log(message, "ERROR");
                    return OperationResult<int>.Failure(message, "API_UNAVAILABLE");
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var message = $"Activity sync timed out: {ex.Message}";
                    AppLogger.Log(message, "ERROR");
                    return OperationResult<int>.Failure(message, "API_TIMEOUT");
                }
            }

            var cleanupResult = await _pendingActivityStore.RemoveSyncedAsync();
            if (!cleanupResult.IsSuccess)
            {
                AppLogger.Log(cleanupResult.Message, "ERROR");
                return OperationResult<int>.Failure(cleanupResult.Message, cleanupResult.ErrorCode);
            }

            return OperationResult<int>.Success(syncedCount, $"Синхронизировано сегментов: {syncedCount}.");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private static ActivitySegmentDto MapToDto(PendingActivitySegment segment)
    {
        var startedAtUtc = segment.StartedAt.ToUniversalTime();
        var endedAtUtc = segment.EndedAt.ToUniversalTime();

        return new ActivitySegmentDto
        {
            UserId = segment.UserId,
            DeviceId = segment.DeviceId,
            AppName = segment.AppName,
            WindowTitle = segment.WindowTitle,
            Category = segment.Category,
            IsIdle = segment.IsIdle,
            IsProductive = segment.IsProductive,
            DurationSeconds = segment.DurationSeconds,
            ActivityDate = DateOnly.FromDateTime(segment.ActivityDate),
            StartedAt = startedAtUtc,
            EndedAt = endedAtUtc
        };
    }
}

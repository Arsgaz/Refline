using System.IO;
using System.Text.Json;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Activity;

public sealed class LocalPendingActivityStore : IPendingActivityStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public LocalPendingActivityStore(string filePath = "pending_activity_segments.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<IReadOnlyList<PendingActivitySegment>>> LoadAsync()
    {
        try
        {
            lock (FileSync)
            {
                return Task.FromResult(OperationResult<IReadOnlyList<PendingActivitySegment>>.Success(ReadAllUnsafe()));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<PendingActivitySegment>>.Failure(
                $"Ошибка чтения pending activity segments: {ex.Message}",
                "PENDING_ACTIVITY_READ_ERROR"));
        }
    }

    public Task<OperationResult> SaveAsync(IEnumerable<PendingActivitySegment> segments)
    {
        try
        {
            lock (FileSync)
            {
                var normalized = segments
                    .Select(Clone)
                    .OrderBy(item => item.Id)
                    .ToList();

                WriteAllUnsafe(normalized);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка сохранения pending activity segments: {ex.Message}",
                "PENDING_ACTIVITY_SAVE_ERROR"));
        }
    }

    public Task<OperationResult> AddAsync(PendingActivitySegment segment)
    {
        return AddRangeAsync(new[] { segment });
    }

    public Task<OperationResult> AddRangeAsync(IEnumerable<PendingActivitySegment> segments)
    {
        try
        {
            lock (FileSync)
            {
                var all = ReadAllUnsafe();
                var nextId = all.Count == 0 ? 1L : all.Max(item => item.Id) + 1L;

                foreach (var segment in segments)
                {
                    var copy = Clone(segment);
                    copy.Id = copy.Id > 0 ? copy.Id : nextId++;
                    copy.IsSynced = false;
                    all.Add(copy);
                }

                WriteAllUnsafe(all);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка добавления pending activity segments: {ex.Message}",
                "PENDING_ACTIVITY_ADD_ERROR"));
        }
    }

    public Task<OperationResult<IReadOnlyList<PendingActivitySegment>>> GetPendingAsync()
    {
        try
        {
            lock (FileSync)
            {
                var pending = ReadAllUnsafe()
                    .Where(item => !item.IsSynced)
                    .OrderBy(item => item.CreatedAt)
                    .ThenBy(item => item.Id)
                    .ToList();

                return Task.FromResult(OperationResult<IReadOnlyList<PendingActivitySegment>>.Success(pending));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<PendingActivitySegment>>.Failure(
                $"Ошибка чтения очереди activity sync: {ex.Message}",
                "PENDING_ACTIVITY_PENDING_READ_ERROR"));
        }
    }

    public Task<OperationResult> RegisterSyncAttemptAsync(IEnumerable<long> ids, DateTimeOffset attemptedAt)
    {
        try
        {
            lock (FileSync)
            {
                var idSet = ids.ToHashSet();
                if (idSet.Count == 0)
                {
                    return Task.FromResult(OperationResult.Success());
                }

                var all = ReadAllUnsafe();
                foreach (var item in all.Where(item => idSet.Contains(item.Id) && !item.IsSynced))
                {
                    item.LastSyncAttemptAt = attemptedAt;
                    item.SyncAttemptCount++;
                }

                WriteAllUnsafe(all);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка обновления попытки sync: {ex.Message}",
                "PENDING_ACTIVITY_SYNC_ATTEMPT_ERROR"));
        }
    }

    public Task<OperationResult> MarkAsSyncedAsync(IEnumerable<long> ids)
    {
        try
        {
            lock (FileSync)
            {
                var idSet = ids.ToHashSet();
                if (idSet.Count == 0)
                {
                    return Task.FromResult(OperationResult.Success());
                }

                var all = ReadAllUnsafe();
                foreach (var item in all.Where(item => idSet.Contains(item.Id)))
                {
                    item.IsSynced = true;
                }

                WriteAllUnsafe(all);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка пометки synced activity segments: {ex.Message}",
                "PENDING_ACTIVITY_MARK_SYNCED_ERROR"));
        }
    }

    public Task<OperationResult> RemoveSyncedAsync()
    {
        try
        {
            lock (FileSync)
            {
                var all = ReadAllUnsafe();
                all.RemoveAll(item => item.IsSynced);
                WriteAllUnsafe(all);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка очистки synced activity segments: {ex.Message}",
                "PENDING_ACTIVITY_REMOVE_SYNCED_ERROR"));
        }
    }

    public Task<OperationResult> ClearAsync()
    {
        return SaveAsync(Array.Empty<PendingActivitySegment>());
    }

    private List<PendingActivitySegment> ReadAllUnsafe()
    {
        if (!File.Exists(_filePath))
        {
            return new List<PendingActivitySegment>();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<PendingActivitySegment>>(json) ?? new List<PendingActivitySegment>();
    }

    private void WriteAllUnsafe(List<PendingActivitySegment> segments)
    {
        var json = JsonSerializer.Serialize(segments, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static PendingActivitySegment Clone(PendingActivitySegment source)
    {
        return new PendingActivitySegment
        {
            Id = source.Id,
            UserId = source.UserId,
            DeviceId = source.DeviceId,
            AppName = source.AppName,
            WindowTitle = source.WindowTitle,
            Category = source.Category,
            IsIdle = source.IsIdle,
            IsProductive = source.IsProductive,
            DurationSeconds = source.DurationSeconds,
            ActivityDate = source.ActivityDate,
            StartedAt = source.StartedAt,
            EndedAt = source.EndedAt,
            IsSynced = source.IsSynced,
            CreatedAt = source.CreatedAt,
            LastSyncAttemptAt = source.LastSyncAttemptAt,
            SyncAttemptCount = source.SyncAttemptCount
        };
    }
}

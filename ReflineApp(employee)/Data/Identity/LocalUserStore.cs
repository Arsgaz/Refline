using System.IO;
using System.Text.Json;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public class LocalUserStore : IUserStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public LocalUserStore(string filePath = "users.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<User?>> GetByLoginAsync(string login)
    {
        try
        {
            lock (FileSync)
            {
                var normalizedLogin = (login ?? string.Empty).Trim();
                var user = ReadAllUnsafe()
                    .FirstOrDefault(u => string.Equals(u.Login, normalizedLogin, StringComparison.OrdinalIgnoreCase));

                return Task.FromResult(OperationResult<User?>.Success(user));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<User?>.Failure(
                $"Ошибка чтения пользователя: {ex.Message}",
                "USER_READ_ERROR"));
        }
    }

    public Task<OperationResult<User?>> GetByIdAsync(Guid userId)
    {
        try
        {
            lock (FileSync)
            {
                var user = ReadAllUnsafe().FirstOrDefault(u => u.Id == userId);
                return Task.FromResult(OperationResult<User?>.Success(user));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<User?>.Failure(
                $"Ошибка поиска пользователя: {ex.Message}",
                "USER_LOOKUP_ERROR"));
        }
    }

    public Task<OperationResult<IReadOnlyList<User>>> GetAllAsync()
    {
        try
        {
            lock (FileSync)
            {
                IReadOnlyList<User> users = ReadAllUnsafe();
                return Task.FromResult(OperationResult<IReadOnlyList<User>>.Success(users));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<User>>.Failure(
                $"Ошибка чтения пользователей: {ex.Message}",
                "USER_LIST_ERROR"));
        }
    }

    public Task<OperationResult> SaveAllAsync(IEnumerable<User> users)
    {
        try
        {
            lock (FileSync)
            {
                WriteAllUnsafe(users.ToList());
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка сохранения пользователей: {ex.Message}",
                "USER_SAVE_ERROR"));
        }
    }

    private List<User> ReadAllUnsafe()
    {
        if (!File.Exists(_filePath))
        {
            var seedUsers = IdentitySeedData.CreateUsers();
            WriteAllUnsafe(seedUsers);
            return seedUsers;
        }

        var json = File.ReadAllText(_filePath);
        var users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();

        if (users.Count == 0)
        {
            users = IdentitySeedData.CreateUsers();
            WriteAllUnsafe(users);
        }

        return users;
    }

    private void WriteAllUnsafe(List<User> users)
    {
        var json = JsonSerializer.Serialize(users, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public sealed class ApiAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ICurrentUserSessionStore _sessionStore;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiAuthenticationService(
        HttpClient httpClient,
        ICurrentUserContext currentUserContext,
        ICurrentUserSessionStore sessionStore)
    {
        _httpClient = httpClient;
        _currentUserContext = currentUserContext;
        _sessionStore = sessionStore;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public Task<OperationResult<User?>> GetUserByLoginAsync(string login)
    {
        var normalizedLogin = (login ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedLogin))
        {
            return Task.FromResult(OperationResult<User?>.Success(null));
        }

        var currentUser = _sessionStore.GetCurrentUser();
        if (currentUser != null &&
            string.Equals(currentUser.Login, normalizedLogin, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(OperationResult<User?>.Success(currentUser));
        }

        return Task.FromResult(OperationResult<User?>.Success(null));
    }

    public async Task<OperationResult<bool>> ValidateCredentialsAsync(string login, string password)
    {
        var normalizedLogin = (login ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedLogin))
        {
            await ClearSessionAsync();
            return OperationResult<bool>.Success(false, "Неверный логин или пароль.");
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequestDto
                {
                    Login = normalizedLogin,
                    Password = password ?? string.Empty
                },
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
                if (loginResponse == null)
                {
                    await ClearSessionAsync();
                    return OperationResult<bool>.Failure("API вернул пустой ответ авторизации.", "API_EMPTY_RESPONSE");
                }

                var user = MapUser(loginResponse);
                var saveSessionResult = await _sessionStore.SetCurrentUserAsync(user);
                if (!saveSessionResult.IsSuccess)
                {
                    _currentUserContext.Clear();
                    return OperationResult<bool>.Failure(saveSessionResult.Message, saveSessionResult.ErrorCode);
                }

                _currentUserContext.SetCurrentUser(user.Id);

                return OperationResult<bool>.Success(true, "OK");
            }

            await ClearSessionAsync();
            var errorMessage = await ReadErrorMessageAsync(response);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.BadRequest)
            {
                return OperationResult<bool>.Success(false, errorMessage);
            }

            return OperationResult<bool>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            await ClearSessionAsync();
            return OperationResult<bool>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            await ClearSessionAsync();
            return OperationResult<bool>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public Task<OperationResult<User?>> GetCurrentUserAsync()
    {
        return Task.FromResult(OperationResult<User?>.Success(_sessionStore.GetCurrentUser()));
    }

    public async Task<OperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (userId == Guid.Empty)
        {
            return OperationResult.Failure("Не удалось определить пользователя для смены пароля.");
        }

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return OperationResult.Failure("Текущий и новый пароль обязательны.");
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/change-password",
                new ChangePasswordRequestDto
                {
                    UserId = ApiIdentityIdMapper.ToServerId(userId),
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                },
                _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                {
                    return OperationResult.Failure(errorMessage);
                }

                return OperationResult.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var currentUser = _sessionStore.GetCurrentUser();
            if (currentUser != null && currentUser.Id == userId)
            {
                currentUser.MustChangePassword = false;
                var saveResult = await _sessionStore.SetCurrentUserAsync(currentUser);
                if (!saveResult.IsSuccess)
                {
                    return OperationResult.Failure(saveResult.Message, saveResult.ErrorCode);
                }
            }

            return OperationResult.Success();
        }
        catch (HttpRequestException ex)
        {
            return OperationResult.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    private static User MapUser(LoginResponseDto response)
    {
        var userId = ApiIdentityIdMapper.ToLocalGuid(response.UserId);

        return new User
        {
            Id = userId,
            CompanyId = ApiIdentityIdMapper.ToLocalGuid(response.CompanyId),
            FullName = response.FullName ?? string.Empty,
            Login = response.Login ?? string.Empty,
            Role = response.Role,
            MustChangePassword = response.MustChangePassword,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_jsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
        }

        var fallback = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(fallback)
            ? $"Ошибка API: HTTP {(int)response.StatusCode}."
            : fallback;
    }

    private async Task ClearSessionAsync()
    {
        await _sessionStore.ClearAsync();
        _currentUserContext.Clear();
    }

    private sealed class LoginRequestDto
    {
        public string Login { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    private sealed class LoginResponseDto
    {
        public long UserId { get; set; }

        public long CompanyId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Login { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public bool MustChangePassword { get; set; }
    }

    private sealed class ChangePasswordRequestDto
    {
        public long UserId { get; set; }

        public string CurrentPassword { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}

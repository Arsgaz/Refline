using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Business.Identity;

public sealed class AdminAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly CurrentSessionContext _currentSessionContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdminAuthenticationService(HttpClient httpClient, CurrentSessionContext currentSessionContext)
    {
        _httpClient = httpClient;
        _currentSessionContext = currentSessionContext;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<AdminUser>> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        var normalizedLogin = (login ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedLogin) || string.IsNullOrWhiteSpace(password))
        {
            _currentSessionContext.Clear();
            return OperationResult<AdminUser>.Failure("Введите логин и пароль.");
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequestDto
                {
                    Login = normalizedLogin,
                    Password = password
                },
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _currentSessionContext.Clear();
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
                {
                    return OperationResult<AdminUser>.Failure(errorMessage);
                }

                return OperationResult<AdminUser>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions, cancellationToken);
            if (loginResponse is null)
            {
                _currentSessionContext.Clear();
                return OperationResult<AdminUser>.Failure("API вернул пустой ответ авторизации.", "API_EMPTY_RESPONSE");
            }

            var user = new AdminUser
            {
                Id = loginResponse.UserId,
                CompanyId = loginResponse.CompanyId,
                FullName = loginResponse.FullName,
                Login = loginResponse.Login,
                Role = loginResponse.Role,
                MustChangePassword = loginResponse.MustChangePassword
            };

            if (!RoleAccessPolicy.CanAccessAdminApp(user.Role))
            {
                _currentSessionContext.Clear();
                return OperationResult<AdminUser>.Failure("У пользователя нет доступа к админскому приложению.");
            }

            _currentSessionContext.SetCurrentUser(user);
            return OperationResult<AdminUser>.Success(user);
        }
        catch (HttpRequestException ex)
        {
            _currentSessionContext.Clear();
            return OperationResult<AdminUser>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            _currentSessionContext.Clear();
            return OperationResult<AdminUser>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult> ChangePasswordAsync(
        long userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
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
                    UserId = userId,
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                },
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                {
                    return OperationResult.Failure(errorMessage);
                }

                return OperationResult.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            if (_currentSessionContext.CurrentUser is not null && _currentSessionContext.CurrentUser.Id == userId)
            {
                _currentSessionContext.CurrentUser.MustChangePassword = false;
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

    private async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_jsonOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
        }

        var fallback = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(fallback)
            ? $"Ошибка API: HTTP {(int)response.StatusCode}."
            : fallback;
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

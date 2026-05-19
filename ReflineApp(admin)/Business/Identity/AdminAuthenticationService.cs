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
    private readonly AdminApiAuthorizationService _apiAuthorizationService;
    private readonly CurrentSessionContext _currentSessionContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdminAuthenticationService(
        HttpClient httpClient,
        AdminApiAuthorizationService apiAuthorizationService,
        CurrentSessionContext currentSessionContext)
    {
        _httpClient = httpClient;
        _apiAuthorizationService = apiAuthorizationService;
        _currentSessionContext = currentSessionContext;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<AdminUser>> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        var normalizedLogin = (login ?? string.Empty).Trim();
        await LogoutAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(normalizedLogin) || string.IsNullOrWhiteSpace(password))
        {
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
                await LogoutAsync(cancellationToken);
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
                await LogoutAsync(cancellationToken);
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
                await LogoutAsync(cancellationToken);
                return OperationResult<AdminUser>.Failure("У пользователя нет доступа к админскому приложению.");
            }

            var saveSessionResult = await _currentSessionContext.SetSessionAsync(
                user,
                new ApiTokenSet
                {
                    AccessToken = loginResponse.AccessToken,
                    AccessTokenExpiresAt = loginResponse.AccessTokenExpiresAt,
                    RefreshToken = loginResponse.RefreshToken,
                    RefreshTokenExpiresAt = loginResponse.RefreshTokenExpiresAt
                });

            if (!saveSessionResult.IsSuccess)
            {
                return OperationResult<AdminUser>.Failure(saveSessionResult.Message, saveSessionResult.ErrorCode);
            }

            _apiAuthorizationService.SetAuthorizationHeader(loginResponse.AccessToken);
            return OperationResult<AdminUser>.Success(user);
        }
        catch (HttpRequestException ex)
        {
            await LogoutAsync(cancellationToken);
            return OperationResult<AdminUser>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            await LogoutAsync(cancellationToken);
            return OperationResult<AdminUser>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        _apiAuthorizationService.ClearAuthorizationHeader();
        return await _currentSessionContext.ClearAsync();
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
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password")
            {
                Content = JsonContent.Create(
                    new ChangePasswordRequestDto
                    {
                        UserId = userId,
                        CurrentPassword = currentPassword,
                        NewPassword = newPassword
                    },
                    options: _jsonOptions)
            };

            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

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
                if (_currentSessionContext.CurrentSession is not null)
                {
                    var updateSessionResult = await _currentSessionContext.SetSessionAsync(
                        _currentSessionContext.CurrentUser,
                        new ApiTokenSet
                        {
                            AccessToken = _currentSessionContext.CurrentSession.AccessToken,
                            AccessTokenExpiresAt = _currentSessionContext.CurrentSession.AccessTokenExpiresAt,
                            RefreshToken = _currentSessionContext.CurrentSession.RefreshToken,
                            RefreshTokenExpiresAt = _currentSessionContext.CurrentSession.RefreshTokenExpiresAt
                        });

                    if (!updateSessionResult.IsSuccess)
                    {
                        return OperationResult.Failure(updateSessionResult.Message, updateSessionResult.ErrorCode);
                    }

                    _apiAuthorizationService.SetAuthorizationHeader(_currentSessionContext.CurrentSession.AccessToken);
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
        public string AccessToken { get; set; } = string.Empty;

        public DateTimeOffset AccessTokenExpiresAt { get; set; }

        public string RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset RefreshTokenExpiresAt { get; set; }

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

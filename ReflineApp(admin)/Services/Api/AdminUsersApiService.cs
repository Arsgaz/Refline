using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public sealed class AdminUsersApiService : IAdminUsersService
{
    private readonly HttpClient _httpClient;
    private readonly Business.Identity.AdminApiAuthorizationService _apiAuthorizationService;
    private readonly Business.Identity.CurrentSessionContext _currentSessionContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdminUsersApiService(
        HttpClient httpClient,
        Business.Identity.AdminApiAuthorizationService apiAuthorizationService,
        Business.Identity.CurrentSessionContext currentSessionContext)
    {
        _httpClient = httpClient;
        _apiAuthorizationService = apiAuthorizationService;
        _currentSessionContext = currentSessionContext;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<IReadOnlyList<CompanyUserListItem>>> GetCompanyUsersAsync(long companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure("Не удалось определить компанию текущего пользователя.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/admin/companies/{companyId}/users");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var users = await response.Content.ReadFromJsonAsync<List<CompanyUserListItem>>(_jsonOptions, cancellationToken);
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Success(users ?? []);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult<CreatedUserCredentials>> CreateUserAsync(AdminUserCreateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, "api/admin/users");
            httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(httpRequest, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<CreatedUserCredentials>.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<CreatedUserCredentials>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var createdUser = await response.Content.ReadFromJsonAsync<CreatedUserCredentials>(_jsonOptions, cancellationToken);
            return createdUser is null
                ? OperationResult<CreatedUserCredentials>.Failure("API вернул пустой ответ после создания пользователя.", "API_EMPTY_RESPONSE")
                : OperationResult<CreatedUserCredentials>.Success(createdUser);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<CreatedUserCredentials>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<CreatedUserCredentials>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult<CompanyUserListItem>> UpdateUserAsync(long userId, AdminUserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return OperationResult<CompanyUserListItem>.Failure("Не удалось определить пользователя для редактирования.");
        }

        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Put, $"api/admin/users/{userId}");
            httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(httpRequest, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<CompanyUserListItem>.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<CompanyUserListItem>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var updatedUser = await response.Content.ReadFromJsonAsync<CompanyUserListItem>(_jsonOptions, cancellationToken);
            return updatedUser is null
                ? OperationResult<CompanyUserListItem>.Failure("API вернул пустой ответ после обновления пользователя.", "API_EMPTY_RESPONSE")
                : OperationResult<CompanyUserListItem>.Success(updatedUser);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<CompanyUserListItem>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<CompanyUserListItem>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult> DeactivateUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return OperationResult.Failure("Не удалось определить пользователя для деактивации.");
        }

        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, $"api/admin/users/{userId}/deactivate");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(httpRequest, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            return OperationResult.Success("Пользователь деактивирован.");
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

    public async Task<OperationResult> ActivateUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return OperationResult.Failure("Не удалось определить пользователя для активации.");
        }

        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, $"api/admin/users/{userId}/activate");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(httpRequest, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            return OperationResult.Success("Пользователь активирован.");
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

    public async Task<OperationResult<ResetPasswordResult>> ResetPasswordAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return OperationResult<ResetPasswordResult>.Failure("Не удалось определить пользователя для сброса пароля.");
        }

        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, $"api/admin/users/{userId}/reset-password");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(httpRequest, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<ResetPasswordResult>.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<ResetPasswordResult>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var resetResult = await response.Content.ReadFromJsonAsync<ResetPasswordResult>(_jsonOptions, cancellationToken);
            return resetResult is null
                ? OperationResult<ResetPasswordResult>.Failure("API вернул пустой ответ после сброса пароля.", "API_EMPTY_RESPONSE")
                : OperationResult<ResetPasswordResult>.Success(resetResult);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<ResetPasswordResult>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<ResetPasswordResult>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
    {
        return new HttpRequestMessage(method, requestUri);
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

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}

using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Refline.Data.Infrastructure;

namespace Refline.Business.Identity;

public sealed class ApiAuthorizationService
{
    private static readonly TimeSpan RefreshLeadTime = TimeSpan.FromMinutes(1);

    private readonly HttpClient _httpClient;
    private readonly ICurrentUserSessionStore _sessionStore;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ApiAuthorizationService(HttpClient httpClient, ICurrentUserSessionStore sessionStore)
    {
        _httpClient = httpClient;
        _sessionStore = sessionStore;
    }

    public async Task<OperationResult> AuthorizeRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var tokenResult = await GetValidAccessTokenAsync(cancellationToken);
        if (!tokenResult.IsSuccess || string.IsNullOrWhiteSpace(tokenResult.Value))
        {
            return OperationResult.Failure(tokenResult.Message, tokenResult.ErrorCode);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);
        return OperationResult.Success();
    }

    private async Task<OperationResult<string>> GetValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        var session = _sessionStore.GetCurrentSession();
        if (session == null)
        {
            return OperationResult<string>.Failure("Пользовательская сессия не найдена.", "AUTH_SESSION_MISSING");
        }

        if (!string.IsNullOrWhiteSpace(session.AccessToken) &&
            session.AccessTokenExpiresAt > DateTimeOffset.UtcNow.Add(RefreshLeadTime))
        {
            return OperationResult<string>.Success(session.AccessToken);
        }

        if (string.IsNullOrWhiteSpace(session.RefreshToken) || session.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            await _sessionStore.ClearAsync();
            return OperationResult<string>.Failure("Сессия истекла. Выполните вход снова.", "AUTH_RELOGIN_REQUIRED");
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            session = _sessionStore.GetCurrentSession();
            if (session != null &&
                !string.IsNullOrWhiteSpace(session.AccessToken) &&
                session.AccessTokenExpiresAt > DateTimeOffset.UtcNow.Add(RefreshLeadTime))
            {
                return OperationResult<string>.Success(session.AccessToken);
            }

            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshTokenRequestDto { RefreshToken = session!.RefreshToken },
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    await _sessionStore.ClearAsync();
                    return OperationResult<string>.Failure("Сессия истекла. Выполните вход снова.", "AUTH_RELOGIN_REQUIRED");
                }

                return OperationResult<string>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var refreshResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>(_jsonOptions, cancellationToken);
            if (refreshResponse == null)
            {
                return OperationResult<string>.Failure("API вернул пустой ответ при обновлении сессии.", "API_EMPTY_RESPONSE");
            }

            var updateResult = await _sessionStore.UpdateTokensAsync(new ApiTokenSet
            {
                AccessToken = refreshResponse.AccessToken,
                AccessTokenExpiresAt = refreshResponse.AccessTokenExpiresAt,
                RefreshToken = refreshResponse.RefreshToken,
                RefreshTokenExpiresAt = refreshResponse.RefreshTokenExpiresAt
            });

            if (!updateResult.IsSuccess)
            {
                return OperationResult<string>.Failure(updateResult.Message, updateResult.ErrorCode);
            }

            return OperationResult<string>.Success(refreshResponse.AccessToken);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<string>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return OperationResult<string>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
        finally
        {
            _refreshLock.Release();
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

    private sealed class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}

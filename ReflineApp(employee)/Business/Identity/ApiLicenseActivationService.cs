using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public sealed class ApiLicenseActivationService : ILicenseActivationService
{
    private readonly HttpClient _httpClient;
    private readonly ApiAuthorizationService _apiAuthorizationService;
    private readonly ILocalActivationStateStore _activationStateStore;
    private readonly IDeviceIdentityProvider _deviceIdentityProvider;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ICurrentUserSessionStore _currentUserSessionStore;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ApiLicenseActivationService(
        HttpClient httpClient,
        ApiAuthorizationService apiAuthorizationService,
        ILocalActivationStateStore activationStateStore,
        IDeviceIdentityProvider deviceIdentityProvider,
        ICurrentUserContext currentUserContext,
        ICurrentUserSessionStore currentUserSessionStore)
    {
        _httpClient = httpClient;
        _apiAuthorizationService = apiAuthorizationService;
        _activationStateStore = activationStateStore;
        _deviceIdentityProvider = deviceIdentityProvider;
        _currentUserContext = currentUserContext;
        _currentUserSessionStore = currentUserSessionStore;
    }

    public Task<OperationResult<License?>> ValidateLicenseKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(
                OperationResult<License?>.Failure("Лицензионный ключ не указан.", "LICENSE_KEY_REQUIRED"));
        }

        return Task.FromResult(OperationResult<License?>.Failure(
            "Предварительная проверка лицензии через API отдельно не поддерживается. Используйте активацию.",
            "LICENSE_PRECHECK_NOT_SUPPORTED"));
    }

    public async Task<OperationResult<DeviceActivation>> ActivateAsync(Guid userId, string licenseKey)
    {
        var serverUserId = ApiIdentityIdMapper.ToServerId(userId);
        if (serverUserId <= 0)
        {
            return OperationResult<DeviceActivation>.Failure("Пользователь не найден или неактивен.", "USER_NOT_ACTIVE");
        }

        var deviceIdResult = await _deviceIdentityProvider.GetOrCreateDeviceIdAsync();
        if (!deviceIdResult.IsSuccess || string.IsNullOrWhiteSpace(deviceIdResult.Value))
        {
            return OperationResult<DeviceActivation>.Failure(deviceIdResult.Message, deviceIdResult.ErrorCode);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/licenses/activate")
            {
                Content = JsonContent.Create(
                    new ActivateLicenseRequestDto
                    {
                        UserId = serverUserId,
                        LicenseKey = (licenseKey ?? string.Empty).Trim(),
                        DeviceId = deviceIdResult.Value,
                        MachineName = Environment.MachineName
                    },
                    options: _jsonOptions)
            };

            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<DeviceActivation>.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response, CancellationToken.None);
                return OperationResult<DeviceActivation>.Failure(message, $"HTTP_{(int)response.StatusCode}");
            }

            var activationResponse = await response.Content.ReadFromJsonAsync<ActivateLicenseResponseDto>(_jsonOptions);
            if (activationResponse == null)
            {
                return OperationResult<DeviceActivation>.Failure(
                    "API вернул пустой ответ активации.",
                    "API_EMPTY_RESPONSE");
            }

            var activation = new DeviceActivation
            {
                Id = ApiIdentityIdMapper.ToLocalGuid(activationResponse.ActivationId),
                LicenseId = ApiIdentityIdMapper.ToLocalGuid(activationResponse.LicenseId),
                UserId = ApiIdentityIdMapper.ToLocalGuid(activationResponse.UserId),
                DeviceId = activationResponse.DeviceId ?? string.Empty,
                MachineName = activationResponse.MachineName ?? Environment.MachineName,
                ActivatedAt = activationResponse.ActivatedAt.UtcDateTime,
                LastSeenAt = activationResponse.LastSeenAt.UtcDateTime,
                IsRevoked = activationResponse.IsRevoked
            };

            var localState = new LocalActivationState
            {
                CurrentUserId = activation.UserId,
                CurrentLicenseKey = (licenseKey ?? string.Empty).Trim(),
                CurrentDeviceId = activation.DeviceId,
                IsActivated = !activation.IsRevoked,
                LastValidatedAt = activation.LastSeenAt
            };

            var saveStateResult = await _activationStateStore.SaveAsync(localState);
            if (!saveStateResult.IsSuccess)
            {
                return OperationResult<DeviceActivation>.Failure(
                    saveStateResult.Message,
                    saveStateResult.ErrorCode);
            }

            _currentUserContext.SetCurrentUser(activation.UserId);
            return OperationResult<DeviceActivation>.Success(activation, "Устройство активировано.");
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<DeviceActivation>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<DeviceActivation>.Failure(
                $"Превышено время ожидания API: {ex.Message}",
                "API_TIMEOUT");
        }
    }

    public Task<OperationResult<LocalActivationState>> GetLocalActivationStateAsync()
    {
        return _activationStateStore.LoadAsync();
    }

    public async Task<OperationResult<bool>> IsActivatedAsync()
    {
        var stateResult = await _activationStateStore.LoadAsync();
        if (!stateResult.IsSuccess)
        {
            return OperationResult<bool>.Failure(stateResult.Message, stateResult.ErrorCode);
        }

        var state = stateResult.Value ?? LocalActivationState.Empty();
        return OperationResult<bool>.Success(
            state.IsActivated &&
            state.CurrentUserId.HasValue &&
            !string.IsNullOrWhiteSpace(state.CurrentLicenseKey) &&
            !string.IsNullOrWhiteSpace(state.CurrentDeviceId));
    }

    public async Task<OperationResult<CurrentActivationValidationResult>> ValidateCurrentActivationAsync(CancellationToken cancellationToken = default)
    {
        var stateResult = await _activationStateStore.LoadAsync();
        if (!stateResult.IsSuccess)
        {
            return OperationResult<CurrentActivationValidationResult>.Failure(stateResult.Message, stateResult.ErrorCode);
        }

        var state = stateResult.Value ?? LocalActivationState.Empty();
        if (!state.IsActivated ||
            !state.CurrentUserId.HasValue ||
            string.IsNullOrWhiteSpace(state.CurrentLicenseKey) ||
            string.IsNullOrWhiteSpace(state.CurrentDeviceId))
        {
            return OperationResult<CurrentActivationValidationResult>.Success(new CurrentActivationValidationResult
            {
                Status = CurrentActivationValidationStatus.NotActivated,
                Message = "Локальная активация отсутствует."
            });
        }

        try
        {
            var requestUri =
                $"api/licenses/activations/current?licenseKey={Uri.EscapeDataString(state.CurrentLicenseKey)}&deviceId={Uri.EscapeDataString(state.CurrentDeviceId)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return await HandleValidationFailureAsync(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var activationResponse = await response.Content.ReadFromJsonAsync<ActivateLicenseResponseDto>(_jsonOptions, cancellationToken);
                if (activationResponse == null)
                {
                    return OperationResult<CurrentActivationValidationResult>.Failure(
                        "API вернул пустой ответ проверки активации.",
                        "API_EMPTY_RESPONSE");
                }

                if (activationResponse.IsRevoked)
                {
                    return await RevokeCurrentActivationAsync();
                }

                state.LastValidatedAt = activationResponse.LastSeenAt.UtcDateTime;
                var saveResult = await _activationStateStore.SaveAsync(state);
                if (!saveResult.IsSuccess)
                {
                    return OperationResult<CurrentActivationValidationResult>.Failure(saveResult.Message, saveResult.ErrorCode);
                }

                return OperationResult<CurrentActivationValidationResult>.Success(new CurrentActivationValidationResult
                {
                    Status = CurrentActivationValidationStatus.Valid,
                    Message = "Активация устройства подтверждена."
                });
            }

            var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return await RevokeCurrentActivationAsync(errorMessage);
            }

            return OperationResult<CurrentActivationValidationResult>.Success(new CurrentActivationValidationResult
            {
                Status = CurrentActivationValidationStatus.Unavailable,
                Message = string.IsNullOrWhiteSpace(errorMessage)
                    ? $"Проверка активации недоступна: HTTP {(int)response.StatusCode}."
                    : errorMessage
            });
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<CurrentActivationValidationResult>.Success(new CurrentActivationValidationResult
            {
                Status = CurrentActivationValidationStatus.Unavailable,
                Message = $"API недоступен: {ex.Message}"
            });
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<CurrentActivationValidationResult>.Success(new CurrentActivationValidationResult
            {
                Status = CurrentActivationValidationStatus.Unavailable,
                Message = $"Превышено время ожидания API: {ex.Message}"
            });
        }
    }

    private async Task<OperationResult<CurrentActivationValidationResult>> HandleValidationFailureAsync(string message, string? errorCode)
    {
        if (string.Equals(errorCode, "HTTP_401", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, "HTTP_403", StringComparison.OrdinalIgnoreCase))
        {
            return await RevokeCurrentActivationAsync(string.IsNullOrWhiteSpace(message)
                ? "Текущая активация более недействительна."
                : message);
        }

        return OperationResult<CurrentActivationValidationResult>.Success(new CurrentActivationValidationResult
        {
            Status = CurrentActivationValidationStatus.Unavailable,
            Message = message
        });
    }

    private async Task<OperationResult<CurrentActivationValidationResult>> RevokeCurrentActivationAsync(string? message = null)
    {
        _currentUserContext.Clear();
        await _currentUserSessionStore.ClearAsync();
        await _activationStateStore.ClearAsync();

        return OperationResult<CurrentActivationValidationResult>.Success(new CurrentActivationValidationResult
        {
            Status = CurrentActivationValidationStatus.Revoked,
            Message = string.IsNullOrWhiteSpace(message)
                ? "Это устройство было отвязано от лицензии. Выполните активацию заново."
                : message
        });
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

    private sealed class ActivateLicenseRequestDto
    {
        public long UserId { get; set; }

        public string LicenseKey { get; set; } = string.Empty;

        public string DeviceId { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;
    }

    private sealed class ActivateLicenseResponseDto
    {
        public long ActivationId { get; set; }

        public long LicenseId { get; set; }

        public long UserId { get; set; }

        public string DeviceId { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public DateTimeOffset ActivatedAt { get; set; }

        public DateTimeOffset LastSeenAt { get; set; }

        public bool IsRevoked { get; set; }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}

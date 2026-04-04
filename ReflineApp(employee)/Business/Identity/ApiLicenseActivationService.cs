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
    private readonly ILocalActivationStateStore _activationStateStore;
    private readonly IDeviceIdentityProvider _deviceIdentityProvider;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ApiLicenseActivationService(
        HttpClient httpClient,
        ILocalActivationStateStore activationStateStore,
        IDeviceIdentityProvider deviceIdentityProvider,
        ICurrentUserContext currentUserContext)
    {
        _httpClient = httpClient;
        _activationStateStore = activationStateStore;
        _deviceIdentityProvider = deviceIdentityProvider;
        _currentUserContext = currentUserContext;
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
            using var response = await _httpClient.PostAsJsonAsync(
                "api/licenses/activate",
                new ActivateLicenseRequestDto
                {
                    UserId = serverUserId,
                    LicenseKey = (licenseKey ?? string.Empty).Trim(),
                    DeviceId = deviceIdResult.Value,
                    MachineName = Environment.MachineName
                },
                _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response);
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

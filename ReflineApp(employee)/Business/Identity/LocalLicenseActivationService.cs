using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public class LocalLicenseActivationService : ILicenseActivationService
{
    private readonly IUserStore _userStore;
    private readonly ILicenseStore _licenseStore;
    private readonly IDeviceActivationStore _deviceActivationStore;
    private readonly ILocalActivationStateStore _activationStateStore;
    private readonly IDeviceIdentityProvider _deviceIdentityProvider;
    private readonly ICurrentUserContext _currentUserContext;

    public LocalLicenseActivationService(
        IUserStore userStore,
        ILicenseStore licenseStore,
        IDeviceActivationStore deviceActivationStore,
        ILocalActivationStateStore activationStateStore,
        IDeviceIdentityProvider deviceIdentityProvider,
        ICurrentUserContext currentUserContext)
    {
        _userStore = userStore;
        _licenseStore = licenseStore;
        _deviceActivationStore = deviceActivationStore;
        _activationStateStore = activationStateStore;
        _deviceIdentityProvider = deviceIdentityProvider;
        _currentUserContext = currentUserContext;
    }

    public async Task<OperationResult<License?>> ValidateLicenseKeyAsync(string key)
    {
        var licenseResult = await _licenseStore.GetByKeyAsync(key);
        if (!licenseResult.IsSuccess)
        {
            return OperationResult<License?>.Failure(licenseResult.Message, licenseResult.ErrorCode);
        }

        var license = licenseResult.Value;
        if (license == null)
        {
            return OperationResult<License?>.Failure("Лицензия не найдена.", "LICENSE_NOT_FOUND");
        }

        if (!license.IsActive || license.ExpiresAt < DateTime.UtcNow)
        {
            return OperationResult<License?>.Failure("Лицензия неактивна или истекла.", "LICENSE_INACTIVE");
        }

        return OperationResult<License?>.Success(license);
    }

    public async Task<OperationResult<DeviceActivation>> ActivateAsync(Guid userId, string licenseKey)
    {
        var userResult = await _userStore.GetByIdAsync(userId);
        if (!userResult.IsSuccess)
        {
            return OperationResult<DeviceActivation>.Failure(userResult.Message, userResult.ErrorCode);
        }

        var user = userResult.Value;
        if (user == null || !user.IsActive)
        {
            return OperationResult<DeviceActivation>.Failure("Пользователь не найден или неактивен.", "USER_NOT_ACTIVE");
        }

        var licenseValidation = await ValidateLicenseKeyAsync(licenseKey);
        if (!licenseValidation.IsSuccess || licenseValidation.Value == null)
        {
            return OperationResult<DeviceActivation>.Failure(licenseValidation.Message, licenseValidation.ErrorCode);
        }

        var license = licenseValidation.Value;
        if (license.CompanyId != user.CompanyId)
        {
            return OperationResult<DeviceActivation>.Failure("Лицензия не принадлежит компании пользователя.", "LICENSE_COMPANY_MISMATCH");
        }

        var deviceIdResult = await _deviceIdentityProvider.GetOrCreateDeviceIdAsync();
        if (!deviceIdResult.IsSuccess || string.IsNullOrWhiteSpace(deviceIdResult.Value))
        {
            return OperationResult<DeviceActivation>.Failure(deviceIdResult.Message, deviceIdResult.ErrorCode);
        }

        var deviceId = deviceIdResult.Value;
        var activationResult = await _deviceActivationStore.GetByLicenseAndDeviceAsync(license.Id, deviceId);
        if (!activationResult.IsSuccess)
        {
            return OperationResult<DeviceActivation>.Failure(activationResult.Message, activationResult.ErrorCode);
        }

        var activation = activationResult.Value;
        if (activation != null && activation.IsRevoked)
        {
            return OperationResult<DeviceActivation>.Failure("Активация этого устройства отозвана.", "DEVICE_ACTIVATION_REVOKED");
        }

        if (activation == null)
        {
            var activeCountResult = await _deviceActivationStore.CountActiveByLicenseAsync(license.Id);
            if (!activeCountResult.IsSuccess)
            {
                return OperationResult<DeviceActivation>.Failure(activeCountResult.Message, activeCountResult.ErrorCode);
            }

            if (activeCountResult.Value >= license.MaxDevices)
            {
                return OperationResult<DeviceActivation>.Failure(
                    "Достигнут лимит активированных устройств по лицензии.",
                    "LICENSE_DEVICE_LIMIT_REACHED");
            }

            activation = new DeviceActivation
            {
                Id = Guid.NewGuid(),
                LicenseId = license.Id,
                UserId = user.Id,
                DeviceId = deviceId,
                MachineName = Environment.MachineName,
                ActivatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IsRevoked = false
            };
        }
        else
        {
            activation.UserId = user.Id;
            activation.MachineName = Environment.MachineName;
            activation.LastSeenAt = DateTime.UtcNow;
        }

        var saveActivationResult = await _deviceActivationStore.SaveAsync(activation);
        if (!saveActivationResult.IsSuccess)
        {
            return OperationResult<DeviceActivation>.Failure(saveActivationResult.Message, saveActivationResult.ErrorCode);
        }

        var localState = new LocalActivationState
        {
            CurrentUserId = user.Id,
            CurrentLicenseKey = license.LicenseKey,
            CurrentDeviceId = deviceId,
            IsActivated = true,
            LastValidatedAt = DateTime.UtcNow
        };

        var saveStateResult = await _activationStateStore.SaveAsync(localState);
        if (!saveStateResult.IsSuccess)
        {
            return OperationResult<DeviceActivation>.Failure(saveStateResult.Message, saveStateResult.ErrorCode);
        }

        _currentUserContext.SetCurrentUser(user.Id);
        return OperationResult<DeviceActivation>.Success(activation, "Устройство активировано.");
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

    public Task<OperationResult<CurrentActivationValidationResult>> ValidateCurrentActivationAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(OperationResult<CurrentActivationValidationResult>.Success(
            new CurrentActivationValidationResult
            {
                Status = CurrentActivationValidationStatus.Valid,
                Message = "Локальная активация считается действительной."
            }));
    }
}

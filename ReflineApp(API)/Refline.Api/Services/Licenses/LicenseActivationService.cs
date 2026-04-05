using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Licenses;
using Refline.Api.Data;
using Refline.Api.Entities;

namespace Refline.Api.Services.Licenses;

public sealed class LicenseActivationService(ReflineDbContext dbContext)
{
    public async Task<LicenseActivationResult> ActivateAsync(
        ActivateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(currentUser => currentUser.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return LicenseActivationResult.Failure(
                LicenseActivationResultStatus.UserNotFound,
                "User not found.");
        }

        if (!user.IsActive)
        {
            return LicenseActivationResult.Failure(
                LicenseActivationResultStatus.UserInactive,
                "User account is inactive.");
        }

        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(
                currentLicense => currentLicense.LicenseKey == request.LicenseKey,
                cancellationToken);

        if (license is null)
        {
            return LicenseActivationResult.Failure(
                LicenseActivationResultStatus.LicenseNotFound,
                "License not found.");
        }

        if (!license.IsActive)
        {
            return LicenseActivationResult.Failure(
                LicenseActivationResultStatus.LicenseInactive,
                "License is inactive.");
        }

        if (license.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return LicenseActivationResult.Failure(
                LicenseActivationResultStatus.LicenseExpired,
                "License has expired.");
        }

        if (license.CompanyId != user.CompanyId)
        {
            return LicenseActivationResult.Failure(
                LicenseActivationResultStatus.CompanyMismatch,
                "License does not belong to the user's company.");
        }

        var existingActivation = await dbContext.DeviceActivations
            .FirstOrDefaultAsync(
                activation => activation.LicenseId == license.Id && activation.DeviceId == request.DeviceId,
                cancellationToken);

        if (existingActivation is not null)
        {
            if (existingActivation.IsRevoked)
            {
                return LicenseActivationResult.Failure(
                    LicenseActivationResultStatus.ActivationRevoked,
                    "This device activation has been revoked.");
            }

            if (existingActivation.UserId != user.Id)
            {
                return LicenseActivationResult.Failure(
                    LicenseActivationResultStatus.DeviceAssignedToAnotherUser,
                    "This device is already activated for another user.");
            }

            existingActivation.MachineName = request.MachineName;
            existingActivation.LastSeenAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return LicenseActivationResult.Success(MapToResponse(existingActivation));
        }

        var activeDeviceCount = await dbContext.DeviceActivations
            .CountAsync(
                activation => activation.LicenseId == license.Id && !activation.IsRevoked,
                cancellationToken);

        if (activeDeviceCount >= license.MaxDevices)
        {
            return LicenseActivationResult.Failure(
                LicenseActivationResultStatus.DeviceLimitReached,
                "License device limit has been reached.");
        }

        var newActivation = new DeviceActivation
        {
            LicenseId = license.Id,
            UserId = user.Id,
            DeviceId = request.DeviceId,
            MachineName = request.MachineName,
            ActivatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };

        dbContext.DeviceActivations.Add(newActivation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return LicenseActivationResult.Success(MapToResponse(newActivation));
    }

    private static ActivateLicenseResponse MapToResponse(DeviceActivation activation)
    {
        return new ActivateLicenseResponse
        {
            ActivationId = activation.Id,
            LicenseId = activation.LicenseId,
            UserId = activation.UserId,
            DeviceId = activation.DeviceId,
            MachineName = activation.MachineName,
            ActivatedAt = activation.ActivatedAt,
            LastSeenAt = activation.LastSeenAt,
            IsRevoked = activation.IsRevoked
        };
    }
}

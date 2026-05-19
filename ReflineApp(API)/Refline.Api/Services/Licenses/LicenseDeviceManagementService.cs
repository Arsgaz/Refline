using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Admin;
using Refline.Api.Contracts.Licenses;
using Refline.Api.Data;
using Refline.Api.Entities;

namespace Refline.Api.Services.Licenses;

public sealed class LicenseDeviceManagementService(ReflineDbContext dbContext)
{
    public Task<List<LicenseDeviceActivationDto>> GetCompanyLicenseDevicesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        return dbContext.DeviceActivations
            .AsNoTracking()
            .Where(activation => activation.License.CompanyId == companyId && activation.License.IsActive)
            .OrderBy(activation => activation.IsRevoked)
            .ThenByDescending(activation => activation.LastSeenAt)
            .ThenByDescending(activation => activation.Id)
            .Select(activation => new LicenseDeviceActivationDto
            {
                ActivationId = activation.Id,
                LicenseId = activation.LicenseId,
                UserId = activation.UserId,
                UserFullName = activation.User.FullName,
                UserLogin = activation.User.Login,
                DeviceId = activation.DeviceId,
                MachineName = activation.MachineName,
                ActivatedAt = activation.ActivatedAt,
                LastSeenAt = activation.LastSeenAt,
                IsRevoked = activation.IsRevoked
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeDeviceActivationAsync(long companyId, long activationId, CancellationToken cancellationToken)
    {
        var activation = await dbContext.DeviceActivations
            .Include(item => item.License)
            .SingleOrDefaultAsync(item => item.Id == activationId, cancellationToken);

        if (activation is null || activation.License.CompanyId != companyId)
        {
            return false;
        }

        if (activation.IsRevoked)
        {
            return true;
        }

        activation.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeActiveUserDevicesAsync(long userId, CancellationToken cancellationToken)
    {
        var activeDevices = await dbContext.DeviceActivations
            .Where(activation => activation.UserId == userId && !activation.IsRevoked)
            .ToListAsync(cancellationToken);

        if (activeDevices.Count == 0)
        {
            return 0;
        }

        foreach (var activation in activeDevices)
        {
            activation.IsRevoked = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return activeDevices.Count;
    }

    public async Task<CurrentDeviceActivationStatusResult> GetCurrentActivationStatusAsync(
        CurrentDeviceActivationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return CurrentDeviceActivationStatusResult.Failure(
                CurrentDeviceActivationStatusResultStatus.UserNotFound,
                "User not found.");
        }

        if (!user.IsActive)
        {
            return CurrentDeviceActivationStatusResult.Failure(
                CurrentDeviceActivationStatusResultStatus.UserInactive,
                "User account is inactive.");
        }

        var normalizedLicenseKey = (request.LicenseKey ?? string.Empty).Trim();
        var normalizedDeviceId = (request.DeviceId ?? string.Empty).Trim();

        var license = await dbContext.Licenses
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.LicenseKey == normalizedLicenseKey, cancellationToken);

        if (license is null)
        {
            return CurrentDeviceActivationStatusResult.Failure(
                CurrentDeviceActivationStatusResultStatus.LicenseNotFound,
                "License not found.");
        }

        if (license.CompanyId != user.CompanyId)
        {
            return CurrentDeviceActivationStatusResult.Failure(
                CurrentDeviceActivationStatusResultStatus.CompanyMismatch,
                "License does not belong to the user's company.");
        }

        var activation = await dbContext.DeviceActivations
            .SingleOrDefaultAsync(
                item => item.LicenseId == license.Id && item.UserId == user.Id && item.DeviceId == normalizedDeviceId,
                cancellationToken);

        if (activation is null)
        {
            return CurrentDeviceActivationStatusResult.Failure(
                CurrentDeviceActivationStatusResultStatus.ActivationNotFound,
                "Device activation was not found.");
        }

        if (!activation.IsRevoked)
        {
            activation.LastSeenAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return CurrentDeviceActivationStatusResult.Success(MapToCurrentStatusResponse(activation));
    }

    public static CurrentDeviceActivationStatusResponse MapToCurrentStatusResponse(DeviceActivation activation)
    {
        return new CurrentDeviceActivationStatusResponse
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

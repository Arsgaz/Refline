using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Admin;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Enums;
using Refline.Api.Security;

namespace Refline.Api.Services.Admin;

public sealed class AdminUserManagementService(ReflineDbContext dbContext)
{
    public async Task<AdminUserManagementResult<AdminManagedUserDto>> CreateUserAsync(
        AdminAccessContext accessContext,
        CreateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Forbidden,
                "Only Admin can manage company users.");
        }

        var fullName = NormalizeRequired(request.FullName);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "FullName is required.");
        }

        var login = NormalizeRequired(request.Login);
        if (string.IsNullOrWhiteSpace(login))
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "Login is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "Password is required.");
        }

        if (!Enum.IsDefined(request.Role))
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "Role value is invalid.");
        }

        var loginInUse = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.CompanyId == accessContext.CompanyId && user.Login == login,
                cancellationToken);

        if (loginInUse)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Conflict,
                "Login must be unique within the company.");
        }

        var managerValidation = await ValidateManagerAsync(
            accessContext.CompanyId,
            request.ManagerId,
            null,
            cancellationToken);

        if (!managerValidation.IsSuccess)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                managerValidation.ErrorType!.Value,
                managerValidation.ErrorMessage!);
        }

        var user = new User
        {
            CompanyId = accessContext.CompanyId,
            FullName = fullName,
            Login = login,
            PasswordHash = PasswordHashHelper.ComputeHash(request.Password),
            Role = request.Role,
            ManagerId = managerValidation.ManagerId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Conflict,
                "Login must be unique within the company.");
        }

        return AdminUserManagementResult<AdminManagedUserDto>.Success(MapUser(user));
    }

    public async Task<AdminUserManagementResult<AdminManagedUserDto>> UpdateUserAsync(
        AdminAccessContext accessContext,
        long userId,
        UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Forbidden,
                "Only Admin can manage company users.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                existingUser => existingUser.Id == userId && existingUser.CompanyId == accessContext.CompanyId,
                cancellationToken);

        if (user is null)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.NotFound,
                "User was not found in the current company.");
        }

        var fullName = NormalizeRequired(request.FullName);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "FullName is required.");
        }

        var login = NormalizeRequired(request.Login);
        if (string.IsNullOrWhiteSpace(login))
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "Login is required.");
        }

        if (!Enum.IsDefined(request.Role))
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "Role value is invalid.");
        }

        var loginInUse = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                existingUser =>
                    existingUser.CompanyId == accessContext.CompanyId &&
                    existingUser.Login == login &&
                    existingUser.Id != userId,
                cancellationToken);

        if (loginInUse)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Conflict,
                "Login must be unique within the company.");
        }

        var managerValidation = await ValidateManagerAsync(
            accessContext.CompanyId,
            request.ManagerId,
            userId,
            cancellationToken);

        if (!managerValidation.IsSuccess)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                managerValidation.ErrorType!.Value,
                managerValidation.ErrorMessage!);
        }

        var removesAdminPrivileges = user.Role == UserRole.Admin && user.IsActive && request.Role != UserRole.Admin;
        if (removesAdminPrivileges)
        {
            var canRemoveAdmin = await HasAnotherActiveAdminAsync(accessContext.CompanyId, user.Id, cancellationToken);
            if (!canRemoveAdmin)
            {
                return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                    AdminUserManagementErrorType.Validation,
                    "You cannot remove the last active Admin from the company.");
            }
        }

        user.FullName = fullName;
        user.Login = login;
        user.Role = request.Role;
        user.ManagerId = managerValidation.ManagerId;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Conflict,
                "Login must be unique within the company.");
        }

        return AdminUserManagementResult<AdminManagedUserDto>.Success(MapUser(user));
    }

    public async Task<AdminUserManagementResult<AdminManagedUserDto>> DeactivateUserAsync(
        AdminAccessContext accessContext,
        long userId,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Forbidden,
                "Only Admin can manage company users.");
        }

        if (accessContext.UserId == userId)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Validation,
                "You cannot deactivate your own account.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                existingUser => existingUser.Id == userId && existingUser.CompanyId == accessContext.CompanyId,
                cancellationToken);

        if (user is null)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.NotFound,
                "User was not found in the current company.");
        }

        if (!user.IsActive)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Success(MapUser(user));
        }

        if (user.Role == UserRole.Admin)
        {
            var canDeactivate = await HasAnotherActiveAdminAsync(accessContext.CompanyId, user.Id, cancellationToken);
            if (!canDeactivate)
            {
                return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                    AdminUserManagementErrorType.Validation,
                    "You cannot deactivate the last active Admin in the company.");
            }
        }

        user.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminUserManagementResult<AdminManagedUserDto>.Success(MapUser(user));
    }

    public async Task<AdminUserManagementResult<AdminManagedUserDto>> ActivateUserAsync(
        AdminAccessContext accessContext,
        long userId,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.Forbidden,
                "Only Admin can manage company users.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                existingUser => existingUser.Id == userId && existingUser.CompanyId == accessContext.CompanyId,
                cancellationToken);

        if (user is null)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                AdminUserManagementErrorType.NotFound,
                "User was not found in the current company.");
        }

        if (user.IsActive)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Success(MapUser(user));
        }

        var managerValidation = await ValidateManagerAsync(
            accessContext.CompanyId,
            user.ManagerId,
            user.Id,
            cancellationToken);

        if (!managerValidation.IsSuccess)
        {
            return AdminUserManagementResult<AdminManagedUserDto>.Failure(
                managerValidation.ErrorType!.Value,
                managerValidation.ErrorMessage!);
        }

        user.IsActive = true;
        user.ManagerId = managerValidation.ManagerId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminUserManagementResult<AdminManagedUserDto>.Success(MapUser(user));
    }

    private async Task<ManagerValidationResult> ValidateManagerAsync(
        long companyId,
        long? managerId,
        long? userId,
        CancellationToken cancellationToken)
    {
        if (!managerId.HasValue)
        {
            return ManagerValidationResult.Success(null);
        }

        if (userId.HasValue && managerId.Value == userId.Value)
        {
            return ManagerValidationResult.Failure(
                AdminUserManagementErrorType.Validation,
                "User cannot be assigned as their own manager.");
        }

        var manager = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == managerId.Value)
            .Select(user => new
            {
                user.Id,
                user.CompanyId,
                user.Role,
                user.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (manager is null)
        {
            return ManagerValidationResult.Failure(
                AdminUserManagementErrorType.Validation,
                "Selected manager was not found.");
        }

        if (manager.CompanyId != companyId)
        {
            return ManagerValidationResult.Failure(
                AdminUserManagementErrorType.Forbidden,
                "Selected manager belongs to another company.");
        }

        if (!manager.IsActive)
        {
            return ManagerValidationResult.Failure(
                AdminUserManagementErrorType.Validation,
                "Selected manager must be active.");
        }

        if (manager.Role is not UserRole.Admin and not UserRole.Manager)
        {
            return ManagerValidationResult.Failure(
                AdminUserManagementErrorType.Validation,
                "Selected manager must have Admin or Manager role.");
        }

        return ManagerValidationResult.Success(manager.Id);
    }

    private async Task<bool> HasAnotherActiveAdminAsync(long companyId, long excludedUserId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.CompanyId == companyId &&
                    user.Id != excludedUserId &&
                    user.IsActive &&
                    user.Role == UserRole.Admin,
                cancellationToken);
    }

    private static string NormalizeRequired(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static AdminManagedUserDto MapUser(User user)
    {
        return new AdminManagedUserDto(
            user.Id,
            user.CompanyId,
            user.FullName,
            user.Login,
            user.Role,
            user.ManagerId,
            user.IsActive,
            user.CreatedAt);
    }

    private sealed record ManagerValidationResult(
        bool IsSuccess,
        long? ManagerId,
        AdminUserManagementErrorType? ErrorType,
        string? ErrorMessage)
    {
        public static ManagerValidationResult Success(long? managerId)
        {
            return new ManagerValidationResult(true, managerId, null, null);
        }

        public static ManagerValidationResult Failure(AdminUserManagementErrorType errorType, string errorMessage)
        {
            return new ManagerValidationResult(false, null, errorType, errorMessage);
        }
    }
}

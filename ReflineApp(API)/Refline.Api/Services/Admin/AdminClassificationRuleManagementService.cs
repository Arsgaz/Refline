using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Admin;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Enums;

namespace Refline.Api.Services.Admin;

public sealed class AdminClassificationRuleManagementService(ReflineDbContext dbContext)
{
    private const int MinPriority = 0;
    private const int MaxPriority = 1000;

    public async Task<IReadOnlyList<ActivityClassificationRuleDto>> GetCompanyRulesAsync(
        AdminAccessContext accessContext,
        long companyId,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin || accessContext.CompanyId != companyId)
        {
            return [];
        }

        return await dbContext.ActivityClassificationRules
            .AsNoTracking()
            .Where(rule => rule.CompanyId == companyId)
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .Select(rule => new ActivityClassificationRuleDto(
                rule.Id,
                rule.CompanyId,
                rule.AppNamePattern,
                rule.WindowTitlePattern,
                rule.Category,
                rule.Priority,
                rule.IsEnabled,
                rule.CreatedAt,
                rule.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>> CreateRuleAsync(
        AdminAccessContext accessContext,
        CreateActivityClassificationRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return Forbidden();
        }

        var validationError = ValidateCompanyAccess(accessContext, request.CompanyId);
        if (validationError is not null)
        {
            return validationError;
        }

        var appNamePattern = NormalizeRequired(request.AppNamePattern);
        if (string.IsNullOrWhiteSpace(appNamePattern))
        {
            return ValidationFailure("AppNamePattern is required.");
        }

        if (!Enum.IsDefined(request.Category))
        {
            return ValidationFailure("Category value is invalid.");
        }

        if (!IsPriorityValid(request.Priority))
        {
            return ValidationFailure($"Priority must be between {MinPriority} and {MaxPriority}.");
        }

        var rule = new ActivityClassificationRule
        {
            CompanyId = request.CompanyId,
            AppNamePattern = appNamePattern,
            WindowTitlePattern = NormalizeOptional(request.WindowTitlePattern),
            Category = request.Category,
            Priority = request.Priority,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.ActivityClassificationRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>.Success(MapRule(rule));
    }

    public async Task<ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>> UpdateRuleAsync(
        AdminAccessContext accessContext,
        long ruleId,
        UpdateActivityClassificationRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return Forbidden();
        }

        var validationError = ValidateCompanyAccess(accessContext, request.CompanyId);
        if (validationError is not null)
        {
            return validationError;
        }

        var rule = await dbContext.ActivityClassificationRules
            .SingleOrDefaultAsync(
                existingRule => existingRule.Id == ruleId && existingRule.CompanyId == accessContext.CompanyId,
                cancellationToken);

        if (rule is null)
        {
            return NotFoundFailure("Classification rule was not found in the current company.");
        }

        var appNamePattern = NormalizeRequired(request.AppNamePattern);
        if (string.IsNullOrWhiteSpace(appNamePattern))
        {
            return ValidationFailure("AppNamePattern is required.");
        }

        if (!Enum.IsDefined(request.Category))
        {
            return ValidationFailure("Category value is invalid.");
        }

        if (!IsPriorityValid(request.Priority))
        {
            return ValidationFailure($"Priority must be between {MinPriority} and {MaxPriority}.");
        }

        rule.AppNamePattern = appNamePattern;
        rule.WindowTitlePattern = NormalizeOptional(request.WindowTitlePattern);
        rule.Category = request.Category;
        rule.Priority = request.Priority;
        rule.IsEnabled = request.IsEnabled;
        rule.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>.Success(MapRule(rule));
    }

    public async Task<ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>> ToggleRuleAsync(
        AdminAccessContext accessContext,
        long ruleId,
        ToggleActivityClassificationRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return Forbidden();
        }

        var rule = await dbContext.ActivityClassificationRules
            .SingleOrDefaultAsync(
                existingRule => existingRule.Id == ruleId && existingRule.CompanyId == accessContext.CompanyId,
                cancellationToken);

        if (rule is null)
        {
            return NotFoundFailure("Classification rule was not found in the current company.");
        }

        rule.IsEnabled = request.IsEnabled;
        rule.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>.Success(MapRule(rule));
    }

    public async Task<ActivityClassificationRuleManagementResult<bool>> DeleteRuleAsync(
        AdminAccessContext accessContext,
        long ruleId,
        CancellationToken cancellationToken)
    {
        if (accessContext.Role != UserRole.Admin)
        {
            return ActivityClassificationRuleManagementResult<bool>.Failure(
                ActivityClassificationRuleManagementErrorType.Forbidden,
                "Only Admin can manage company classification rules.");
        }

        var rule = await dbContext.ActivityClassificationRules
            .SingleOrDefaultAsync(
                existingRule => existingRule.Id == ruleId && existingRule.CompanyId == accessContext.CompanyId,
                cancellationToken);

        if (rule is null)
        {
            return ActivityClassificationRuleManagementResult<bool>.Failure(
                ActivityClassificationRuleManagementErrorType.NotFound,
                "Classification rule was not found in the current company.");
        }

        dbContext.ActivityClassificationRules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ActivityClassificationRuleManagementResult<bool>.Success(true);
    }

    private static string? NormalizeRequired(string? value)
    {
        return value?.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsPriorityValid(int priority)
    {
        return priority is >= MinPriority and <= MaxPriority;
    }

    private static ActivityClassificationRuleDto MapRule(ActivityClassificationRule rule)
    {
        return new ActivityClassificationRuleDto(
            rule.Id,
            rule.CompanyId,
            rule.AppNamePattern,
            rule.WindowTitlePattern,
            rule.Category,
            rule.Priority,
            rule.IsEnabled,
            rule.CreatedAt,
            rule.UpdatedAt);
    }

    private static ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>? ValidateCompanyAccess(
        AdminAccessContext accessContext,
        long companyId)
    {
        if (companyId <= 0)
        {
            return ValidationFailure("CompanyId must be a valid positive value.");
        }

        if (accessContext.CompanyId != companyId)
        {
            return ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>.Failure(
                ActivityClassificationRuleManagementErrorType.Forbidden,
                "Admin can manage classification rules only for the current company.");
        }

        return null;
    }

    private static ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto> Forbidden()
    {
        return ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>.Failure(
            ActivityClassificationRuleManagementErrorType.Forbidden,
            "Only Admin can manage company classification rules.");
    }

    private static ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto> ValidationFailure(string message)
    {
        return ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>.Failure(
            ActivityClassificationRuleManagementErrorType.Validation,
            message);
    }

    private static ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto> NotFoundFailure(string message)
    {
        return ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto>.Failure(
            ActivityClassificationRuleManagementErrorType.NotFound,
            message);
    }
}

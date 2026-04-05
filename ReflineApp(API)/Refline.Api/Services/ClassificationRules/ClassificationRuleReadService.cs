using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.ClassificationRules;
using Refline.Api.Data;

namespace Refline.Api.Services.ClassificationRules;

public sealed class ClassificationRuleReadService(ReflineDbContext dbContext)
{
    public async Task<IReadOnlyList<EmployeeClassificationRuleDto>> GetActiveRulesForCompanyAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ActivityClassificationRules
            .AsNoTracking()
            .Where(rule =>
                rule.CompanyId == companyId &&
                rule.IsEnabled &&
                !string.IsNullOrWhiteSpace(rule.AppNamePattern))
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .Select(rule => new EmployeeClassificationRuleDto(
                rule.Id,
                rule.AppNamePattern,
                rule.WindowTitlePattern,
                rule.Category,
                rule.Priority))
            .ToListAsync(cancellationToken);
    }
}

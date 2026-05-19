using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public interface IActivityClassificationRulesService
{
    Task<OperationResult<IReadOnlyList<ActivityClassificationRule>>> GetCompanyRulesAsync(long companyId, CancellationToken cancellationToken = default);

    Task<OperationResult<ActivityClassificationRule>> CreateRuleAsync(ActivityClassificationRuleCreateRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<ActivityClassificationRule>> UpdateRuleAsync(long ruleId, ActivityClassificationRuleUpdateRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<ActivityClassificationRule>> ToggleRuleAsync(long ruleId, bool isEnabled, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteRuleAsync(long ruleId, CancellationToken cancellationToken = default);
}

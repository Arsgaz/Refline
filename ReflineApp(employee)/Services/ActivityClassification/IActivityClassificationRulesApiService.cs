using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Services.ActivityClassification;

public interface IActivityClassificationRulesApiService
{
    Task<OperationResult<IReadOnlyList<ActivityClassificationRule>>> GetCompanyRulesAsync(Guid companyId, CancellationToken cancellationToken = default);
}

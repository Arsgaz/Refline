using Refline.Models;

namespace Refline.Business.Activity;

public interface IActivityMetricsService
{
    ActivityMetricsSummary Calculate(IReadOnlyList<AppActivity> activities);
}

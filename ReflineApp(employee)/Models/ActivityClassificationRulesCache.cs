namespace Refline.Models;

public sealed class ActivityClassificationRulesCache
{
    public Guid CompanyId { get; set; }

    public DateTimeOffset RefreshedAt { get; set; }

    public List<ActivityClassificationRule> Rules { get; set; } = [];
}

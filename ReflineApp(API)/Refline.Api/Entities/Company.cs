namespace Refline.Api.Entities;

public sealed class Company
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<License> Licenses { get; set; } = new List<License>();

    public ICollection<ActivityClassificationRule> ActivityClassificationRules { get; set; } = new List<ActivityClassificationRule>();
}

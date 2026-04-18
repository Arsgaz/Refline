using Refline.Api.Enums;

namespace Refline.Api.Entities;

public sealed class User
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public long? ManagerId { get; set; }

    public User? Manager { get; set; }

    public ICollection<User> Subordinates { get; set; } = new List<User>();

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<DeviceActivation> DeviceActivations { get; set; } = new List<DeviceActivation>();

    public ICollection<ActivityRecord> ActivityRecords { get; set; } = new List<ActivityRecord>();
}

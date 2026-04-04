using Refline.Models;
using Refline.Utils;

namespace Refline.Data.Identity;

public static class IdentitySeedData
{
    public static Guid CompanyId { get; } = Guid.Parse("1f0a60d4-4744-4b77-a4a7-19b656fbfe0f");
    public static Guid AdminUserId { get; } = Guid.Parse("0715b070-2ad6-4f6f-bf22-f2ae1dfd08c4");
    public static Guid ManagerUserId { get; } = Guid.Parse("f062f034-8937-4f62-a3f0-d136e134b73a");
    public static Guid EmployeeUserId { get; } = Guid.Parse("83d71d52-e7a1-49e9-9225-51658955b99f");
    public static Guid LicenseId { get; } = Guid.Parse("5f716452-bcd3-4d4c-88ef-2ea27e9f6944");
    public const string DemoLicenseKey = "RFLA-DEMO-2026-MVP1";

    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static List<User> CreateUsers()
    {
        return new List<User>
        {
            new()
            {
                Id = AdminUserId,
                CompanyId = CompanyId,
                FullName = "Refline Admin",
                Login = "admin",
                PasswordHash = PasswordHashHelper.ComputeHash("admin123"),
                Role = UserRole.Admin,
                ManagerId = null,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new()
            {
                Id = ManagerUserId,
                CompanyId = CompanyId,
                FullName = "Refline Manager",
                Login = "manager",
                PasswordHash = PasswordHashHelper.ComputeHash("manager123"),
                Role = UserRole.Manager,
                ManagerId = AdminUserId,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new()
            {
                Id = EmployeeUserId,
                CompanyId = CompanyId,
                FullName = "Refline Employee",
                Login = "employee",
                PasswordHash = PasswordHashHelper.ComputeHash("employee123"),
                Role = UserRole.Employee,
                ManagerId = ManagerUserId,
                IsActive = true,
                CreatedAt = SeedDate
            }
        };
    }

    public static List<License> CreateLicenses()
    {
        return new List<License>
        {
            new()
            {
                Id = LicenseId,
                CompanyId = CompanyId,
                LicenseKey = DemoLicenseKey,
                MaxDevices = 25,
                IssuedAt = SeedDate,
                ExpiresAt = SeedDate.AddYears(5),
                IsActive = true
            }
        };
    }
}

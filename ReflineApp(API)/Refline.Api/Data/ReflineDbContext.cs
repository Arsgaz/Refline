using Microsoft.EntityFrameworkCore;
using Refline.Api.Entities;
using Refline.Api.Security;

namespace Refline.Api.Data;

public sealed class ReflineDbContext(DbContextOptions<ReflineDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    public DbSet<License> Licenses => Set<License>();

    public DbSet<DeviceActivation> DeviceActivations => Set<DeviceActivation>();

    public DbSet<ActivityRecord> ActivityRecords => Set<ActivityRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReflineDbContext).Assembly);
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var companyCreatedAt = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var licenseExpiresAt = new DateTimeOffset(2027, 4, 4, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<Company>().HasData(
            new Company
            {
                Id = 1,
                Name = "Refline Demo Company",
                IsActive = true,
                CreatedAt = companyCreatedAt
            });

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                CompanyId = 1,
                FullName = "System Admin",
                Login = "admin",
                PasswordHash = PasswordHashHelper.ComputeHash("admin123"),
                Role = Enums.UserRole.Admin,
                ManagerId = null,
                IsActive = true,
                CreatedAt = companyCreatedAt
            },
            new User
            {
                Id = 2,
                CompanyId = 1,
                FullName = "Team Manager",
                Login = "manager",
                PasswordHash = PasswordHashHelper.ComputeHash("manager123"),
                Role = Enums.UserRole.Manager,
                ManagerId = 1,
                IsActive = true,
                CreatedAt = companyCreatedAt
            },
            new User
            {
                Id = 3,
                CompanyId = 1,
                FullName = "Regular Employee",
                Login = "employee",
                PasswordHash = PasswordHashHelper.ComputeHash("employee123"),
                Role = Enums.UserRole.Employee,
                ManagerId = 2,
                IsActive = true,
                CreatedAt = companyCreatedAt
            });

        modelBuilder.Entity<License>().HasData(
            new License
            {
                Id = 1,
                CompanyId = 1,
                LicenseKey = "REFLINE-DEMO-LICENSE-001",
                MaxDevices = 100,
                IssuedAt = companyCreatedAt,
                ExpiresAt = licenseExpiresAt,
                IsActive = true
            });
    }
}

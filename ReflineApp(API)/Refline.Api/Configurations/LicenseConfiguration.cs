using Refline.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refline.Api.Entities;

namespace Refline.Api.Configurations;

public sealed class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("licenses");

        builder.HasKey(license => license.Id);

        builder.Property(license => license.LicenseKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(license => license.MaxDevices)
            .IsRequired();

        builder.Property(license => license.LicenseType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(license => license.IssuedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(license => license.ExpiresAt)
            .IsRequired();

        builder.Property(license => license.IsActive)
            .IsRequired();

        builder.HasOne(license => license.Company)
            .WithMany(company => company.Licenses)
            .HasForeignKey(license => license.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(license => license.CompanyId);

        builder.HasIndex(license => license.LicenseKey)
            .IsUnique();

        builder.HasIndex(license => license.IsActive);
        builder.HasIndex(license => license.LicenseType);
    }
}

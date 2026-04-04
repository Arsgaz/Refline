using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refline.Api.Entities;

namespace Refline.Api.Configurations;

public sealed class DeviceActivationConfiguration : IEntityTypeConfiguration<DeviceActivation>
{
    public void Configure(EntityTypeBuilder<DeviceActivation> builder)
    {
        builder.ToTable("device_activations");

        builder.HasKey(activation => activation.Id);

        builder.Property(activation => activation.DeviceId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(activation => activation.MachineName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(activation => activation.ActivatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(activation => activation.LastSeenAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(activation => activation.IsRevoked)
            .IsRequired();

        builder.HasOne(activation => activation.License)
            .WithMany(license => license.DeviceActivations)
            .HasForeignKey(activation => activation.LicenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(activation => activation.User)
            .WithMany(user => user.DeviceActivations)
            .HasForeignKey(activation => activation.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(activation => activation.LicenseId);
        builder.HasIndex(activation => activation.UserId);

        builder.HasIndex(activation => new { activation.LicenseId, activation.DeviceId })
            .IsUnique();

        builder.HasIndex(activation => new { activation.UserId, activation.DeviceId });
        builder.HasIndex(activation => activation.IsRevoked);
        builder.HasIndex(activation => activation.LastSeenAt);
    }
}

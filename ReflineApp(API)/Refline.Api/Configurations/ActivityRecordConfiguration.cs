using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refline.Api.Entities;

namespace Refline.Api.Configurations;

public sealed class ActivityRecordConfiguration : IEntityTypeConfiguration<ActivityRecord>
{
    public void Configure(EntityTypeBuilder<ActivityRecord> builder)
    {
        builder.ToTable("activity_records");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.DeviceId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(activity => activity.AppName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(activity => activity.WindowTitle)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(activity => activity.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(activity => activity.IsIdle)
            .IsRequired();

        builder.Property(activity => activity.IsProductive)
            .IsRequired();

        builder.Property(activity => activity.DurationSeconds)
            .IsRequired();

        builder.Property(activity => activity.ActivityDate)
            .IsRequired();

        builder.Property(activity => activity.StartedAt)
            .IsRequired();

        builder.Property(activity => activity.EndedAt)
            .IsRequired();

        builder.HasOne(activity => activity.User)
            .WithMany(user => user.ActivityRecords)
            .HasForeignKey(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(activity => new { activity.UserId, activity.ActivityDate });
        builder.HasIndex(activity => new { activity.UserId, activity.DeviceId, activity.EndedAt });
        builder.HasIndex(activity => activity.Category);
        builder.HasIndex(activity => activity.IsProductive);
    }
}

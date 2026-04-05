using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refline.Api.Entities;

namespace Refline.Api.Configurations;

public sealed class ActivityClassificationRuleConfiguration : IEntityTypeConfiguration<ActivityClassificationRule>
{
    public void Configure(EntityTypeBuilder<ActivityClassificationRule> builder)
    {
        builder.ToTable("activity_classification_rules");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.AppNamePattern)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(rule => rule.WindowTitlePattern)
            .HasMaxLength(500);

        builder.Property(rule => rule.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(rule => rule.Priority)
            .IsRequired();

        builder.Property(rule => rule.IsEnabled)
            .IsRequired();

        builder.Property(rule => rule.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(rule => rule.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasOne(rule => rule.Company)
            .WithMany(company => company.ActivityClassificationRules)
            .HasForeignKey(rule => rule.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rule => rule.CompanyId);
        builder.HasIndex(rule => new { rule.CompanyId, rule.IsEnabled });
        builder.HasIndex(rule => new { rule.CompanyId, rule.Priority });
    }
}

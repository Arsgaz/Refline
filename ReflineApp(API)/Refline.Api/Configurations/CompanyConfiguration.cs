using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refline.Api.Entities;

namespace Refline.Api.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.IsActive)
            .IsRequired();

        builder.Property(company => company.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(company => company.Name);
        builder.HasIndex(company => company.IsActive);
    }
}

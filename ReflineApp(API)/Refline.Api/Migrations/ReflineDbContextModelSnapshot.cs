using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Refline.Api.Data;

#nullable disable

namespace Refline.Api.Migrations;

[DbContext(typeof(ReflineDbContext))]
partial class ReflineDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("Refline.Api.Entities.ActivityRecord", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

            b.Property<DateOnly>("ActivityDate")
                .HasColumnType("date");

            b.Property<string>("AppName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("Category")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.Property<string>("DeviceId")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<int>("DurationSeconds")
                .HasColumnType("integer");

            b.Property<bool>("IsIdle")
                .HasColumnType("boolean");

            b.Property<bool>("IsProductive")
                .HasColumnType("boolean");

            b.Property<DateTimeOffset>("EndedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTimeOffset>("StartedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<long>("UserId")
                .HasColumnType("bigint");

            b.Property<string>("WindowTitle")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("character varying(500)");

            b.HasKey("Id");

            b.HasIndex("Category");

            b.HasIndex("IsProductive");

            b.HasIndex("UserId", "ActivityDate");

            b.HasIndex("UserId", "DeviceId", "EndedAt");

            b.ToTable("activity_records", (string)null);
        });

        modelBuilder.Entity("Refline.Api.Entities.Company", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

            b.Property<DateTimeOffset>("CreatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            b.Property<bool>("IsActive")
                .HasColumnType("boolean");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.HasKey("Id");

            b.HasIndex("IsActive");

            b.HasIndex("Name");

            b.ToTable("companies", (string)null);

            b.HasData(new
            {
                Id = 1L,
                CreatedAt = new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                IsActive = true,
                Name = "Refline Demo Company"
            });
        });

        modelBuilder.Entity("Refline.Api.Entities.DeviceActivation", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

            b.Property<DateTimeOffset>("ActivatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            b.Property<string>("DeviceId")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<bool>("IsRevoked")
                .HasColumnType("boolean");

            b.Property<DateTimeOffset>("LastSeenAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            b.Property<long>("LicenseId")
                .HasColumnType("bigint");

            b.Property<string>("MachineName")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.Property<long>("UserId")
                .HasColumnType("bigint");

            b.HasKey("Id");

            b.HasIndex("IsRevoked");

            b.HasIndex("LastSeenAt");

            b.HasIndex("LicenseId");

            b.HasIndex("LicenseId", "DeviceId")
                .IsUnique();

            b.HasIndex("UserId");

            b.HasIndex("UserId", "DeviceId");

            b.ToTable("device_activations", (string)null);
        });

        modelBuilder.Entity("Refline.Api.Entities.License", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

            b.Property<long>("CompanyId")
                .HasColumnType("bigint");

            b.Property<DateTimeOffset>("ExpiresAt")
                .HasColumnType("timestamp with time zone");

            b.Property<bool>("IsActive")
                .HasColumnType("boolean");

            b.Property<DateTimeOffset>("IssuedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            b.Property<string>("LicenseKey")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<string>("LicenseType")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.Property<int>("MaxDevices")
                .HasColumnType("integer");

            b.HasKey("Id");

            b.HasIndex("CompanyId");

            b.HasIndex("IsActive");

            b.HasIndex("LicenseType");

            b.HasIndex("LicenseKey")
                .IsUnique();

            b.ToTable("licenses", (string)null);

            b.HasData(new
            {
                Id = 1L,
                CompanyId = 1L,
                ExpiresAt = new DateTimeOffset(new DateTime(2027, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                IsActive = true,
                IssuedAt = new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                LicenseKey = "REFLINE-DEMO-LICENSE-001",
                LicenseType = "Corporate",
                MaxDevices = 100
            });
        });

        modelBuilder.Entity("Refline.Api.Entities.User", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

            b.Property<long>("CompanyId")
                .HasColumnType("bigint");

            b.Property<DateTimeOffset>("CreatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            b.Property<string>("FullName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<bool>("IsActive")
                .HasColumnType("boolean");

            b.Property<string>("Login")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<long?>("ManagerId")
                .HasColumnType("bigint");

            b.Property<string>("PasswordHash")
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnType("character varying(512)");

            b.Property<string>("Role")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.HasKey("Id");

            b.HasIndex("CompanyId", "Login")
                .IsUnique();

            b.HasIndex("IsActive");

            b.HasIndex("ManagerId");

            b.HasIndex("Role");

            b.ToTable("users", (string)null);

            b.HasData(
                new
                {
                    Id = 1L,
                    CompanyId = 1L,
                    CreatedAt = new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                    FullName = "System Admin",
                    IsActive = true,
                    Login = "admin",
                    PasswordHash = "240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9",
                    Role = "Admin"
                },
                new
                {
                    Id = 2L,
                    CompanyId = 1L,
                    CreatedAt = new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                    FullName = "Team Manager",
                    IsActive = true,
                    Login = "manager",
                    ManagerId = 1L,
                    PasswordHash = "866485796CFA8D7C0CF7111640205B83076433547577511D81F8030AE99ECEA5",
                    Role = "Manager"
                },
                new
                {
                    Id = 3L,
                    CompanyId = 1L,
                    CreatedAt = new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                    FullName = "Regular Employee",
                    IsActive = true,
                    Login = "employee",
                    ManagerId = 2L,
                    PasswordHash = "5B2F8E27E2E5B4081C03CE70B288C87BD1263140CBD1BD9AE078123509B7CAFF",
                    Role = "Employee"
                });
        });

        modelBuilder.Entity("Refline.Api.Entities.ActivityRecord", b =>
        {
            b.HasOne("Refline.Api.Entities.User", "User")
                .WithMany("ActivityRecords")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("User");
        });

        modelBuilder.Entity("Refline.Api.Entities.DeviceActivation", b =>
        {
            b.HasOne("Refline.Api.Entities.License", "License")
                .WithMany("DeviceActivations")
                .HasForeignKey("LicenseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("Refline.Api.Entities.User", "User")
                .WithMany("DeviceActivations")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("License");
            b.Navigation("User");
        });

        modelBuilder.Entity("Refline.Api.Entities.License", b =>
        {
            b.HasOne("Refline.Api.Entities.Company", "Company")
                .WithMany("Licenses")
                .HasForeignKey("CompanyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Company");
        });

        modelBuilder.Entity("Refline.Api.Entities.User", b =>
        {
            b.HasOne("Refline.Api.Entities.Company", "Company")
                .WithMany("Users")
                .HasForeignKey("CompanyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("Refline.Api.Entities.User", "Manager")
                .WithMany("Subordinates")
                .HasForeignKey("ManagerId")
                .OnDelete(DeleteBehavior.SetNull);

            b.Navigation("Company");
            b.Navigation("Manager");
        });

        modelBuilder.Entity("Refline.Api.Entities.Company", b =>
        {
            b.Navigation("Licenses");
            b.Navigation("Users");
        });

        modelBuilder.Entity("Refline.Api.Entities.License", b =>
        {
            b.Navigation("DeviceActivations");
        });

        modelBuilder.Entity("Refline.Api.Entities.User", b =>
        {
            b.Navigation("ActivityRecords");
            b.Navigation("DeviceActivations");
            b.Navigation("Subordinates");
        });
#pragma warning restore 612, 618
    }
}

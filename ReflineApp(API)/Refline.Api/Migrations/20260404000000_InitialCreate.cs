using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Refline.Api.Data;

#nullable disable

namespace Refline.Api.Migrations;

[DbContext(typeof(ReflineDbContext))]
[Migration("20260404000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "companies",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_companies", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "licenses",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CompanyId = table.Column<long>(type: "bigint", nullable: false),
                LicenseKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                MaxDevices = table.Column<int>(type: "integer", nullable: false),
                LicenseType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_licenses", x => x.Id);
                table.ForeignKey(
                    name: "FK_licenses_companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CompanyId = table.Column<long>(type: "bigint", nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ManagerId = table.Column<long>(type: "bigint", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.Id);
                table.ForeignKey(
                    name: "FK_users_companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_users_users_ManagerId",
                    column: x => x.ManagerId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "activity_records",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AppName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                WindowTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsIdle = table.Column<bool>(type: "boolean", nullable: false),
                IsProductive = table.Column<bool>(type: "boolean", nullable: false),
                DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                ActivityDate = table.Column<DateOnly>(type: "date", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_activity_records", x => x.Id);
                table.ForeignKey(
                    name: "FK_activity_records_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "device_activations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LicenseId = table.Column<long>(type: "bigint", nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                MachineName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_device_activations", x => x.Id);
                table.ForeignKey(
                    name: "FK_device_activations_licenses_LicenseId",
                    column: x => x.LicenseId,
                    principalTable: "licenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_device_activations_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "companies",
            columns: new[] { "Id", "CreatedAt", "IsActive", "Name" },
            columnTypes: new[] { "bigint", "timestamp with time zone", "boolean", "character varying(200)" },
            values: new object[] { 1L, new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), true, "Refline Demo Company" });

        migrationBuilder.InsertData(
            table: "licenses",
            columns: new[] { "Id", "CompanyId", "ExpiresAt", "IsActive", "IssuedAt", "LicenseKey", "LicenseType", "MaxDevices" },
            columnTypes: new[] { "bigint", "bigint", "timestamp with time zone", "boolean", "timestamp with time zone", "character varying(128)", "character varying(32)", "integer" },
            values: new object[] { 1L, 1L, new DateTimeOffset(new DateTime(2027, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), true, new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), "REFLINE-DEMO-LICENSE-001", "Corporate", 100 });

        migrationBuilder.InsertData(
            table: "users",
            columns: new[] { "Id", "CompanyId", "CreatedAt", "FullName", "IsActive", "Login", "ManagerId", "PasswordHash", "Role" },
            columnTypes: new[] { "bigint", "bigint", "timestamp with time zone", "character varying(200)", "boolean", "character varying(100)", "bigint", "character varying(512)", "character varying(32)" },
            values: new object[] { 1L, 1L, new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), "System Admin", true, "admin", null, "240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9", "Admin" });

        migrationBuilder.InsertData(
            table: "users",
            columns: new[] { "Id", "CompanyId", "CreatedAt", "FullName", "IsActive", "Login", "ManagerId", "PasswordHash", "Role" },
            columnTypes: new[] { "bigint", "bigint", "timestamp with time zone", "character varying(200)", "boolean", "character varying(100)", "bigint", "character varying(512)", "character varying(32)" },
            values: new object[] { 2L, 1L, new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), "Team Manager", true, "manager", 1L, "866485796CFA8D7C0CF7111640205B83076433547577511D81F8030AE99ECEA5", "Manager" });

        migrationBuilder.InsertData(
            table: "users",
            columns: new[] { "Id", "CompanyId", "CreatedAt", "FullName", "IsActive", "Login", "ManagerId", "PasswordHash", "Role" },
            columnTypes: new[] { "bigint", "bigint", "timestamp with time zone", "character varying(200)", "boolean", "character varying(100)", "bigint", "character varying(512)", "character varying(32)" },
            values: new object[] { 3L, 1L, new DateTimeOffset(new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero), "Regular Employee", true, "employee", 2L, "5B2F8E27E2E5B4081C03CE70B288C87BD1263140CBD1BD9AE078123509B7CAFF", "Employee" });

        migrationBuilder.CreateIndex(
            name: "IX_activity_records_Category",
            table: "activity_records",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_activity_records_IsProductive",
            table: "activity_records",
            column: "IsProductive");

        migrationBuilder.CreateIndex(
            name: "IX_activity_records_UserId_ActivityDate",
            table: "activity_records",
            columns: new[] { "UserId", "ActivityDate" });

        migrationBuilder.CreateIndex(
            name: "IX_activity_records_UserId_DeviceId_EndedAt",
            table: "activity_records",
            columns: new[] { "UserId", "DeviceId", "EndedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_companies_IsActive",
            table: "companies",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_companies_Name",
            table: "companies",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_device_activations_IsRevoked",
            table: "device_activations",
            column: "IsRevoked");

        migrationBuilder.CreateIndex(
            name: "IX_device_activations_LastSeenAt",
            table: "device_activations",
            column: "LastSeenAt");

        migrationBuilder.CreateIndex(
            name: "IX_device_activations_LicenseId",
            table: "device_activations",
            column: "LicenseId");

        migrationBuilder.CreateIndex(
            name: "IX_device_activations_LicenseId_DeviceId",
            table: "device_activations",
            columns: new[] { "LicenseId", "DeviceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_device_activations_UserId",
            table: "device_activations",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_device_activations_UserId_DeviceId",
            table: "device_activations",
            columns: new[] { "UserId", "DeviceId" });

        migrationBuilder.CreateIndex(
            name: "IX_licenses_CompanyId",
            table: "licenses",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_licenses_IsActive",
            table: "licenses",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_licenses_LicenseType",
            table: "licenses",
            column: "LicenseType");

        migrationBuilder.CreateIndex(
            name: "IX_licenses_LicenseKey",
            table: "licenses",
            column: "LicenseKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_CompanyId_Login",
            table: "users",
            columns: new[] { "CompanyId", "Login" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_IsActive",
            table: "users",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_users_ManagerId",
            table: "users",
            column: "ManagerId");

        migrationBuilder.CreateIndex(
            name: "IX_users_Role",
            table: "users",
            column: "Role");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "activity_records");
        migrationBuilder.DropTable(name: "device_activations");
        migrationBuilder.DropTable(name: "licenses");
        migrationBuilder.DropTable(name: "users");
        migrationBuilder.DropTable(name: "companies");
    }
}

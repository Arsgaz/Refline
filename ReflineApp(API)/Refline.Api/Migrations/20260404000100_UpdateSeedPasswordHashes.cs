using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Refline.Api.Data;

#nullable disable

namespace Refline.Api.Migrations;

[DbContext(typeof(ReflineDbContext))]
[Migration("20260404000100_UpdateSeedPasswordHashes")]
public partial class UpdateSeedPasswordHashes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE users
            SET "PasswordHash" = '240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9'
            WHERE "Id" = 1;
            """);

        migrationBuilder.Sql("""
            UPDATE users
            SET "PasswordHash" = '866485796CFA8D7C0CF7111640205B83076433547577511D81F8030AE99ECEA5'
            WHERE "Id" = 2;
            """);

        migrationBuilder.Sql("""
            UPDATE users
            SET "PasswordHash" = '5B2F8E27E2E5B4081C03CE70B288C87BD1263140CBD1BD9AE078123509B7CAFF'
            WHERE "Id" = 3;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE users
            SET "PasswordHash" = 'seed-admin-password-hash'
            WHERE "Id" = 1;
            """);

        migrationBuilder.Sql("""
            UPDATE users
            SET "PasswordHash" = 'seed-manager-password-hash'
            WHERE "Id" = 2;
            """);

        migrationBuilder.Sql("""
            UPDATE users
            SET "PasswordHash" = 'seed-employee-password-hash'
            WHERE "Id" = 3;
            """);
    }
}

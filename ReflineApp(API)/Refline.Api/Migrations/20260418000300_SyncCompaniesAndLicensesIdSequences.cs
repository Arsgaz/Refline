using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Refline.Api.Data;

#nullable disable

namespace Refline.Api.Migrations;

[DbContext(typeof(ReflineDbContext))]
[Migration("20260418000300_SyncCompaniesAndLicensesIdSequences")]
public partial class SyncCompaniesAndLicensesIdSequences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            SELECT setval(
                pg_get_serial_sequence('"companies"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "companies"), 1),
                EXISTS (SELECT 1 FROM "companies"));
            """);

        migrationBuilder.Sql(
            """
            SELECT setval(
                pg_get_serial_sequence('"licenses"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "licenses"), 1),
                EXISTS (SELECT 1 FROM "licenses"));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}

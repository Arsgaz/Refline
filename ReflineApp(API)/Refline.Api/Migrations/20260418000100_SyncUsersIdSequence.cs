using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Refline.Api.Data;

#nullable disable

namespace Refline.Api.Migrations;

[DbContext(typeof(ReflineDbContext))]
[Migration("20260418000100_SyncUsersIdSequence")]
public partial class SyncUsersIdSequence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            SELECT setval(
                pg_get_serial_sequence('"users"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "users"), 1),
                EXISTS (SELECT 1 FROM "users"));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}

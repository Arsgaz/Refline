using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Refline.Api.Data;

#nullable disable

namespace Refline.Api.Migrations;

[DbContext(typeof(ReflineDbContext))]
[Migration("20260418000200_AddMustChangePassword")]
public partial class AddMustChangePassword : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "MustChangePassword",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MustChangePassword",
            table: "users");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Refline.Api.Data;

#nullable disable

namespace Refline.Api.Migrations;

[DbContext(typeof(ReflineDbContext))]
[Migration("20260405000000_AddActivityClassificationRules")]
public partial class AddActivityClassificationRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "activity_classification_rules",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CompanyId = table.Column<long>(type: "bigint", nullable: false),
                AppNamePattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                WindowTitlePattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_activity_classification_rules", x => x.Id);
                table.ForeignKey(
                    name: "FK_activity_classification_rules_companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_activity_classification_rules_CompanyId",
            table: "activity_classification_rules",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_activity_classification_rules_CompanyId_IsEnabled",
            table: "activity_classification_rules",
            columns: new[] { "CompanyId", "IsEnabled" });

        migrationBuilder.CreateIndex(
            name: "IX_activity_classification_rules_CompanyId_Priority",
            table: "activity_classification_rules",
            columns: new[] { "CompanyId", "Priority" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "activity_classification_rules");
    }
}

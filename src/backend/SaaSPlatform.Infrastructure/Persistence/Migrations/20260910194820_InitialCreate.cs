using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                    table.CheckConstraint("ck_tenants_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_tenants_slug_format", "slug ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_tenants_slug_not_blank", "length(btrim(slug)) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "uq_tenants_slug",
                schema: "public",
                table: "tenants",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenants",
                schema: "public");
        }
    }
}

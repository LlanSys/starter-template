using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.IamPostgreSqlDB
{
    /// <inheritdoc />
    public partial class RenameTenantSettingsToOrgSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20107"),
                columns: new[] { "Key", "Title", "Url" },
                values: new object[] { "admin-org-settings", "Org Settings", "/admin/org-settings" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10619"),
                columns: new[] { "Key", "Label", "Url" },
                values: new object[] { "admin-org-settings", "Org Settings", "/admin/org-settings" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20107"),
                columns: new[] { "Key", "Title", "Url" },
                values: new object[] { "admin-tenant-settings", "Tenant Settings", "/admin/tenant-settings" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10619"),
                columns: new[] { "Key", "Label", "Url" },
                values: new object[] { "admin-tenant-settings", "Tenant Settings", "/admin/tenant-settings" });
        }
    }
}

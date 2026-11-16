using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class CompleteTenantSettingsAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000021"),
                column: "PermissionKeys",
                value: "[\"users.view\",\"users.create\",\"users.edit\",\"users.deactivate\",\"users.manage_roles\",\"users.manage_permissions\",\"roles.view\",\"roles.create\",\"roles.edit\",\"roles.delete\",\"roles.manage_permissions\",\"menus.view\",\"menus.create\",\"menus.edit\",\"menus.delete\",\"tasks.assign\",\"tenant-settings.view\",\"tenant-settings.create\",\"tenant-settings.update\",\"tenant-settings.delete\",\"support.approve\"]");

            migrationBuilder.InsertData(
                table: "ModuleCatalogue",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DisplayOrder", "FeatureFlag", "Icon", "IsActive", "Key", "Kind", "ModuleKey", "ParentKey", "Path", "PermissionKeys", "Placement", "RoleNames", "ShowInNavigation", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0194f700-0000-7000-8000-000000000096"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "tenant-settings.view", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "View workspace organization settings.", null, null, 1 },
                    { new Guid("0194f700-0000-7000-8000-000000000097"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "tenant-settings.create", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Create workspace organization settings.", null, null, 1 },
                    { new Guid("0194f700-0000-7000-8000-000000000098"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "tenant-settings.update", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Update workspace organization settings.", null, null, 1 },
                    { new Guid("0194f700-0000-7000-8000-000000000099"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "tenant-settings.delete", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Delete workspace organization settings.", null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000096"));

            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000097"));

            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000098"));

            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000099"));

            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000021"),
                column: "PermissionKeys",
                value: "[\"users.view\",\"users.create\",\"users.edit\",\"users.deactivate\",\"users.manage_roles\",\"users.manage_permissions\",\"roles.view\",\"roles.create\",\"roles.edit\",\"roles.delete\",\"roles.manage_permissions\",\"menus.view\",\"menus.create\",\"menus.edit\",\"menus.delete\",\"tasks.assign\",\"support.approve\"]");
        }
    }
}

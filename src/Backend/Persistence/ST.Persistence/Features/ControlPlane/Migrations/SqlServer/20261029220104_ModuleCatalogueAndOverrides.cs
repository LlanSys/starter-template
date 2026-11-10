using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class ModuleCatalogueAndOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DependencyKeys",
                table: "ModuleDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "ModuleCatalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    ParentKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Placement = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ShowInNavigation = table.Column<bool>(type: "bit", nullable: false),
                    PermissionKeys = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleNames = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeatureFlag = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleCatalogue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleFeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleFeatureFlags", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ModuleCatalogue",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DisplayOrder", "FeatureFlag", "Icon", "IsActive", "Key", "Kind", "ModuleKey", "ParentKey", "Path", "PermissionKeys", "Placement", "RoleNames", "ShowInNavigation", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10101"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "users.view", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "View application users.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10102"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "users.create", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Create application users.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10103"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "users.edit", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Update application users.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10104"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "users.deactivate", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Deactivate application users.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10105"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "users.manage_roles", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Manage user role assignments.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10106"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "users.manage_permissions", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Manage direct user permissions.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10201"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "roles.view", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "View platform roles.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10202"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "roles.create", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Create platform roles.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10203"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "roles.edit", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Update platform roles.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10204"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "roles.delete", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Delete platform roles.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10205"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "roles.manage_permissions", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Manage role permission assignments.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10601"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "menus.view", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "View menu catalog.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10602"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "menus.create", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Create menu items.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10603"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "menus.edit", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Update menu items.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10604"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "menus.delete", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Delete menu items.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10701"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "payments.view", 1, "payments", null, "", "[]", "Sidebar", "[]", false, "View payment history and provider readiness.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10702"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "payments.create", 1, "payments", null, "", "[]", "Sidebar", "[]", false, "Initiate payment checkout flows.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10703"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "payments.admin", 1, "payments", null, "", "[]", "Sidebar", "[]", false, "Manage M-Pesa payment administration actions.", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10101"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 2, null, "Dashboard", true, "dashboard", 0, "core", null, "/dashboard", "[]", "Sidebar", "[]", true, "Dashboard", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 3, null, "AdminPanelSettings", true, "admin-center", 0, "core", null, "/admin", "[]", "Sidebar", "[]", true, "Admin Center", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10301"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 1, null, "AutoStories", true, "solution-overview", 0, "showcase", null, "/overview", "[]", "Sidebar", "[]", true, "Solution Overview", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 4, null, "CreditCard", true, "features", 0, "payments", null, "/payments", "[\"payments.view\"]", "Sidebar", "[]", true, "Payments", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 10, null, "CreditCard", true, "features-payments", 0, "payments", "features", "/payments", "[\"payments.view\"]", "Features", "[]", true, "Payments", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20104"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 40, null, "MenuOpen", true, "admin-menus", 0, "iam", "admin-center", "/admin/menus", "[]", "AdminCenter", "[]", true, "Menus", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20107"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 50, null, "Settings", true, "admin-org-settings", 0, "core", "admin-center", "/admin/org-settings", "[]", "AdminCenter", "[]", true, "Org Settings", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20110"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 70, null, "LockPerson", true, "admin-access-catalog", 0, "iam", "admin-center", "/admin/access-catalog", "[]", "AdminCenter", "[]", true, "Access Catalog", null, null, 1 },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 60, null, "Group", true, "admin-iam", 0, "iam", "admin-center", "/admin/iam", "[]", "AdminCenter", "[]", true, "Identity & Access", null, null, 1 },
                    { new Guid("0194f700-0000-7000-8000-000000000021"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "iam-administrator", 2, "iam", null, "", "[\"users.view\",\"users.create\",\"users.edit\",\"users.deactivate\",\"users.manage_roles\",\"users.manage_permissions\",\"roles.view\",\"roles.create\",\"roles.edit\",\"roles.delete\",\"roles.manage_permissions\",\"menus.view\",\"menus.create\",\"menus.edit\",\"menus.delete\"]", "Sidebar", "[]", false, "Tenant Administrator", null, null, 1 },
                    { new Guid("0194f700-0000-7000-8000-000000000022"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "payments-viewer", 2, "payments", null, "", "[\"payments.view\"]", "Sidebar", "[]", false, "Payments Viewer", null, null, 1 }
                });

            migrationBuilder.UpdateData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000011"),
                column: "DependencyKeys",
                value: "[]");

            migrationBuilder.UpdateData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000012"),
                column: "DependencyKeys",
                value: "[\"core\"]");

            migrationBuilder.UpdateData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000013"),
                column: "DependencyKeys",
                value: "[\"core\"]");

            migrationBuilder.UpdateData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000014"),
                column: "DependencyKeys",
                value: "[\"core\"]");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCatalogue_Key",
                table: "ModuleCatalogue",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCatalogue_ModuleKey_Kind",
                table: "ModuleCatalogue",
                columns: new[] { "ModuleKey", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleFeatureFlags_ScopeTenantId_ModuleKey_Key",
                table: "ModuleFeatureFlags",
                columns: new[] { "ScopeTenantId", "ModuleKey", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleCatalogue");

            migrationBuilder.DropTable(
                name: "ModuleFeatureFlags");

            migrationBuilder.DropColumn(
                name: "DependencyKeys",
                table: "ModuleDefinitions");
        }
    }
}

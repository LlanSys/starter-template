using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class TenantCatalogueScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_Permissions_Key",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_PermissionResources_Key",
                table: "PermissionResources");

            migrationBuilder.DropIndex(
                name: "UX_PermissionResources_Context_Key",
                table: "PermissionResources");

            migrationBuilder.DropIndex(
                name: "UX_PermissionContexts_Key",
                table: "PermissionContexts");

            migrationBuilder.DropIndex(
                name: "UX_PermissionActions_Key",
                table: "PermissionActions");

            migrationBuilder.DropIndex(
                name: "UX_MenuRoutes_Key",
                table: "MenuRoutes");

            migrationBuilder.DropIndex(
                name: "UX_MenuRoutes_Url",
                table: "MenuRoutes");

            migrationBuilder.DropIndex(
                name: "UX_MenuPlacements_Key",
                table: "MenuPlacements");

            migrationBuilder.DropIndex(
                name: "UX_MenuItems_Key",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "UX_MenuIcons_Key",
                table: "MenuIcons");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
DO $$ BEGIN IF EXISTS (SELECT ur."RoleId" FROM "UserRoles" ur JOIN "Users" u ON u."Id" = ur."UserId" GROUP BY ur."RoleId" HAVING COUNT(DISTINCT u."TenantId") > 1) THEN RAISE EXCEPTION 'Shared roles require an explicit tenant ownership migration before this upgrade.'; END IF; END $$;
UPDATE "Roles" r SET "TenantId" = COALESCE((SELECT u."TenantId" FROM "UserRoles" ur JOIN "Users" u ON u."Id" = ur."UserId" WHERE ur."RoleId" = r."Id" LIMIT 1), '0194f700-0000-7000-8000-000000000001'::uuid);
""");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "UX_Roles_Tenant_Name",
                table: "Roles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Permissions_Key",
                table: "Permissions",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionResources_Key",
                table: "PermissionResources",
                columns: new[] { "TenantId", "Key" });

            migrationBuilder.CreateIndex(
                name: "UX_PermissionResources_Context_Key",
                table: "PermissionResources",
                columns: new[] { "TenantId", "ContextKey", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PermissionContexts_Key",
                table: "PermissionContexts",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PermissionActions_Key",
                table: "PermissionActions",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuRoutes_Key",
                table: "MenuRoutes",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuRoutes_Url",
                table: "MenuRoutes",
                columns: new[] { "TenantId", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuPlacements_Key",
                table: "MenuPlacements",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuItems_Key",
                table: "MenuItems",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuIcons_Key",
                table: "MenuIcons",
                columns: new[] { "TenantId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_Roles_Tenant_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_Permissions_Key",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_PermissionResources_Key",
                table: "PermissionResources");

            migrationBuilder.DropIndex(
                name: "UX_PermissionResources_Context_Key",
                table: "PermissionResources");

            migrationBuilder.DropIndex(
                name: "UX_PermissionContexts_Key",
                table: "PermissionContexts");

            migrationBuilder.DropIndex(
                name: "UX_PermissionActions_Key",
                table: "PermissionActions");

            migrationBuilder.DropIndex(
                name: "UX_MenuRoutes_Key",
                table: "MenuRoutes");

            migrationBuilder.DropIndex(
                name: "UX_MenuRoutes_Url",
                table: "MenuRoutes");

            migrationBuilder.DropIndex(
                name: "UX_MenuPlacements_Key",
                table: "MenuPlacements");

            migrationBuilder.DropIndex(
                name: "UX_MenuItems_Key",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "UX_MenuIcons_Key",
                table: "MenuIcons");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Roles");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Permissions_Key",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionResources_Key",
                table: "PermissionResources",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "UX_PermissionResources_Context_Key",
                table: "PermissionResources",
                columns: new[] { "ContextKey", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PermissionContexts_Key",
                table: "PermissionContexts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PermissionActions_Key",
                table: "PermissionActions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuRoutes_Key",
                table: "MenuRoutes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuRoutes_Url",
                table: "MenuRoutes",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuPlacements_Key",
                table: "MenuPlacements",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuItems_Key",
                table: "MenuItems",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuIcons_Key",
                table: "MenuIcons",
                column: "Key",
                unique: true);
        }
    }
}

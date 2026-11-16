using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class CompleteTenantSettingsAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Upgrade only the trusted provisioning-owned role; copied templates and labels receive no grant.
            migrationBuilder.Sql("""
INSERT INTO [RoleClaims] ([RoleId], [ClaimType], [ClaimValue])
SELECT role.[Id], 'permission', capability.permission_key
FROM [Roles] AS role
CROSS JOIN (VALUES ('tenant-settings.view'), ('tenant-settings.create'), ('tenant-settings.update'), ('tenant-settings.delete')) capability(permission_key)
WHERE role.[Name] = 'Tenant Administrator' AND role.[NormalizedName] = 'TENANT ADMINISTRATOR'
  AND role.[TemplateKey] = 'iam-administrator' AND role.[IsDeleted] = 0
  AND NOT EXISTS (SELECT 1 FROM [RoleClaims] claim WHERE claim.[RoleId] = role.[Id]
      AND claim.[ClaimType] = 'permission' AND claim.[ClaimValue] = capability.permission_key);
""");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "ModuleKey", "Resource", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0194f700-0000-7000-8000-000000000096"), "view", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "View workspace organization settings.", true, false, "tenant-settings.view", "iam", "tenant-settings", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000097"), "create", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Create workspace organization settings.", true, false, "tenant-settings.create", "iam", "tenant-settings", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000098"), "update", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Update workspace organization settings.", true, false, "tenant-settings.update", "iam", "tenant-settings", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000099"), "delete", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Delete workspace organization settings.", true, false, "tenant-settings.delete", "iam", "tenant-settings", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000096"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000097"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000098"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000099"));
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddPlatformStaffAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformStaffChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviousStaff = table.Column<bool>(type: "bit", nullable: false),
                    CurrentStaff = table.Column<bool>(type: "bit", nullable: false),
                    PreviousActive = table.Column<bool>(type: "bit", nullable: false),
                    CurrentActive = table.Column<bool>(type: "bit", nullable: false),
                    PreviousPermissionsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    CurrentPermissionsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformStaffChanges", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "ModuleKey", "Resource", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0194f700-0000-7000-8000-000000000094"), "view_platform_staff", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "View platform staff and their access history.", true, false, "control_plane.view_platform_staff", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000095"), "manage_platform_staff", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Manage platform staff membership and operation grants.", true, false, "control_plane.manage_platform_staff", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            // Carry forward only explicitly designated staff with every prior platform grant. Role labels confer nothing.
            migrationBuilder.Sql("""
INSERT INTO [UserClaims] (UserId, ClaimType, ClaimValue)
SELECT u.Id, 'permission', n.permission_key
FROM [Users] u
CROSS JOIN (VALUES ('control_plane.view_platform_staff'), ('control_plane.manage_platform_staff')) n(permission_key)
WHERE u.TenantId = '0194f700-0000-7000-8000-000000000001'
  AND u.Kind = 'Human' AND u.IsActive = 1 AND u.IsDeleted <> 1
  AND EXISTS (SELECT 1 FROM [UserClaims] s WHERE s.UserId = u.Id AND s.ClaimType = 'platform_staff' AND s.ClaimValue = 'true')
  AND NOT EXISTS (SELECT 1 FROM (VALUES ('control_plane.manage_tasks'),('control_plane.view_tenants'),('control_plane.create_tenants'),('control_plane.edit_tenants'),('control_plane.manage_tenant_administrators'),('control_plane.manage_tenant_lifecycle'),('control_plane.manage_tenant_modules'),('control_plane.migrate_tenants'),('control_plane.view_stamps'),('control_plane.manage_stamps'),('control_plane.view_modules'),('control_plane.manage_modules'),('control_plane.view_audit'),('control_plane.view_support'),('control_plane.manage_support'),('control_plane.enter_demo'),('control_plane.reset_demo'),('control_plane.view_kyc'),('control_plane.upload_kyc_documents'),('control_plane.download_kyc_documents'),('control_plane.review_kyc_documents'),('control_plane.manage_kyc_submissions'),('control_plane.approve_kyc'),('control_plane.manage_kyc_retention'),('control_plane.manage_kyc_holds')) required(permission_key)
      WHERE NOT EXISTS (SELECT 1 FROM [UserClaims] p WHERE p.UserId = u.Id AND p.ClaimType = 'permission' AND p.ClaimValue = required.permission_key))
  AND NOT EXISTS (SELECT 1 FROM [UserClaims] p WHERE p.UserId = u.Id AND p.ClaimType = 'permission' AND p.ClaimValue = n.permission_key);
""");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformStaffChanges_OccurredAt",
                table: "PlatformStaffChanges",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformStaffChanges_UserId_OccurredAt",
                table: "PlatformStaffChanges",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DELETE FROM [UserClaims] WHERE ClaimType = 'permission' AND ClaimValue IN ('control_plane.view_platform_staff', 'control_plane.manage_platform_staff');
""");

            migrationBuilder.DropTable(
                name: "PlatformStaffChanges");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000094"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000095"));
        }
    }
}

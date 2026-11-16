using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class GranularPlatformPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve existing trusted staff authority through explicit operation grants.
            migrationBuilder.Sql("""
                INSERT INTO [UserClaims] ([UserId], [ClaimType], [ClaimValue])
                SELECT DISTINCT u.[Id], 'permission', p.[Permission]
                FROM [Users] u
                CROSS JOIN (VALUES ('control_plane.view_tenants'),('control_plane.create_tenants'),('control_plane.edit_tenants'),('control_plane.manage_tenant_administrators'),('control_plane.manage_tenant_lifecycle'),('control_plane.manage_tenant_modules'),('control_plane.migrate_tenants'),('control_plane.view_stamps'),('control_plane.manage_stamps'),('control_plane.view_modules'),('control_plane.manage_modules'),('control_plane.view_audit'),('control_plane.view_support'),('control_plane.manage_support'),('control_plane.enter_demo'),('control_plane.reset_demo'),('control_plane.view_kyc'),('control_plane.upload_kyc_documents'),('control_plane.download_kyc_documents'),('control_plane.review_kyc_documents'),('control_plane.manage_kyc_submissions'),('control_plane.approve_kyc'),('control_plane.manage_kyc_retention'),('control_plane.manage_kyc_holds')) p([Permission])
                WHERE u.[TenantId] = '0194f700-0000-7000-8000-000000000001'
                AND EXISTS (SELECT 1 FROM [UserClaims] c WHERE c.[UserId]=u.[Id] AND c.[ClaimType]='platform_staff' AND c.[ClaimValue]='true')
                AND ((p.[Permission] IN ('control_plane.view_tenants','control_plane.create_tenants','control_plane.edit_tenants','control_plane.manage_tenant_administrators','control_plane.manage_tenant_lifecycle','control_plane.manage_tenant_modules','control_plane.migrate_tenants','control_plane.view_stamps','control_plane.manage_stamps','control_plane.view_modules','control_plane.manage_modules','control_plane.view_audit','control_plane.view_support','control_plane.manage_support','control_plane.enter_demo','control_plane.reset_demo')
                AND EXISTS (SELECT 1 FROM [UserClaims] c WHERE c.[UserId]=u.[Id] AND c.[ClaimType]='permission' AND c.[ClaimValue]='control_plane.manage'))
                OR (p.[Permission] IN ('control_plane.view_kyc','control_plane.upload_kyc_documents','control_plane.download_kyc_documents','control_plane.review_kyc_documents','control_plane.manage_kyc_submissions','control_plane.approve_kyc')
                AND EXISTS (SELECT 1 FROM [UserClaims] c WHERE c.[UserId]=u.[Id] AND c.[ClaimType]='permission' AND c.[ClaimValue]='control_plane.approve_kyc'))
                OR (p.[Permission] IN ('control_plane.manage_kyc_retention','control_plane.manage_kyc_holds')
                AND EXISTS (SELECT 1 FROM [UserClaims] c WHERE c.[UserId]=u.[Id] AND c.[ClaimValue]='control_plane.manage' AND c.[ClaimType]='permission')
                AND EXISTS (SELECT 1 FROM [UserClaims] c WHERE c.[UserId]=u.[Id] AND c.[ClaimValue]='control_plane.approve_kyc' AND c.[ClaimType]='permission')))
                AND NOT EXISTS (SELECT 1 FROM [UserClaims] c WHERE c.[UserId]=u.[Id] AND c.[ClaimType]='permission' AND c.[ClaimValue]=p.[Permission]);
                DELETE FROM [UserClaims] WHERE [ClaimType]='permission' AND [ClaimValue]='control_plane.manage';
                DELETE FROM [RoleClaims] WHERE [ClaimType]='permission' AND [ClaimValue]='control_plane.manage';
                """);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10801"));

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"),
                column: "RequiredPermissionKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30101"),
                column: "RequiredPermissionKey",
                value: "control_plane.view_tenants");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30102"),
                column: "RequiredPermissionKey",
                value: "control_plane.view_stamps");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "ModuleKey", "Resource", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12001"), "view_tenants", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: view tenants.", true, false, "control_plane.view_tenants", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12002"), "create_tenants", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: create tenants.", true, false, "control_plane.create_tenants", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12003"), "edit_tenants", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: edit tenants.", true, false, "control_plane.edit_tenants", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12004"), "manage_tenant_administrators", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage tenant administrators.", true, false, "control_plane.manage_tenant_administrators", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12005"), "manage_tenant_lifecycle", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage tenant lifecycle.", true, false, "control_plane.manage_tenant_lifecycle", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12006"), "manage_tenant_modules", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage tenant modules.", true, false, "control_plane.manage_tenant_modules", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12007"), "migrate_tenants", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: migrate tenants.", true, false, "control_plane.migrate_tenants", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12008"), "view_stamps", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: view stamps.", true, false, "control_plane.view_stamps", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12009"), "manage_stamps", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage stamps.", true, false, "control_plane.manage_stamps", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12010"), "view_modules", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: view modules.", true, false, "control_plane.view_modules", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12011"), "manage_modules", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage modules.", true, false, "control_plane.manage_modules", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12012"), "view_audit", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: view audit.", true, false, "control_plane.view_audit", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12013"), "view_support", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: view support.", true, false, "control_plane.view_support", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12014"), "manage_support", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage support.", true, false, "control_plane.manage_support", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12015"), "enter_demo", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: enter demo.", true, false, "control_plane.enter_demo", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12016"), "reset_demo", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: reset demo.", true, false, "control_plane.reset_demo", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12017"), "view_kyc", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: view kyc.", true, false, "control_plane.view_kyc", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12018"), "upload_kyc_documents", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: upload kyc documents.", true, false, "control_plane.upload_kyc_documents", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12019"), "download_kyc_documents", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: download kyc documents.", true, false, "control_plane.download_kyc_documents", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12020"), "review_kyc_documents", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: review kyc documents.", true, false, "control_plane.review_kyc_documents", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12021"), "manage_kyc_submissions", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage kyc submissions.", true, false, "control_plane.manage_kyc_submissions", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12022"), "approve_kyc", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: approve kyc.", true, false, "control_plane.approve_kyc", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12023"), "manage_kyc_retention", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage kyc retention.", true, false, "control_plane.manage_kyc_retention", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12024"), "manage_kyc_holds", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, "Platform operation: manage kyc holds.", true, false, "control_plane.manage_kyc_holds", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12001"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12002"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12003"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12004"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12005"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12006"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12007"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12008"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12009"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12010"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12011"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12012"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12013"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12014"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12015"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12016"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12017"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12018"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12019"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12020"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12021"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12022"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12023"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b12024"));

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"),
                column: "RequiredPermissionKey",
                value: "control_plane.manage");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30101"),
                column: "RequiredPermissionKey",
                value: "control_plane.manage");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30102"),
                column: "RequiredPermissionKey",
                value: "control_plane.manage");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "ModuleKey", "Resource", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10801"), "manage", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage Control Plane Tenants and Stamps.", true, false, "control_plane.manage", "iam", "control_plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null });
        }
    }
}

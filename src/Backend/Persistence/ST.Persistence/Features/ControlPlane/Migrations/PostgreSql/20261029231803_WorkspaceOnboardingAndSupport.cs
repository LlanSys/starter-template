using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class WorkspaceOnboardingAndSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdministratorEmail",
                table: "Tenants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorUserId",
                table: "Tenants",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DemoFixtureVersion",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DemoResetAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvitationSentAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WorkspaceInitializedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceVersion",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SupportGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PermissionKeys = table.Column<string[]>(type: "text[]", nullable: false),
                    ReadOnly = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProxyUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportAudit_SupportGrants_GrantId",
                        column: x => x.GrantId,
                        principalTable: "SupportGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20104"),
                column: "Title",
                value: "Navigation");

            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000021"),
                column: "PermissionKeys",
                value: new[] { "users.view", "users.create", "users.edit", "users.deactivate", "users.manage_roles", "users.manage_permissions", "roles.view", "roles.create", "roles.edit", "roles.delete", "roles.manage_permissions", "menus.view", "menus.create", "menus.edit", "menus.delete", "support.approve" });

            migrationBuilder.InsertData(
                table: "ModuleCatalogue",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DisplayOrder", "FeatureFlag", "Icon", "IsActive", "Key", "Kind", "ModuleKey", "ParentKey", "Path", "PermissionKeys", "Placement", "RoleNames", "RowVersion", "ShowInNavigation", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0194f700-0000-7000-8000-000000000023"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "support.approve", 1, "iam", null, "", new string[0], "Sidebar", new string[0], new byte[0], false, "Approve and revoke customer support", null, null, 1 },
                    { new Guid("0194f700-0000-7000-8000-000000000024"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 90, null, "SupportAgent", true, "support-access", 0, "iam", "admin-center", "/admin/support", new[] { "support.approve" }, "AdminCenter", new string[0], new byte[0], true, "Support access", null, null, 1 }
                });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"),
                columns: new[] { "AdministratorEmail", "AdministratorUserId", "DemoFixtureVersion", "DemoResetAt", "InvitationSentAt", "WorkspaceInitializedAt", "WorkspaceVersion" },
                values: new object[] { null, null, 0, null, null, null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_SupportAudit_GrantId_CreatedAt",
                table: "SupportAudit",
                columns: new[] { "GrantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportGrants_WorkspaceId_Status",
                table: "SupportGrants",
                columns: new[] { "WorkspaceId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportAudit");

            migrationBuilder.DropTable(
                name: "SupportGrants");

            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000024"));

            migrationBuilder.DropColumn(
                name: "AdministratorEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AdministratorUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DemoFixtureVersion",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DemoResetAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "InvitationSentAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WorkspaceInitializedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WorkspaceVersion",
                table: "Tenants");

            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20104"),
                column: "Title",
                value: "Menus");

            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000021"),
                column: "PermissionKeys",
                value: new[] { "users.view", "users.create", "users.edit", "users.deactivate", "users.manage_roles", "users.manage_permissions", "roles.view", "roles.create", "roles.edit", "roles.delete", "roles.manage_permissions", "menus.view", "menus.create", "menus.edit", "menus.delete" });
        }
    }
}

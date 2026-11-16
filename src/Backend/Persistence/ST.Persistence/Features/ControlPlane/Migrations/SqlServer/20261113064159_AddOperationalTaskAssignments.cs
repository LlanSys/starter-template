using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddOperationalTaskAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalTaskAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OwnerTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectLabel = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssigneeUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalTaskAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationalTaskAssignmentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PreviousAssigneeUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AssigneeUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalTaskAssignmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalTaskAssignmentEvents_OperationalTaskAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "OperationalTaskAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000021"),
                column: "PermissionKeys",
                value: "[\"users.view\",\"users.create\",\"users.edit\",\"users.deactivate\",\"users.manage_roles\",\"users.manage_permissions\",\"roles.view\",\"roles.create\",\"roles.edit\",\"roles.delete\",\"roles.manage_permissions\",\"menus.view\",\"menus.create\",\"menus.edit\",\"menus.delete\",\"tasks.assign\",\"support.approve\"]");

            migrationBuilder.InsertData(
                table: "ModuleCatalogue",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DisplayOrder", "FeatureFlag", "Icon", "IsActive", "Key", "Kind", "ModuleKey", "ParentKey", "Path", "PermissionKeys", "Placement", "RoleNames", "ShowInNavigation", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0194f700-0000-7000-8000-000000000091"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "work-tasks", 0, "core", null, "/tasks", "[]", "Sidebar", "[]", false, "Tasks", null, null, 1 },
                    { new Guid("0194f700-0000-7000-8000-000000000092"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", 0, null, "Menu", true, "tasks.assign", 1, "iam", null, "", "[]", "Sidebar", "[]", false, "Assign operational tasks within this workspace.", null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTaskAssignmentEvents_AssignmentId_CreatedAt",
                table: "OperationalTaskAssignmentEvents",
                columns: new[] { "AssignmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTaskAssignments_Kind_SubjectId_CycleId_CycleAt",
                table: "OperationalTaskAssignments",
                columns: new[] { "Kind", "SubjectId", "CycleId", "CycleAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTaskAssignments_OwnerTenantId_AssigneeUserId_CompletedAt",
                table: "OperationalTaskAssignments",
                columns: new[] { "OwnerTenantId", "AssigneeUserId", "CompletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalTaskAssignmentEvents");

            migrationBuilder.DropTable(
                name: "OperationalTaskAssignments");

            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000091"));

            migrationBuilder.DeleteData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000092"));

            migrationBuilder.UpdateData(
                table: "ModuleCatalogue",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000021"),
                column: "PermissionKeys",
                value: "[\"users.view\",\"users.create\",\"users.edit\",\"users.deactivate\",\"users.manage_roles\",\"users.manage_permissions\",\"roles.view\",\"roles.create\",\"roles.edit\",\"roles.delete\",\"roles.manage_permissions\",\"menus.view\",\"menus.create\",\"menus.edit\",\"menus.delete\",\"support.approve\"]");
        }
    }
}

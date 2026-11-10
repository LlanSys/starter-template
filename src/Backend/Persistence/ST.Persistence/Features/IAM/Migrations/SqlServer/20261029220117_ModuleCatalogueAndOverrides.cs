using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class ModuleCatalogueAndOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModuleKey",
                table: "Roles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                table: "Roles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateVersion",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModuleKey",
                table: "Permissions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "iam");

            migrationBuilder.Sql("UPDATE [Permissions] SET [ModuleKey] = 'payments' WHERE [Resource] = 'payments';");

            migrationBuilder.CreateTable(
                name: "NavigationOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PageKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Placement = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    UsePlatformDefaults = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RoleNames = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PermissionKeys = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationOverrides", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10101"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10102"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10103"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10104"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10105"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10106"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10201"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10202"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10203"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10204"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10205"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10601"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10602"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10603"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10604"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10701"),
                column: "ModuleKey",
                value: "payments");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10702"),
                column: "ModuleKey",
                value: "payments");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10703"),
                column: "ModuleKey",
                value: "payments");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10801"),
                column: "ModuleKey",
                value: "iam");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationOverrides_TenantId_PageKey",
                table: "NavigationOverrides",
                columns: new[] { "TenantId", "PageKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NavigationOverrides");

            migrationBuilder.DropColumn(
                name: "ModuleKey",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TemplateKey",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TemplateVersion",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ModuleKey",
                table: "Permissions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class PaymentsModuleLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRetired",
                table: "ModuleDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000011"),
                column: "IsRetired",
                value: false);

            migrationBuilder.UpdateData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000012"),
                column: "IsRetired",
                value: false);

            migrationBuilder.UpdateData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000013"),
                column: "IsRetired",
                value: false);

            migrationBuilder.InsertData(
                table: "ModuleDefinitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "IsRetired", "Key", "Name", "RowVersion", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("0194f700-0000-7000-8000-000000000014"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", "Payment history, checkout and provider administration.", true, false, "Payments", "Payments", new byte[0], null, null });

            migrationBuilder.InsertData(
                table: "TenantModules",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "ExpiresAt", "IsActive", "ModuleKey", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("0194f700-0000-7000-8000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "Payments", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ModuleDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000006"));

            migrationBuilder.DropColumn(
                name: "IsRetired",
                table: "ModuleDefinitions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MenuVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "RequiredPermissionKeys",
                table: "MenuItems",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "RequiredRoleNames",
                table: "MenuItems",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10101"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10301"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20104"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20107"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20110"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20111"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30101"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30102"),
                columns: new[] { "RequiredPermissionKeys", "RequiredRoleNames" },
                values: new object[] { new string[0], new string[0] });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredPermissionKeys",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "RequiredRoleNames",
                table: "MenuItems");
        }
    }
}

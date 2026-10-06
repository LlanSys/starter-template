using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class MovePaymentsUnderFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"),
                column: "Placement",
                value: "Features");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"),
                column: "Placement",
                value: "Sidebar");
        }
    }
}

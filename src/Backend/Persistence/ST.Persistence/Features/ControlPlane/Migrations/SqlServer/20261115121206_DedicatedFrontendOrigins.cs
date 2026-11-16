using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class DedicatedFrontendOrigins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DedicatedFrontendOrigin",
                table: "Tenants",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"),
                column: "DedicatedFrontendOrigin",
                value: null);

            migrationBuilder.CreateIndex(
                name: "UX_Tenants_DedicatedFrontendOrigin",
                table: "Tenants",
                column: "DedicatedFrontendOrigin",
                unique: true,
                filter: "[DedicatedFrontendOrigin] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Tenants_DedicatedFrontendOrigin",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DedicatedFrontendOrigin",
                table: "Tenants");
        }
    }
}

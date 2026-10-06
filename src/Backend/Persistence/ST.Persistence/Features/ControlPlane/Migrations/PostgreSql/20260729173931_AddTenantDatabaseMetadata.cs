using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddTenantDatabaseMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatabaseConnectionString",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseProvider",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseConnectionString",
                table: "DeploymentStamps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseProvider",
                table: "DeploymentStamps",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DeploymentStamps",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"),
                columns: new[] { "DatabaseConnectionString", "DatabaseProvider" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"),
                columns: new[] { "DatabaseConnectionString", "DatabaseProvider" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatabaseConnectionString",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DatabaseProvider",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DatabaseConnectionString",
                table: "DeploymentStamps");

            migrationBuilder.DropColumn(
                name: "DatabaseProvider",
                table: "DeploymentStamps");
        }
    }
}

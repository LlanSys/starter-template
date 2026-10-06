using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class RemoveStampConnectionStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CacheConnectionString",
                table: "DeploymentStamps");

            migrationBuilder.DropColumn(
                name: "DatabaseConnectionString",
                table: "DeploymentStamps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CacheConnectionString",
                table: "DeploymentStamps",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseConnectionString",
                table: "DeploymentStamps",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DeploymentStamps",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"),
                columns: new[] { "CacheConnectionString", "DatabaseConnectionString" },
                values: new object[] { null, null });
        }
    }
}

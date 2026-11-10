using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class TrackProvisioningAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProvisioningDeadline",
                table: "Tenants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProvisioningOperationId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProvisioningDeadline",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ProvisioningOperationId",
                table: "Tenants");
        }
    }
}

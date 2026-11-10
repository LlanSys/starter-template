using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SerializeWorkspaceSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceSetupLeaseId",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WorkspaceSetupLeaseUntil",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkspaceSetupLeaseId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WorkspaceSetupLeaseUntil",
                table: "Tenants");
        }
    }
}

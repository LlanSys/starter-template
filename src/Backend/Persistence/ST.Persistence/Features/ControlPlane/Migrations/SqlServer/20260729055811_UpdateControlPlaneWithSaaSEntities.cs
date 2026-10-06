using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class UpdateControlPlaneWithSaaSEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Tenants",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "Tenants",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "DeploymentStamps",
                columns: new[] { "Id", "CacheConnectionString", "CreatedAt", "CreatedBy", "DatabaseConnectionString", "IsolationTier", "KeyVaultUri", "Name", "TargetResourceGroup", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("0194f700-0000-7000-8000-000000000001"), null, new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, 0, null, "default-pooled-stamp", "rg-startertemplate-dev", null, null });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "ContactEmail", "CreatedAt", "CreatedBy", "DeploymentStampId", "DisplayName", "HostName", "Identifier", "MaxUsers", "Status", "SubscriptionTier", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("0194f700-0000-7000-8000-000000000001"), "admin@startertemplate.local", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", new Guid("0194f700-0000-7000-8000-000000000001"), "Default Tenant", "localhost", "default", 100, 0, 0, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Identifier",
                table: "Tenants",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentStamps_Name",
                table: "DeploymentStamps",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Identifier",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_DeploymentStamps_Name",
                table: "DeploymentStamps");

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "DeploymentStamps",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"));

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "Tenants");
        }
    }
}

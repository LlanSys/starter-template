using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class EnforceExclusiveStampOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExclusiveStampId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Tenants] SET [ExclusiveStampId] = [DeploymentStampId] WHERE [DeploymentStampId] IN (SELECT [Id] FROM [DeploymentStamps] WHERE [IsolationTier] = 1);");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ExclusiveStampId",
                table: "Tenants",
                column: "ExclusiveStampId",
                unique: true,
                filter: "[ExclusiveStampId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_ExclusiveStampId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ExclusiveStampId",
                table: "Tenants");
        }
    }
}

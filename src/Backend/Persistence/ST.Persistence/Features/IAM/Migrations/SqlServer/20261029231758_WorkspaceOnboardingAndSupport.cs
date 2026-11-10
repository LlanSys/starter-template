using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class WorkspaceOnboardingAndSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_NormalizedUserName",
                table: "Users",
                column: "NormalizedUserName");

            migrationBuilder.CreateIndex(
                name: "UX_AppUsers_Tenant_UserName",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUsers_NormalizedUserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_AppUsers_Tenant_UserName",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }
    }
}

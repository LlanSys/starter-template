using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.PostgreSql
{
    public partial class UpgradeManagedTaskAssignmentRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Upgrade only the trusted provisioning-owned role; labels and template copies alone are insufficient.
            migrationBuilder.Sql(
                """
                INSERT INTO "RoleClaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT role."Id", 'permission', 'tasks.assign'
                FROM "Roles" AS role
                WHERE role."Name" = 'Tenant Administrator'
                  AND role."NormalizedName" = 'TENANT ADMINISTRATOR'
                  AND role."TemplateKey" = 'iam-administrator' AND NOT role."IsDeleted"
                  AND NOT EXISTS (SELECT 1 FROM "RoleClaims" AS claim
                      WHERE claim."RoleId" = role."Id" AND claim."ClaimType" = 'permission' AND claim."ClaimValue" = 'tasks.assign');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not remove pre-existing or subsequently delegated grants on rollback.
        }
    }
}

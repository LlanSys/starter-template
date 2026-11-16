using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class EnforceTenantAccountAdmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Human");

            migrationBuilder.Sql("""
DO $$ BEGIN
IF EXISTS (SELECT 1 FROM "Users" WHERE "NormalizedEmail" IS NOT NULL GROUP BY "TenantId", "NormalizedEmail" HAVING COUNT(*) > 1) THEN
RAISE EXCEPTION 'Resolve duplicate normalized emails within each workspace before upgrading account admission.';
END IF; END $$;
UPDATE "Users" AS u SET "Kind" = 'SupportProxy'
WHERE EXISTS (SELECT 1 FROM "UserClaims" a JOIN "UserClaims" g ON g."UserId" = a."UserId"
WHERE a."UserId" = u."Id" AND a."ClaimType" = 'support_actor_id' AND a."ClaimValue" = u."CreatedBy"
AND g."ClaimType" = 'support_grant_id' AND u."UserName" = 'support-' || replace(g."ClaimValue", '-', '')
AND u."Email" = u."UserName" || '@example.invalid');
UPDATE "Users" AS u SET "Kind" = 'DemoProxy'
WHERE EXISTS (SELECT 1 FROM "UserClaims" a JOIN "UserClaims" d ON d."UserId" = a."UserId"
WHERE a."UserId" = u."Id" AND a."ClaimType" = 'demo_actor_id' AND a."ClaimValue" = u."CreatedBy"
AND d."ClaimType" = 'demo_workspace' AND d."ClaimValue" = 'true'
AND u."UserName" IN ('demo-viewer-' || a."ClaimValue", 'demo-admin-' || a."ClaimValue")
AND u."Email" = u."UserName" || '@example.invalid');
""");

            migrationBuilder.CreateIndex(
                name: "UX_AppUsers_Tenant_Email",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AppUsers_Tenant_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Users");
        }
    }
}

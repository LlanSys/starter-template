using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
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
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Human");

            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM [Users] WHERE [NormalizedEmail] IS NOT NULL GROUP BY [TenantId], [NormalizedEmail] HAVING COUNT(*) > 1)
THROW 51001, 'Resolve duplicate normalized emails within each workspace before upgrading account admission.', 1;
UPDATE u SET [Kind] = N'SupportProxy' FROM [Users] u
WHERE EXISTS (SELECT 1 FROM [UserClaims] a JOIN [UserClaims] g ON g.[UserId] = a.[UserId]
WHERE a.[UserId] = u.[Id] AND a.[ClaimType] = N'support_actor_id' AND a.[ClaimValue] = u.[CreatedBy]
AND g.[ClaimType] = N'support_grant_id' AND u.[UserName] = N'support-' + REPLACE(g.[ClaimValue], N'-', N'')
AND u.[Email] = u.[UserName] + N'@example.invalid');
UPDATE u SET [Kind] = N'DemoProxy' FROM [Users] u
WHERE EXISTS (SELECT 1 FROM [UserClaims] a JOIN [UserClaims] d ON d.[UserId] = a.[UserId]
WHERE a.[UserId] = u.[Id] AND a.[ClaimType] = N'demo_actor_id' AND a.[ClaimValue] = u.[CreatedBy]
AND d.[ClaimType] = N'demo_workspace' AND d.[ClaimValue] = N'true'
AND u.[UserName] IN (N'demo-viewer-' + a.[ClaimValue], N'demo-admin-' + a.[ClaimValue])
AND u.[Email] = u.[UserName] + N'@example.invalid');
""");

            migrationBuilder.CreateIndex(
                name: "UX_AppUsers_Tenant_Email",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");
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

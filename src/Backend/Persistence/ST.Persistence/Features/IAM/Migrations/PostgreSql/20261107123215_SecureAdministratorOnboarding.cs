using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SecureAdministratorOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdministratorInvitationId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdministratorSetupCompletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SsoAuthorizationCodes",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SecurityStampHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConnectionProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SsoAuthorizationCodes", x => x.Hash);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SsoAuthorizationCodes_ExpiresAt",
                table: "SsoAuthorizationCodes",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SsoAuthorizationCodes");

            migrationBuilder.DropColumn(
                name: "AdministratorInvitationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdministratorSetupCompletedAt",
                table: "Users");
        }
    }
}

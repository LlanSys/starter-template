using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class QuarantineKycEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextSafetyScanAt",
                table: "KycDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SafetyCheckedAt",
                table: "KycDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyDefinitions",
                table: "KycDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SafetyDefinitionsAt",
                table: "KycDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyEngine",
                table: "KycDocuments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SafetyLeaseId",
                table: "KycDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SafetyLeaseUntil",
                table: "KycDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyReasonCode",
                table: "KycDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SafetyScanAttempts",
                table: "KycDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SafetyStatus",
                table: "KycDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
UPDATE [KycDocuments] SET [SafetyStatus] = CASE WHEN [ErasedAt] IS NULL THEN 0 ELSE 6 END, [NextSafetyScanAt] = CASE WHEN [ErasedAt] IS NULL THEN CAST('1970-01-01T00:00:00+00:00' AS datetimeoffset) ELSE NULL END;
""");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_SafetyStatus_NextSafetyScanAt_SafetyLeaseUntil",
                table: "KycDocuments",
                columns: new[] { "SafetyStatus", "NextSafetyScanAt", "SafetyLeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KycDocuments_SafetyStatus_NextSafetyScanAt_SafetyLeaseUntil",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "NextSafetyScanAt",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyCheckedAt",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyDefinitions",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyDefinitionsAt",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyEngine",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyLeaseId",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyLeaseUntil",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyReasonCode",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyScanAttempts",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "SafetyStatus",
                table: "KycDocuments");
        }
    }
}

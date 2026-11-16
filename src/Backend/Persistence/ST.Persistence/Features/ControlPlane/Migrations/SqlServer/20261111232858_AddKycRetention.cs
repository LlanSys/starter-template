using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddKycRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ErasedAt",
                table: "KycDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KycRetentionAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycRetentionAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KycRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ApprovedMonths = table.Column<int>(type: "int", nullable: false),
                    ClosedDays = table.Column<int>(type: "int", nullable: false),
                    InactiveDays = table.Column<int>(type: "int", nullable: false),
                    RecoveryDays = table.Column<int>(type: "int", nullable: false),
                    OrphanDays = table.Column<int>(type: "int", nullable: false),
                    HoldReviewDays = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KycUploadReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycUploadReservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KycRetentionCases",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetainUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecoveryUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Held = table.Column<bool>(type: "bit", nullable: false),
                    HoldReviewDue = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycRetentionCases", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_KycRetentionCases_KycRetentionPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "KycRetentionPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KycRetentionCases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KycRetentionPolicySelection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycRetentionPolicySelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KycRetentionPolicySelection_KycRetentionPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "KycRetentionPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "KycRetentionPolicies",
                columns: new[] { "Id", "ApprovedMonths", "ClosedDays", "HoldReviewDays", "InactiveDays", "Name", "OrphanDays", "PublishedAt", "PublishedBy", "Reason", "RecoveryDays" },
                values: new object[] { new Guid("019a7420-0000-7000-8000-000000000001"), 24, 90, 90, 30, "General Business", 7, new DateTimeOffset(new DateTime(2026, 11, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", "Initial general-business retention schedule.", 30 });

            migrationBuilder.InsertData(
                table: "KycRetentionPolicySelection",
                columns: new[] { "Id", "PolicyId", "Revision" },
                values: new object[] { new Guid("019a7420-0000-7000-8000-000000000001"), new Guid("019a7420-0000-7000-8000-000000000001"), new Guid("019a7420-0000-7000-8000-000000000001") });

            migrationBuilder.CreateIndex(
                name: "IX_KycRetentionAudit_TenantId_OccurredAt",
                table: "KycRetentionAudit",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KycRetentionCases_NextAttemptAt",
                table: "KycRetentionCases",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_KycRetentionCases_Phase_RetainUntil",
                table: "KycRetentionCases",
                columns: new[] { "Phase", "RetainUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_KycRetentionCases_PolicyId",
                table: "KycRetentionCases",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_KycRetentionPolicySelection_PolicyId",
                table: "KycRetentionPolicySelection",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_KycUploadReservations_State_LeaseUntil",
                table: "KycUploadReservations",
                columns: new[] { "State", "LeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KycRetentionAudit");

            migrationBuilder.DropTable(
                name: "KycRetentionCases");

            migrationBuilder.DropTable(
                name: "KycRetentionPolicySelection");

            migrationBuilder.DropTable(
                name: "KycUploadReservations");

            migrationBuilder.DropTable(
                name: "KycRetentionPolicies");

            migrationBuilder.DropColumn(
                name: "ErasedAt",
                table: "KycDocuments");
        }
    }
}

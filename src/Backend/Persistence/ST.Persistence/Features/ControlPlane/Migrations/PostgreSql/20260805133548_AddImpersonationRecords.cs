using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddImpersonationRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImpersonationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTenantName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiryTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpersonationRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationRecords_ActorId",
                table: "ImpersonationRecords",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationRecords_TargetTenantId",
                table: "ImpersonationRecords",
                column: "TargetTenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpersonationRecords");
        }
    }
}

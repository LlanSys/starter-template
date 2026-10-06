using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialControlPlanePostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeploymentStamps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetResourceGroup = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsolationTier = table.Column<int>(type: "integer", nullable: false),
                    DatabaseConnectionString = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    KeyVaultUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CacheConnectionString = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentStamps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HostName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeploymentStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_DeploymentStamps_DeploymentStampId",
                        column: x => x.DeploymentStampId,
                        principalTable: "DeploymentStamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DeploymentStampId",
                table: "Tenants",
                column: "DeploymentStampId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_HostName",
                table: "Tenants",
                column: "HostName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "DeploymentStamps");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialStarter : Migration
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
                    KeyVaultUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DatabaseProvider = table.Column<string>(type: "text", nullable: true),
                    DatabaseConnectionString = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentStamps", x => x.Id);
                });

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
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpersonationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HostName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionTier = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeploymentStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatabaseProvider = table.Column<string>(type: "text", nullable: true),
                    DatabaseConnectionString = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
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

            migrationBuilder.CreateTable(
                name: "TenantModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantModules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DeploymentStamps",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DatabaseConnectionString", "DatabaseProvider", "IsolationTier", "KeyVaultUri", "Name", "RowVersion", "TargetResourceGroup", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("0194f700-0000-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, 0, null, "default-pooled-stamp", Array.Empty<byte>(), "rg-startertemplate-dev", null, null });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "ContactEmail", "CreatedAt", "CreatedBy", "DatabaseConnectionString", "DatabaseProvider", "DeploymentStampId", "DisplayName", "HostName", "Identifier", "MaxUsers", "RowVersion", "Status", "SubscriptionTier", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("0194f700-0000-7000-8000-000000000001"), "admin@startertemplate.local", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, new Guid("0194f700-0000-7000-8000-000000000001"), "Default Tenant", "localhost", "default", 100, Array.Empty<byte>(), 3, 0, null, null });

            migrationBuilder.InsertData(
                table: "TenantModules",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "ExpiresAt", "IsActive", "ModuleKey", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0194f700-0000-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "Core", Array.Empty<byte>(), new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "IAM", Array.Empty<byte>(), new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "Shared", Array.Empty<byte>(), new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "Showcase", Array.Empty<byte>(), new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentStamps_Name",
                table: "DeploymentStamps",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationRecords_ActorId",
                table: "ImpersonationRecords",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationRecords_TargetTenantId",
                table: "ImpersonationRecords",
                column: "TargetTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantModules_TenantId_ModuleKey",
                table: "TenantModules",
                columns: new[] { "TenantId", "ModuleKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DeploymentStampId",
                table: "Tenants",
                column: "DeploymentStampId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_HostName",
                table: "Tenants",
                column: "HostName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Identifier",
                table: "Tenants",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpersonationRecords");

            migrationBuilder.DropTable(
                name: "TenantModules");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "DeploymentStamps");
        }
    }
}

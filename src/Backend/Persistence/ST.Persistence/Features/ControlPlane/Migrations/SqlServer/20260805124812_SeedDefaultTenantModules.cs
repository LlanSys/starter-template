using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class SeedDefaultTenantModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TenantModules",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "ExpiresAt", "IsActive", "ModuleKey", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0194f700-0000-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "Core", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "IAM", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "Banking", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f700-0000-7000-8000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, true, "HR", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000005"));
        }
    }
}

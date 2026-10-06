using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.Shared.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RemoveDatetimeOffsetAndFixFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentRecords_ProviderReference",
                table: "PaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRecords_TenantId_IdempotencyKey",
                table: "PaymentRecords");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_ProviderReference",
                table: "PaymentRecords",
                column: "ProviderReference",
                filter: "[ProviderReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_TenantId_IdempotencyKey",
                table: "PaymentRecords",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentRecords_ProviderReference",
                table: "PaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRecords_TenantId_IdempotencyKey",
                table: "PaymentRecords");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_ProviderReference",
                table: "PaymentRecords",
                column: "ProviderReference",
                filter: "ProviderReference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_TenantId_IdempotencyKey",
                table: "PaymentRecords",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "IdempotencyKey IS NOT NULL");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class ScopeKycUploadDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KycDocuments_TenantId_Sha256",
                table: "KycDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_TenantId_Sha256_UploadedBy",
                table: "KycDocuments",
                columns: new[] { "TenantId", "Sha256", "UploadedBy" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KycDocuments_TenantId_Sha256_UploadedBy",
                table: "KycDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_TenantId_Sha256",
                table: "KycDocuments",
                columns: new[] { "TenantId", "Sha256" },
                unique: true);
        }
    }
}

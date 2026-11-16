using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class ClassifyKycDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KycDocuments_TenantId_Sha256_UploadedBy",
                table: "KycDocuments");

            migrationBuilder.AddColumn<string>(
                name: "DocumentTypeKey",
                table: "KycDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_TenantId_DocumentTypeKey_UploadedAt",
                table: "KycDocuments",
                columns: new[] { "TenantId", "DocumentTypeKey", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_TenantId_Sha256_UploadedBy_DocumentTypeKey_DocumentType",
                table: "KycDocuments",
                columns: new[] { "TenantId", "Sha256", "UploadedBy", "DocumentTypeKey", "DocumentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KycDocuments_TenantId_DocumentTypeKey_UploadedAt",
                table: "KycDocuments");

            migrationBuilder.DropIndex(
                name: "IX_KycDocuments_TenantId_Sha256_UploadedBy_DocumentTypeKey_DocumentType",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentTypeKey",
                table: "KycDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_TenantId_Sha256_UploadedBy",
                table: "KycDocuments",
                columns: new[] { "TenantId", "Sha256", "UploadedBy" },
                unique: true);
        }
    }
}

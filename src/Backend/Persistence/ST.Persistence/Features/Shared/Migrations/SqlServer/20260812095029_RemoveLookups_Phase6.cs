using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.Shared.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RemoveLookups_Phase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lkp_CustomerStatuses");

            migrationBuilder.DropTable(
                name: "Lkp_CustomerTypes");

            migrationBuilder.DropTable(
                name: "Lkp_DirectorRelationTypes");

            migrationBuilder.DropTable(
                name: "Lkp_FailedMessageStatuses");

            migrationBuilder.DropTable(
                name: "Lkp_IdentificationTypes");

            migrationBuilder.DropTable(
                name: "Lkp_LineOfBusiness");

            migrationBuilder.DropTable(
                name: "Lkp_SegmentTypes");

            migrationBuilder.DropTable(
                name: "Lkp_SubSegmentTypes");

            migrationBuilder.DropTable(
                name: "LookupCatalogTypes");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "PaymentRecords",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "Lkp_CustomerStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_CustomerStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_CustomerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_CustomerTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_DirectorRelationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_DirectorRelationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_FailedMessageStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_FailedMessageStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_IdentificationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_IdentificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_LineOfBusiness",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_LineOfBusiness", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_SegmentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_SegmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_SubSegmentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_SubSegmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupCatalogTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCatalogTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Lkp_CustomerStatuses",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "Draft", null, null, "Draft", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "Active", null, null, "Active", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "Suspended", null, null, "Suspended", 4, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, "Closed", null, null, "Closed", 5, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 5, "PendingApproval", null, null, "Pending Approval", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Lkp_CustomerTypes",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "Individual", null, null, "Individual", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "Corporate", null, null, "Corporate", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "Institutional", null, null, "Institutional", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, "SmallMediumEnterprise", null, null, "Small & Medium Enterprise", 4, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 5, "Enterprise", null, null, "Enterprise", 5, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Lkp_DirectorRelationTypes",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "Director", null, null, "Director", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "Shareholder", null, null, "Shareholder", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "Signatory", null, null, "Signatory", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, "BeneficialOwner", null, null, "Beneficial Owner", 4, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 5, "Guarantor", null, null, "Guarantor", 5, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Lkp_FailedMessageStatuses",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "Transient", null, null, "Transient", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "Permanent", null, null, "Permanent", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "ManualRetry", null, null, "Manual Retry", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Lkp_IdentificationTypes",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "CertificateOfIncorporation", null, null, "Certificate of Incorporation", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "TIN", null, null, "Tax Identification Number", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "BusinessLicense", null, null, "Business License", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, "CompanyRegistrationCertificate", null, null, "Company Registration Certificate", 4, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Lkp_LineOfBusiness",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "Agriculture", null, null, "Agriculture", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "Manufacturing", null, null, "Manufacturing", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "Technology", null, null, "Technology", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, "FinancialServices", null, null, "Financial Services", 4, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 5, "Retail", null, null, "Retail", 5, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 6, "Services", null, null, "Services", 6, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 7, "Proprietary", null, null, "Proprietary", 7, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 8, "Trading", null, null, "Trading", 8, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Lkp_SegmentTypes",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "PublicLimitedCompany", null, null, "Public Limited Company", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "PrivateLimitedCompany", null, null, "Private Limited Company", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "SoleProprietorship", null, null, "Sole Proprietorship", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, "Corporate", null, null, "Corporate", 4, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 5, "Retail", null, null, "Retail", 5, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 6, "SME", null, null, "Small & Medium Enterprise", 6, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 7, "Individual", null, null, "Individual", 7, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Lkp_SubSegmentTypes",
                columns: new[] { "Id", "Code", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsDeleted", "TenantId" },
                values: new object[,]
                {
                    { 1, "Local", null, null, "Local", 1, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, "Multinational", null, null, "Multinational", 2, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, "GovernmentOwned", null, null, "Government Owned", 3, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, "Partnership", null, null, "Partnership", 4, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 5, "PrivateLimitedCompany", null, null, "Private Limited Company", 5, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 6, "PublicLimitedCompany", null, null, "Public Limited Company", 6, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 7, "SoleProprietorship", null, null, "Sole Proprietorship", 7, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 8, "NGO", null, null, "Non-Governmental Organisation", 8, false, new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 9, "Individual", null, null, "Individual", 9, false, new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "LookupCatalogTypes",
                columns: new[] { "Id", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "TenantId" },
                values: new object[,]
                {
                    { 1, null, null, "Lifecycle statuses available to customer records.", true, false, "CustomerStatus", "Customer statuses", new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 2, null, null, "Classification values used when creating and segmenting customers.", true, false, "CustomerType", "Customer types", new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 3, null, null, "Relationship labels used for customer directors and signatories.", true, false, "DirectorRelationType", "Director relation types", new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 4, null, null, "Operational statuses for failed message tracking.", true, false, "FailedMessageStatus", "Failed message statuses", new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 5, null, null, "Identity document types used across onboarding and verification.", true, false, "IdentificationType", "Identification types", new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 6, null, null, "Business line values used by banking and reporting flows.", true, false, "LineOfBusiness", "Lines of business", new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 7, null, null, "Primary customer segmentation values.", true, false, "SegmentType", "Segment types", new Guid("0194f700-0000-7000-8000-000000000001") },
                    { 8, null, null, "Secondary customer segmentation values.", true, false, "SubSegmentType", "Sub-segment types", new Guid("0194f700-0000-7000-8000-000000000001") }
                });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerStatusLookup_TenantId_Code",
                table: "Lkp_CustomerStatuses",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CustomerTypeLookup_TenantId_Code",
                table: "Lkp_CustomerTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DirectorRelationTypeLookup_TenantId_Code",
                table: "Lkp_DirectorRelationTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FailedMessageStatusLookup_TenantId_Code",
                table: "Lkp_FailedMessageStatuses",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_IdentificationTypeLookup_TenantId_Code",
                table: "Lkp_IdentificationTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LineOfBusinessLookup_TenantId_Code",
                table: "Lkp_LineOfBusiness",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SegmentTypeLookup_TenantId_Code",
                table: "Lkp_SegmentTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SubSegmentTypeLookup_TenantId_Code",
                table: "Lkp_SubSegmentTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LookupCatalogTypes_TenantId_Key",
                table: "LookupCatalogTypes",
                columns: new[] { "TenantId", "Key" },
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.Shared.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialSharedMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FailedMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ErrorStackTrace = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxState",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "Lkp_CustomerStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_CustomerStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_CustomerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_CustomerTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_DirectorRelationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_DirectorRelationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_FailedMessageStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_FailedMessageStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_IdentificationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_IdentificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_LineOfBusiness",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_LineOfBusiness", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_SegmentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_SegmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lkp_SubSegmentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lkp_SubSegmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupCatalogTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCatalogTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CustomerReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StatusMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
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
                name: "IX_EmailTemplates_Name",
                table: "EmailTemplates",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FailedMessages_MessageId",
                table: "FailedMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_FailedMessages_Status_IsResolved_FailedAt",
                table: "FailedMessages",
                columns: new[] { "Status", "IsResolved", "FailedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                table: "InboxState",
                column: "Delivered");

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

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_CustomerReference",
                table: "PaymentRecords",
                column: "CustomerReference",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId_Key",
                table: "TenantSettings",
                columns: new[] { "TenantId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "FailedMessages");

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

            migrationBuilder.DropTable(
                name: "OutboxMessage");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropTable(
                name: "InboxState");

            migrationBuilder.DropTable(
                name: "OutboxState");
        }
    }
}

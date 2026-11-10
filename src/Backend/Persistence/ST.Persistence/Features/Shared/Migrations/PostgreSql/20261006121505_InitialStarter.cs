using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ST.Persistence.Features.Shared.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialStarter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
CREATE TABLE qrtz_job_details
  (
    sched_name TEXT NOT NULL,
    job_name TEXT NOT NULL,
    job_group TEXT NOT NULL,
    description TEXT NULL,
    job_class_name TEXT NOT NULL,
    is_durable BOOL NOT NULL,
    is_nonconcurrent BOOL NOT NULL,
    is_update_data BOOL NOT NULL,
    requests_recovery BOOL NOT NULL,
    job_data BYTEA NULL,
    PRIMARY KEY (sched_name, job_name, job_group)
);

CREATE TABLE qrtz_triggers
  (
    sched_name TEXT NOT NULL,
    trigger_name TEXT NOT NULL,
    trigger_group TEXT NOT NULL,
    job_name TEXT NOT NULL,
    job_group TEXT NOT NULL,
    description TEXT NULL,
    next_fire_time BIGINT NULL,
    prev_fire_time BIGINT NULL,
    priority INTEGER NULL,
    trigger_state TEXT NOT NULL,
    trigger_type TEXT NOT NULL,
    start_time BIGINT NOT NULL,
    end_time BIGINT NULL,
    calendar_name TEXT NULL,
    misfire_instr SMALLINT NULL,
    misfire_orig_fire_time BIGINT NULL,
    execution_group VARCHAR(200) NULL,
    preferred_node VARCHAR(200) NULL,
    preferred_node_auto BOOL NOT NULL DEFAULT FALSE,
    job_data BYTEA NULL,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, job_name, job_group)
      REFERENCES qrtz_job_details (sched_name, job_name, job_group)
);

CREATE TABLE qrtz_simple_triggers
  (
    sched_name TEXT NOT NULL,
    trigger_name TEXT NOT NULL,
    trigger_group TEXT NOT NULL,
    repeat_count BIGINT NOT NULL,
    repeat_interval BIGINT NOT NULL,
    times_triggered BIGINT NOT NULL,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
      REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
      ON DELETE CASCADE
);

CREATE TABLE qrtz_simprop_triggers
  (
    sched_name TEXT NOT NULL,
    trigger_name TEXT NOT NULL,
    trigger_group TEXT NOT NULL,
    str_prop_1 TEXT NULL,
    str_prop_2 TEXT NULL,
    str_prop_3 TEXT NULL,
    int_prop_1 INTEGER NULL,
    int_prop_2 INTEGER NULL,
    long_prop_1 BIGINT NULL,
    long_prop_2 BIGINT NULL,
    dec_prop_1 NUMERIC NULL,
    dec_prop_2 NUMERIC NULL,
    bool_prop_1 BOOL NULL,
    bool_prop_2 BOOL NULL,
    time_zone_id TEXT NULL,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
      REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
      ON DELETE CASCADE
);

CREATE TABLE qrtz_cron_triggers
  (
    sched_name TEXT NOT NULL,
    trigger_name TEXT NOT NULL,
    trigger_group TEXT NOT NULL,
    cron_expression TEXT NOT NULL,
    time_zone_id TEXT,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
      REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
      ON DELETE CASCADE
);

CREATE TABLE qrtz_blob_triggers
  (
    sched_name TEXT NOT NULL,
    trigger_name TEXT NOT NULL,
    trigger_group TEXT NOT NULL,
    blob_data BYTEA NULL,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
      REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
      ON DELETE CASCADE
);

CREATE TABLE qrtz_calendars
  (
    sched_name TEXT NOT NULL,
    calendar_name TEXT NOT NULL,
    calendar BYTEA NOT NULL,
    PRIMARY KEY (sched_name, calendar_name)
);

CREATE TABLE qrtz_paused_trigger_grps
  (
    sched_name TEXT NOT NULL,
    trigger_group TEXT NOT NULL,
    PRIMARY KEY (sched_name, trigger_group)
);

CREATE TABLE qrtz_fired_triggers
  (
    sched_name TEXT NOT NULL,
    entry_id TEXT NOT NULL,
    trigger_name TEXT NOT NULL,
    trigger_group TEXT NOT NULL,
    instance_name TEXT NOT NULL,
    fired_time BIGINT NOT NULL,
    sched_time BIGINT NOT NULL,
    priority INTEGER NOT NULL,
    state TEXT NOT NULL,
    job_name TEXT NULL,
    job_group TEXT NULL,
    is_nonconcurrent BOOL NOT NULL,
    requests_recovery BOOL NULL,
    execution_group VARCHAR(200) NULL,
    PRIMARY KEY (sched_name, entry_id)
);

CREATE TABLE qrtz_scheduler_state
  (
    sched_name TEXT NOT NULL,
    instance_name TEXT NOT NULL,
    last_checkin_time BIGINT NOT NULL,
    checkin_interval BIGINT NOT NULL,
    PRIMARY KEY (sched_name, instance_name)
);

CREATE TABLE qrtz_locks
  (
    sched_name TEXT NOT NULL,
    lock_name TEXT NOT NULL,
    PRIMARY KEY (sched_name, lock_name)
);

CREATE INDEX idx_qrtz_j_req_recovery ON qrtz_job_details (requests_recovery);
CREATE INDEX idx_qrtz_t_next_fire_time ON qrtz_triggers (next_fire_time);
CREATE INDEX idx_qrtz_t_state ON qrtz_triggers (trigger_state);
CREATE INDEX idx_qrtz_t_nft_st ON qrtz_triggers (next_fire_time, trigger_state);
CREATE INDEX idx_qrtz_ft_trig_name ON qrtz_fired_triggers (trigger_name);
CREATE INDEX idx_qrtz_ft_trig_group ON qrtz_fired_triggers (trigger_group);
CREATE INDEX idx_qrtz_ft_trig_nm_gp ON qrtz_fired_triggers (sched_name, trigger_name, trigger_group);
CREATE INDEX idx_qrtz_ft_trig_inst_name ON qrtz_fired_triggers (instance_name);
CREATE INDEX idx_qrtz_ft_job_name ON qrtz_fired_triggers (job_name);
CREATE INDEX idx_qrtz_ft_job_group ON qrtz_fired_triggers (job_group);
CREATE INDEX idx_qrtz_ft_job_req_recovery ON qrtz_fired_triggers (requests_recovery);

""");

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
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
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
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
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
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true, defaultValue: Array.Empty<byte>()),
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
                name: "OrgSettings",
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
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true, defaultValue: Array.Empty<byte>()),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true),
                    BusName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
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
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: Array.Empty<byte>())
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name",
                table: "EmailTemplates",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

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
                name: "IX_OrgSettings_TenantId_Key",
                table: "OrgSettings",
                columns: new[] { "TenantId", "Key" },
                unique: true);

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
                name: "IX_OutboxState_BusName_Created",
                table: "OutboxState",
                columns: new[] { "BusName", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_CustomerReference",
                table: "PaymentRecords",
                column: "CustomerReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_ProviderReference",
                table: "PaymentRecords",
                column: "ProviderReference",
                filter: "\"ProviderReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_TenantId_IdempotencyKey",
                table: "PaymentRecords",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "FailedMessages");

            migrationBuilder.DropTable(
                name: "OrgSettings");

            migrationBuilder.DropTable(
                name: "OutboxMessage");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "InboxState");

            migrationBuilder.DropTable(
                name: "OutboxState");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace ST.Persistence.Features.Shared.Migrations.PostgreSql;

public partial class Quartz46SchemaCompatibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Quartz owns these tables outside EF's model. Apply the required schema changes through 4.6.
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS qrtz_paused_job_grps (
                sched_name text NOT NULL,
                job_group text NOT NULL,
                PRIMARY KEY (sched_name, job_group)
            );

            ALTER TABLE qrtz_triggers
                ADD COLUMN IF NOT EXISTS misfire_orig_fire_time bigint NULL,
                ADD COLUMN IF NOT EXISTS execution_group varchar(200) NULL,
                ADD COLUMN IF NOT EXISTS preferred_node varchar(200) NULL,
                ADD COLUMN IF NOT EXISTS preferred_node_auto boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS retry_policy varchar(250) NULL,
                ADD COLUMN IF NOT EXISTS retry_attempt integer NULL,
                ADD COLUMN IF NOT EXISTS continues_trigger_name text NULL,
                ADD COLUMN IF NOT EXISTS continues_trigger_group text NULL,
                ADD COLUMN IF NOT EXISTS continuation_condition integer NULL,
                ADD COLUMN IF NOT EXISTS overlap_policy integer NULL,
                ADD COLUMN IF NOT EXISTS pause_reason varchar(250) NULL,
                ADD COLUMN IF NOT EXISTS paused_by varchar(200) NULL,
                ADD COLUMN IF NOT EXISTS paused_at bigint NULL,
                ADD COLUMN IF NOT EXISTS misfire_threshold bigint NULL;

            ALTER TABLE qrtz_fired_triggers
                ADD COLUMN IF NOT EXISTS execution_group varchar(200) NULL,
                ADD COLUMN IF NOT EXISTS progress integer NULL,
                ADD COLUMN IF NOT EXISTS progress_message varchar(250) NULL,
                ADD COLUMN IF NOT EXISTS interrupt_requested_time bigint NULL;

            ALTER TABLE qrtz_paused_trigger_grps
                ADD COLUMN IF NOT EXISTS pause_reason varchar(250) NULL,
                ADD COLUMN IF NOT EXISTS paused_by varchar(200) NULL,
                ADD COLUMN IF NOT EXISTS paused_at bigint NULL;

            ALTER TABLE qrtz_paused_job_grps
                ADD COLUMN IF NOT EXISTS pause_reason varchar(250) NULL,
                ADD COLUMN IF NOT EXISTS paused_by varchar(200) NULL,
                ADD COLUMN IF NOT EXISTS paused_at bigint NULL;

            ALTER TABLE qrtz_scheduler_state
                ADD COLUMN IF NOT EXISTS quartz_version varchar(20) NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retain scheduler data and any columns that already existed before this migration.
    }
}

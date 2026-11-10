# Tenant Backup and Restore Procedure

Record the recovery point objective and recovery time objective for each deployment. Verify the actual retention, redundancy, and earliest restorable point for each database and storage account. The isolated stamp templates rely on provider defaults for database backup settings. Rehearse recovery to measure what the deployment can deliver before committing to an objective.

## Rehearse before cutover

1. Identify the workspace, stamp, database provider, and the restore timestamp. Record the current route for rollback.
2. Restore into a new target using the provider's point-in-time restore process. Use an environment inaccessible to ordinary users and external integrations. Preserve the Data Protection application name, key ring, and access to its encryption key. Include workspace blobs in the rehearsal.
3. Check the restored schema, workspace ownership, data counts, and private assets. If the restore uses an earlier point, reconcile credentials, grants, MFA, and revoked access against current records before opening the workspace. Check pending jobs and outbox messages before enabling delivery.
4. For a pooled restore, treat the recovered database as containing multiple workspaces. For one-workspace recovery, extract only that workspace's rows with tenant predicates and ownership joins. Validate the import against the affected workspace and a second workspace. For a stamp-wide recovery, validate all workspaces on that stamp and coordinate the outage for all of them.

## Restore an isolated workspace

1. Suspend the approved workspace with `POST /api/v1/control-plane/tenants/{id}/suspend`. Quiesce scheduled and in-flight work and confirm the source stops receiving writes.
2. Verify the restored destination is registered, uses the serving host's database provider, and has the required schema and secrets. Run the [stamp migration procedure](stamp-migration-runbook.md) using `POST /api/v1/control-plane/tenants/{id}/stamp-migration` over TLS. Keep connection strings out of logs and tickets.
3. Reactivate with `POST /api/v1/control-plane/tenants/{id}/activate`. Confirm that old access and refresh tokens are rejected. Sign in afresh and check that reads, writes, jobs, and private assets use the restored route. Record the observed recovery point and time.
4. If validation fails, suspend again and assess any writes to the new database before returning to the recorded route. Retain the source and restore evidence under the deployment's retention policy until the recovery is accepted.

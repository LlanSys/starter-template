# Tenant Stamp Migration Runbook (Pooled to Isolated)

This runbook outlines the process of migrating an active tenant from a pooled infrastructure stamp to an isolated infrastructure stamp.

## 1. Preparation
1. Ensure the isolated stamp infrastructure is provisioned and running (Phase 5).
2. Note the target `DeploymentStampId`.
3. Put the Tenant into `Maintenance` status to prevent writes.
   - `PUT /api/controlplane/tenants/{tenantId}/status` (Body: `{ status: "Maintenance" }`)
   - This invalidates the tenant's active sessions and caches.

## 2. Database Migration
1. Obtain a read-only credential for the **Pooled Database**.
2. Run `pg_dump` (PostgreSQL) or `sqlpackage` (SQL Server) to extract the tenant's schema and data.
   - For PostgreSQL: `pg_dump -U read_user -h pooled-db.postgres.database.azure.com -d my_db -t "tenant_schema.*" > tenant_data.sql`
   - For SQL Server, BCP or specialized tooling (like Redgate) might be needed to isolate data by `TenantId`. If row-level security is active, impersonate the tenant to export.
3. Import the data into the **Isolated Database**.

## 3. Configuration Update
1. Once data is verified, update the Tenant's `DeploymentStampId` and `DatabaseConnectionString` using the Migration API.
2. The Control Plane API call will emit `TenantStampChangedDomainEvent`, flushing any remaining cached states.

## 4. Cutover
1. Update the Tenant status back to `Active`.
2. Users will be forced to log in again and acquire a new session token, which will now route to the new Isolated Stamp.

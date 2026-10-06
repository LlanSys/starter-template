# Tenant Backup and Restore Procedure

This runbook outlines the Data Protection (Backup) and Disaster Recovery (Restore) strategy for tenants hosted in Isolated Stamps, as well as the standard Azure backup strategy for the Pooled Stamp.

## Recovery Objectives (Isolated Stamps)
- **RPO (Recovery Point Objective):** 5 minutes (via Azure SQL/PostgreSQL Point-in-Time Restore transaction logs).
- **RTO (Recovery Time Objective):** < 2 hours for a full isolated stamp restoration.

## Azure Managed Backups

By default, all Azure SQL and Azure Database for PostgreSQL flexible servers provisioned by the Bicep template have automated backups enabled.
- **Retention:** 35 days for Point-in-Time Restore (PITR).
- **Geo-redundancy:** Backups are replicated to a paired Azure region to protect against primary region failure.

## Procedure: Point-In-Time Restore (PITR)

If an isolated tenant experiences catastrophic data loss or corruption (e.g., accidental deletion), the operator must perform a PITR.

1. **Locate the Database:** Navigate to the Azure Portal -> Resource Groups -> The isolated tenant's resource group.
2. **Initiate Restore:**
   - Select the SQL/PostgreSQL Server.
   - Click **Restore** from the overview blade.
   - Select the desired timestamp (must be within the 35-day retention window).
   - Enter a new database name for the restored data (e.g., `tenantdb-restored-20260810`).
3. **Verify Data Integrity:** Connect to the restored database using `psql` or SQL Server Management Studio and verify the target data is intact.
4. **Cutover to Restored DB:**
   - Put the Tenant into `Maintenance` mode via the Control Plane API: `PUT /api/controlplane/tenants/{tenantId}/status`.
   - Update the `DatabaseConnectionString` using the `MigrateTenantStampCommand` endpoint, pointing it to the newly restored database.
   - Set the Tenant status back to `Active`.
5. **Cleanup:** Delete the old (corrupted) database to stop incurring compute/storage charges.

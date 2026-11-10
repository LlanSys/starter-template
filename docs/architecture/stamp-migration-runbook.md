# Tenant Stamp Migration Runbook (Pooled to Isolated)

This runbook outlines the process of migrating an active tenant from a pooled infrastructure stamp to an isolated infrastructure stamp.

## 1. Preparation
1. Ensure the isolated stamp infrastructure is provisioned and running (Phase 5).
2. Note the target `DeploymentStampId`.
3. Suspend the workspace through Control Plane to prevent tenant traffic during the move. Reactivation invalidates sessions issued before the cutover.

## 2. Database Migration
1. Obtain a read-only credential for the pooled database.
2. Build a tenant-scoped export using `TenantId` predicates and foreign-key ownership joins for dependent rows. Pooled tenants use shared tables, so a schema or database dump includes other workspaces.
3. Validate record counts, ownership, credentials that require the original key ring, and required blobs before importing into the isolated database. Rehearse this in staging before scheduling a production cutover.

## 3. Configuration Update
1. Once the tenant export, private assets and queued work are verified, submit the destination stamp and connection through the Migration API. Supply `NewClientOrigin` when moving compute so sign-in follows the owning UI. The API verifies destination schemas and isolated ownership before switching routing.
2. The Control Plane API call will emit `TenantStampChangedDomainEvent`, flushing any remaining cached states.

## 4. Cutover
1. Reactivate the workspace through Control Plane after verifying the restored data.
2. Users will be forced to log in again and acquire a new session token, which will now route to the new Isolated Stamp.

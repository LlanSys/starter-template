# Workstation endpoint allocations

Host endpoints use localhost. Container-to-container connections use service DNS names and container ports.

| Project | Database | Redis | RabbitMQ / UI | SMTP / Mailpit UI | Seq | Azurite blob / queue / table |
| --- | --- | --- | --- | --- | --- | --- |
| StarterTemplate | SQL 14333, BT | 6380 | 5672 / 15672 | 1025 / 8025 | 5341 | 10000 / 10001 / 10002 |
| LlanSacco | SQL 14334, LS | 6381 | 5673 / 15673 | 1026 / 8026 | 5343 | 10010 / 10011 / 10012 |
| UbuntuSentinel | PostgreSQL 5433, ubuntusentinel | — | — | 1027 / 8027 | 5342 | 10003 / 10004 / 10005 |
| Lebrese Consultants | PostgreSQL 5434, lebrese_consultants | — | — | 1028 / 8028 | — | — |
| KHIEAdapter | PostgreSQL 5435, khieadapter_db | — | 5674 / 15674 | 1029 / 8029 | — | — |

These include optional services and reserved allocations, not a claim that every service is running. Native PostgreSQL owns 5432. The obsolete standalone ubuntu-sentinel-postgres container was removed on 2026-10-06; its data volume 92588bf47e4922876d8f527f24d33b24d90a5343596396ff621f7f79bb62ea20 is retained for recovery. Native SQL Express currently uses dynamic port 62225.

Run scripts/check-local-endpoints.ps1 before starting another Compose group. It resolves discovered Compose groups including all profiles, checks existing container bindings, host launch ports and duplicate user-secret identities. Supply -ComposeFiles for new groups without existing containers. The StarterTemplate and LlanSacco setup scripts run this check before startup. Linked Git worktrees that share a Compose project are treated as alternate checkouts of the same local application. The supplied Compose checkout owns its Visual Studio allocations for that check. Switch between those checkouts when running the same application. Separate local applications still need their own ports, Compose project names and user-secret IDs. Native services and future projects require a fresh inventory; allocations are not a permanent workstation-wide reservation.

StarterTemplate and LlanSacco API and Blazor hosts now have separate UserSecretsId values. Restart Visual Studio hosts after this change. The rename script generates new identities for future clones; local secrets must then be provisioned for the clone. Allocate new host ports and independent Compose project/volume names before first startup. Keep DataProtection:ApplicationName unchanged for an existing application.

Updated local secrets match each application's infrastructure. No database volumes were deleted or migrated. Application login and MFA flows still require a browser smoke test.

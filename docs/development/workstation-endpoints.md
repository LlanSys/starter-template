# Workstation endpoint allocations

Host endpoints use localhost. Container-to-container connections use service DNS names and container ports.

Several applications cloned from StarterTemplate often run on one workstation. Each needs its own host ports, Compose project and volume names, and user-secret identities. Keep a private inventory of the allocations on your workstation. Don't commit other applications' allocations to a shared template.

## StarterTemplate defaults and an example allocation

| Application | Database | Redis | RabbitMQ / UI | SMTP / Mailpit UI | Seq | Azurite blob / queue / table |
| --- | --- | --- | --- | --- | --- | --- |
| StarterTemplate (defaults) | SQL Server 14335 or PostgreSQL 15435; database ST | 6382 | 5675 / 15675 | 1030 / 8030 | 5344 | 10020 / 10021 / 10022 |
| Example second clone | SQL Server 14336 or PostgreSQL 15436 | 6383 | 5676 / 15676 | 1031 / 8031 | 5345 | 10030 / 10031 / 10032 |

The table covers optional services and reserved allocations; it doesn't mean every service is running. Native database services, such as a local PostgreSQL on 5432 or SQL Express on a dynamic port, also occupy ports. Include them in your inventory.

## Checking for conflicts

Run `scripts/check-local-endpoints.ps1` before starting another Compose group. It resolves the discovered Compose groups including all profiles, and checks existing container bindings, host launch ports and duplicate user-secret identities. Pass `-ComposeFiles` for new groups that have no containers yet.

Linked Git worktrees that share a Compose project are treated as alternate checkouts of the same local application. The supplied Compose checkout owns its Visual Studio allocations for that check, so switch between those checkouts when running the same application. Separate local applications still need their own ports, Compose project names and user-secret IDs. Allocations are not a permanent workstation-wide reservation: take a fresh inventory for native services and new projects.

For a new application, pass `-FocusSupplied` with its Compose files. This mode fails on conflicts that involve that application, including its host secret identities and standalone Docker port reservations, and reports conflicts between unrelated projects as warnings. Without the switch, the script audits the whole workstation. `scripts/setup-local-platform.ps1` and `scripts/setup-local-app-deployment.ps1` use the focused mode. Set infrastructure ports through the `*_HOST_PORT` environment values before setup. ClamAV uses `CLAMAV_HOST_PORT` and binds to loopback.

## Each clone's identity

The rename script generates new API and Blazor `UserSecretsId` values for every clone, so you must provision local secrets for the clone. Allocate new host ports and independent Compose project and volume names before the first startup. Keep `DataProtection:ApplicationName` unchanged for an application that already has data.

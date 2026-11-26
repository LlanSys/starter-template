# Workstation endpoint allocations

Host endpoints use localhost. Container-to-container connections use service DNS names and container ports.

Several applications cloned from StarterTemplate often run on one workstation. Each needs its own host ports, Compose project and volume names, and user-secret identities. The rename script gives each clone new Compose and identity names, and `scripts/allocate-local-ports.ps1` gives it its own ports.

## Automatic port allocation

Each application gets a *slot*. Slot 0 is the template's defaults; slot *n* adds *n* × 10 to every default port, so each clone keeps the template's layout.

- **When it runs:**
  - at the end of `rename-template.ps1`, so a new clone's launch settings are committed with its own ports
  - at the start of `setup-local-platform.ps1` (and therefore `setup-local-app-deployment.ps1`)
  - by hand: `.\scriptsllocate-local-ports.ps1`, with `-WhatIf` to preview or `-Force` to move to a new block
- **What counts as taken:**
  - every Docker container, running or stopped, and every service of its Compose project with all profiles
  - ports listening on this machine (native SQL Server, PostgreSQL, IIS and so on)
  - every application in the per-user registry `%LOCALAPPDATA%\StarterTemplate\port-allocations.json`, so applications that aren't running, or whose containers were removed, keep their ports. An application whose folder no longer exists is dropped from the registry automatically.
- **What it changes:**
  - the `*_HOST_PORT` and `APP_*_HOST_PORT` values in the ignored `ops/local/.env`
  - both `launchSettings.json` files
  - the local URLs in the API and Blazor `appsettings*.json` files and `scripts/Get-LocalQaLogin.ps1`
- **The template itself** keeps its defaults and registers them as slot 0. A clone already registered with free ports is left alone.

If an application has already run locally and you move it with `-Force`, rerun `setup-local-platform.ps1` so its user-secrets use the new ports, and recreate its containers.

## StarterTemplate defaults and the first clone

| Application | Database | Redis | RabbitMQ / UI | SMTP / Mailpit UI | Seq | Azurite blob / queue / table |
| --- | --- | --- | --- | --- | --- | --- |
| StarterTemplate (defaults) | SQL Server 14335 or PostgreSQL 15435; database ST | 6382 | 5675 / 15675 | 1030 / 8030 | 5344 | 10020 / 10021 / 10022 |
| First clone (slot 1) | SQL Server 14345 or PostgreSQL 15445 | 6392 | 5685 / 15685 | 1040 / 8040 | 5354 | 10030 / 10031 / 10032 |

Slot 1 also moves the Docker apps to 8190 / 8191 and the Visual Studio hosts to API 5267 / 7239 and Web 5216 / 7159. The table covers optional services and reserved allocations; it doesn't mean every service is running.

## Checking for conflicts

Run `scripts/check-local-endpoints.ps1` before starting another Compose group. It resolves the discovered Compose groups including all profiles, and checks existing container bindings, host launch ports and duplicate user-secret identities. Pass `-ComposeFiles` for new groups that have no containers yet.

Linked Git worktrees that share a Compose project are treated as alternate checkouts of the same local application. The supplied Compose checkout owns its Visual Studio allocations for that check, so switch between those checkouts when running the same application. Separate local applications still need their own ports, Compose project names and user-secret IDs. Registered allocations count as taken; other projects are seen through their containers.

For a new application, pass `-FocusSupplied` with its Compose files. This mode fails on conflicts that involve that application, including its host secret identities and standalone Docker port reservations, and reports conflicts between unrelated projects as warnings. Without the switch, the script audits the whole workstation. `scripts/setup-local-platform.ps1` and `scripts/setup-local-app-deployment.ps1` use the focused mode. ClamAV uses `CLAMAV_HOST_PORT` and binds to loopback.

## Each clone's identity

The rename script generates new API and Blazor `UserSecretsId` values, Compose project and volume names, and its own local ports for every clone, so you must provision local secrets for the clone. Keep `DataProtection:ApplicationName` unchanged for an application that already has data.

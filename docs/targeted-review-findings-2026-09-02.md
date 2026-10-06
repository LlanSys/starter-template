# Targeted Review Findings - 2026-09-02

## Scope

This note records only the findings observed during a narrow comparison of StarterTemplate's Azure observability implementation with the Lebrese Consultants deployment design. It is not a full security, architecture, cost, or operational audit.

## 1. Local Publish Profiles Contain Deployment Credentials

### Evidence

The following local files exist and contain Visual Studio publishing credentials:

- `src/Backend/Api/ST.Api/starter-template-api-dev.PublishSettings`
- `src/Frontend/Web/ST.UI.Blazor/starter-template-web-dev.PublishSettings`

Git verification performed on 2026-09-02 found that:

- Both files are excluded by the existing `*.publishsettings` rule in `.gitignore`.
- Neither file is currently tracked.
- No matching file was found in the repository's reachable Git history.

The finding is therefore a local credential-handling issue, not evidence that these credentials were committed to this repository.

### Required correction

- Rotate or reset both App Service publishing credentials because their secrecy can no longer be assumed after discovery and inspection.
- Delete the local publish-settings files when they are no longer required.
- Continue using GitHub Actions OIDC and managed identity for deployment; do not reintroduce publish profiles or long-lived deployment credentials into source control or workflows.
- Confirm in Azure that the referenced development App Services still exist. If they do not, record that the obsolete credentials are no longer usable.

### Status

Open. Credential rotation and local-file cleanup require deliberate Azure and workstation actions.

## 2. Grafana OTLP Is Implemented, but Azure Container Apps Still Depends on Log Analytics

### Evidence

The application implements a portable observability path:

- `Serilog.Sinks.OpenTelemetry` exports structured application logs.
- .NET OpenTelemetry exporters send traces and metrics through OTLP.
- Deployment configuration supplies `Observability__Otlp__Endpoint` and `Observability__Otlp__Headers` for Grafana Cloud or another compatible OTLP backend.

However, `ops/azure/modules/containerapps.bicep` configures the Container Apps environment with:

```bicep
appLogsConfiguration: {
  destination: 'log-analytics'
}
```

The root Azure deployment also provisions Log Analytics and Application Insights modules. Consequently, the Azure path is not currently a Grafana-only deployment and can incur Azure Monitor ingestion and retention costs in addition to the external OTLP backend.

### Required correction

- Make the Azure platform-log destination an explicit deployment choice rather than an unconditional dependency.
- Support a cost-controlled mode that sets the Container Apps environment log destination to `none` while continuing to export application logs, traces, and metrics directly through OTLP.
- Provision Log Analytics and Application Insights only when the selected observability mode requires them.
- Document the tradeoff: disabling stored Azure platform logs retains live Container Apps log streaming but removes historical Container Apps system and console log queries from Log Analytics.
- Keep OTLP endpoint headers in protected deployment secrets; never put Grafana credentials in parameter files or source control.

### Status

Open. The portable application telemetry path exists, but the Azure IaC needs an explicit observability mode and conditional resources.

## 3. The Existing OTLP Design Should Be Preserved

### Evidence

The provider-neutral `Observability` configuration and OTLP exporters allow the same application instrumentation to work with Grafana Cloud, New Relic, Azure Monitor-compatible collectors, and self-hosted OpenTelemetry collectors without changing business code.

### Required correction

No redesign is required. Future work should preserve the current OTLP boundary while removing unconditional Azure-specific observability resources. Local Seq support may remain a development convenience and must not become a required production dependency.

### Status

Accepted architectural direction; deployment conditionality remains open under Finding 2.

## Deferred Work

The following are deliberately outside this note:

- A repository-wide credential or secret-history scan.
- A complete Azure cost review.
- Validation of telemetry redaction and PII handling.
- Runtime verification of Grafana logs, traces, metrics, dashboards, alerts, and retention.
- Review of every Azure resource, workflow, Bicep module, generated ARM file, or deployment target.
- Remediation implementation and verification.

These items require a separate, explicitly scoped audit session.

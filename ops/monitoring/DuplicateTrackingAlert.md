Title: Alert rule - EF Core duplicate tracked entity detected

Description:
When EF Core throws InvalidOperationException with text "already being tracked" or "cannot be tracked", raise an alert for investigation.

Suggested rule (pseudo):

- Source: application logs (Serilog / ApplicationInsights)
- Condition: count of exceptions where Exception.Message contains "already being tracked" OR "cannot be tracked" > 0 in 5m
- Severity: High
- Actions: Create incident, attach recent logs, notify on-call

Include contextual fields: UserId, UserName, RemoteIp, UserAgent, RequestId

Notes:
- Enable sensitive data logging in staging for diagnostics; do NOT enable in production.

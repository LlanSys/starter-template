# API Security & Lifecycle

This document outlines the security and lifecycle policies for the StarterTemplate API.

## 1. Rate Limiting
Rate limiting is configured globally using `Microsoft.AspNetCore.RateLimiting`.
The API returns a standard `application/problem+json` response (HTTP 429) when rate limits are exceeded. The `Retry-After` header is included in the response.
Rate limits are configurable via `appsettings.json` under the `RateLimiting` section.

## 2. API Versioning & Deprecation
API versioning is enabled using `Asp.Versioning.Http`.
The API reports its version via the `X-API-Version` header.
When endpoints are deprecated, the `api-supported-versions` and `api-deprecated-versions` headers are included in the response.

## 3. Cross-Site Request Forgery (CSRF) Stance
The StarterTemplate solution uses a decoupled architecture where the Blazor Web App acts as an HTTP client consuming the API.
Authentication is managed via `JwtBearer` tokens stored securely by the client and passed in the `Authorization` header.

Because the API does **not** rely on ambient authority (i.e., it does not authenticate requests using cookies automatically sent by the browser), CSRF attacks are structurally impossible against the API endpoints.
Therefore, Anti-Forgery (`[AutoValidateAntiforgeryToken]`) is **not required** and intentionally omitted from the API layer.

## 4. PII Log Sanitization
Sensitive properties are automatically masked in Serilog before being written to any sink (Console, Seq, Application Insights).
The `PiiDestructuringPolicy` intercepts objects and redacts fields such as `Password`, `Token`, `MfaCode`, `TINNumber`, `Email`, and other Personal Identifiable Information (PII).

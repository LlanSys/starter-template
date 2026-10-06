# UI-to-Backend Flow

The default product-building flow is:

1. Start from the UI journey.
2. Call backend APIs through typed HTTP clients.
3. Keep DTO contracts in `ST.SharedKernel`.
4. Keep UI state, presentation models, and browser/session concerns in the UI layer.
5. Push business rules into Application and Domain when the journey exposes them.

## Default Rule

Frontend projects must not reference backend Application, Domain, Persistence, EF Core contexts, repositories, or MediatR handlers directly.

Use this boundary:

```text
Blazor UI -> typed HTTP client -> ST.Api -> Application -> Domain -> Persistence/Infrastructure
```

This keeps the API contract real from day one. The same backend endpoints can later serve Blazor, mobile, external API clients, API Management, integration tests, and deployment probes.

## Why Not Call Application Directly?

Direct Application calls can reduce HTTP serialization overhead, but they also couple the UI deployment to backend internals. That coupling makes these things harder:

- Independent API/UI deployment.
- Azure API Management adoption.
- Mobile and external client reuse.
- Authentication and authorization consistency.
- Contract testing.
- Observability at the API boundary.
- Versioning and backward compatibility.

For this base template, the HTTP boundary is the enterprise default.

## Allowed Exception

Direct in-process Application calls are allowed only when a module is explicitly designed as one deployable unit and does not need to expose the same capability to API, mobile, external clients, or API Management.

Document the exception before using it.

# Deployment Stamps over GraphQL

The Deployment Stamps slice exposes GraphQL alongside the REST controller. Both transports send the same MediatR requests and use the same platform permission, FluentValidation rules, and persistence. The GraphQL schema lives in `ST.Api/Features/ControlPlane/Stamps/GraphQL`.

## Run the example

- Visual Studio HTTPS API: `https://localhost:7229/graphql`
- Local Docker API: `http://localhost:8180/graphql`
- Development explorer: append `/ui` to the GraphQL endpoint, for example `http://localhost:8180/graphql/ui`.

Use a platform account with `platform_staff`, `control_plane.manage`, and authenticator enrollment. Add an `Authorization: Bearer YOUR_ACCESS_TOKEN` header in Nitro or your HTTP client. Use the existing API login in Scalar at `/scalar/v1` to obtain a token. For a password login that returns `requires2FA: true`, send `userId`, the authenticator `code`, and the login response's `token` as `challengeToken` to `POST /api/v1/iam/users/totp/verify`. Use the access token from the verification response for GraphQL.

Paste this into Nitro to list the stamps.

```graphql
query ListDeploymentStamps {
  deploymentStamps {
    id
    name
    targetResourceGroup
    isolationTier
  }
}
```

Create a stamp in a local development database with a unique name.

```graphql
mutation CreateStamp {
  createDeploymentStamp(input: {
    name: "graphql-demo"
    targetResourceGroup: "rg-graphql-demo"
    isolationTier: "Pooled"
  }) {
    stamp { id name }
    errors { code message field }
  }
}
```

Use the returned ID with `deploymentStamp(id: ...)`, `updateDeploymentStamp(id: ..., input: ...)`, or `GET /api/v1/control-plane/stamps/{id}` to see the same record through both transports.

For a plain HTTP request, POST JSON with `query` and optional `variables` to `/graphql`. Here is the list query in PowerShell.

```powershell
Invoke-RestMethod -Method Post -Uri 'http://localhost:8180/graphql' `
  -Headers @{ Authorization = 'Bearer YOUR_ACCESS_TOKEN' } `
  -ContentType 'application/json' `
  -Body '{"query":"{ deploymentStamps { id name isolationTier } }"}'
```

## Extend the pattern

| GraphQL field | REST equivalent | Application request |
| --- | --- | --- |
| `deploymentStamps` | `GET /api/v1/control-plane/stamps` | `GetAllDeploymentStampsQuery` |
| `deploymentStamp` | `GET /api/v1/control-plane/stamps/{id}` | `GetDeploymentStampByIdQuery` |
| `createDeploymentStamp` | `POST /api/v1/control-plane/stamps` | `CreateDeploymentStampCommand` |
| `updateDeploymentStamp` | `PUT /api/v1/control-plane/stamps/{id}` | `UpdateDeploymentStampCommand` |

Keep resolvers in the owning API feature. Apply the existing permission policy to each field, dispatch through MediatR, and allowlist output fields. Query resolvers receive separate service scopes so sibling queries can use EF concurrently. Mutations return expected business and validation failures in `errors`, where a field path such as `input.name` identifies the failing input. Transport authentication and resolver authorization failures use the usual HTTP and GraphQL errors.

`databaseConnectionString` is write-only. Supply sensitive inputs in variables. The API omits GraphQL request bodies from debug logging. The transport accepts authenticated POST requests, and the explorer is available in Development.

using System;

namespace ST.Api.Features.ControlPlane.Tenants.Dtos;

public record MigrateTenantStampRequest(
    Guid NewDeploymentStampId,
    string NewDatabaseConnectionString);

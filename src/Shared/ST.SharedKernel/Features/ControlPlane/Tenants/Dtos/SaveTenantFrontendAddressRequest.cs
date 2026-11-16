namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record SaveTenantFrontendAddressRequest(string? Origin, string Revision);

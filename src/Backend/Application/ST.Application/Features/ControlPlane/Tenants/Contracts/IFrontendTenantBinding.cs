namespace ST.Application.Features.ControlPlane.Tenants.Contracts;

public interface IFrontendTenantBinding
{
    Guid? TenantId
    {
        get;
    }
}
